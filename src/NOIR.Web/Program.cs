var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for performance and security
builder.WebHost.ConfigureKestrel(options =>
{
    // Request limits
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB default
    options.Limits.MaxRequestHeadersTotalSize = 32 * 1024; // 32 KB
    options.Limits.MaxRequestLineSize = 8 * 1024; // 8 KB

    // Connection limits
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxConcurrentUpgradedConnections = 100;

    // Timeouts
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);

    // Enable HTTP/2 and HTTP/3 (QUIC)
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });
});

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

// Add services to the container
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"]; // Vite dev server (port 3000 for Vibe Kanban compatibility)

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configure Response Compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

// Configure Output Caching (server-side caching)
builder.Services.AddOutputCache(options =>
{
    // No cache policy for sensitive data (auth, audit endpoints)
    options.AddPolicy("NoCache", builder => builder.NoCache());
});

// Configure JSON options with Source Generator for performance
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global rate limit policy - 100 requests per minute (Fixed Window)
    // Override in appsettings.json: RateLimiting:PermitLimit and RateLimiting:WindowMinutes
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100);
        limiterOptions.Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:WindowMinutes", 1));
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    // Auth endpoints use Sliding Window to prevent burst attacks at window boundaries
    // Configurable via appsettings.json: RateLimiting:AuthPermitLimit and RateLimiting:AuthWindowMinutes
    options.AddSlidingWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 100);
        limiterOptions.Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:AuthWindowMinutes", 1));
        limiterOptions.SegmentsPerWindow = 6; // 10-second segments for smoother limiting
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0; // No queuing for auth - immediate rejection
    });

    // Export endpoints - limit for data export operations
    // Configurable via appsettings.json: RateLimiting:ExportPermitLimit and RateLimiting:ExportWindowMinutes
    options.AddFixedWindowLimiter("export", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Configuration.GetValue("RateLimiting:ExportPermitLimit", 100);
        limiterOptions.Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:ExportWindowMinutes", 1));
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0; // No queuing for exports - immediate rejection
    });
});

// Add SignalR for real-time audit streaming
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 64 * 1024; // 64 KB
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add Application and Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

// Configure AuditRetention settings
builder.Services.Configure<AuditRetentionSettings>(builder.Configuration.GetSection(AuditRetentionSettings.SectionName));

// Configure Wolverine for CQRS
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(NOIR.Application.DependencyInjection).Assembly);
    opts.Discovery.IncludeAssembly(typeof(NOIR.Infrastructure.DependencyInjection).Assembly);

    // Enable FluentValidation middleware - auto-validates all commands/queries
    // Validators are discovered automatically from registered assemblies
    opts.UseFluentValidation();

    // Add logging middleware globally - logs before/after handler execution
    opts.Policies.AddMiddleware<LoggingMiddleware>();

    // Add performance tracking middleware - warns on slow handlers
    opts.Policies.AddMiddleware<PerformanceMiddleware>();

    // Add handler audit middleware - captures handler execution with DTO diff
    opts.Policies.AddMiddleware<HandlerAuditMiddleware>();

    // Code generation mode:
    // - Production: Static (pre-generated handlers for faster cold start)
    // - Development/Testing: Auto (dynamic code generation for rapid iteration)
    //
    // IMPORTANT: Static mode requires pre-generated handler wrapper classes.
    // If Static mode fails with ExpectedTypeMissingException, it means the DI container
    // changed (e.g., new services registered via IScopedService/ITransientService markers)
    // and the handler hash no longer matches. Use Auto mode for development.
    //
    // Note: BuildHost-net472 folders in bin/ are from JasperFx.RuntimeCompiler - safe to ignore
    opts.CodeGeneration.TypeLoadMode = builder.Environment.IsProduction()
        ? TypeLoadMode.Static
        : TypeLoadMode.Auto;
});

// Configure Cookie Settings for dual auth (JWT-in-Cookie support)
builder.Services.AddOptions<CookieSettings>()
    .Bind(builder.Configuration.GetSection(CookieSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register JwtCookieEvents for reading JWT from cookies
builder.Services.AddScoped<JwtCookieEvents>();

// Configure JWT Authentication with cookie support
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };

    // Use custom events to support reading JWT from cookies
    options.EventsType = typeof(JwtCookieEvents);
});

builder.Services.AddAuthorization();

// Add API documentation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "NOIR API";
        document.Info.Version = "v1";
        document.Info.Description = """
            Enterprise-ready .NET SaaS API with JWT authentication.

            ## Authentication

            NOIR supports two authentication methods:

            ### 1. JWT Bearer Token (API Clients)
            Include the JWT token in the Authorization header:
            ```
            Authorization: Bearer <your-jwt-token>
            ```

            ### 2. HttpOnly Cookie (Browser Clients)
            For browser-based applications, use cookie authentication:
            1. Call `POST /api/auth/login?useCookies=true` with credentials
            2. The response sets HttpOnly cookies automatically
            3. All subsequent requests include cookies automatically

            **Login Page:** Visit `/login` to authenticate via the web interface.

            ## Multi-Tenancy

            Include the tenant ID in requests using the header:
            ```
            X-Tenant-Id: <tenant-id>
            ```

            ## Default Credentials

            For development: `admin@noir.local` / `123qwe`
            """;

        return Task.CompletedTask;
    });
});

// Add Health Checks (skip SQL Server check in Testing environment)
var healthChecksBuilder = builder.Services.AddHealthChecks();
if (!builder.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    healthChecksBuilder.AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "database",
        tags: ["db", "sql", "sqlserver"]);
}

var app = builder.Build();

// Seed database
await ApplicationDbContextSeeder.SeedDatabaseAsync(app.Services);

// Configure the HTTP request pipeline
app.UseSerilogRequestLogging();

// Custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Security Headers
app.UseSecurityHeaders();

app.UseHttpsRedirection();

// HSTS - HTTP Strict Transport Security (production only)
// Forces browsers to always use HTTPS for this domain
// Not used in development to allow HTTP debugging
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// CORS (must be before auth and routing)
app.UseCors();

// Serve static files from wwwroot (React SPA build output)
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static assets for 1 year (they have content hashes)
        if (ctx.File.Name.Contains('.') && !ctx.File.Name.EndsWith(".html"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
        }
        else
        {
            // Don't cache HTML files
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        }
    }
});

// Routing must come before rate limiter for endpoint-specific rate limiting to work
app.UseRouting();

// Rate limiting - after routing for endpoint-specific policies ([EnableRateLimiting] attributes)
app.UseRateLimiter();

// Response compression (after rate limiting)
app.UseResponseCompression();

// Output caching (server-side)
app.UseOutputCache();

// API Documentation (available in all environments for now, can restrict later)
// Route: /api/docs for Scalar UI, /api/openapi/v1.json for OpenAPI spec
app.MapOpenApi("/api/openapi/{documentName}.json");
app.MapScalarApiReference("/api/docs", options =>
{
    options
        .WithTitle("NOIR API")
        .WithTheme(ScalarTheme.DeepSpace)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithOpenApiRoutePattern("/api/openapi/{documentName}.json");
    // Use empty servers array so Scalar uses the current request URL (works with Vite proxy on 3000 or direct on 4000)
    options.Servers = [];
});

// Multi-tenant middleware (must be before auth)
app.UseMultiTenant();

// HTTP Request Audit Middleware (captures request/response for audit logging)
// Must be after multi-tenant and before authentication to have tenant context
app.UseMiddleware<HttpRequestAuditMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Map API Endpoints
app.MapAuthEndpoints();
app.MapAuditEndpoints();
app.MapRoleEndpoints();
app.MapTenantEndpoints();
app.MapUserEndpoints();
app.MapEmailTemplateEndpoints();

// Map SignalR Hubs
app.MapHub<AuditHub>("/hubs/audit")
    .RequireAuthorization(policy => policy.RequireClaim(Permissions.ClaimType, Permissions.AuditStream));

// Hangfire Dashboard (requires Admin role in production, skip in Testing)
if (!app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireAuthorizationFilter(app.Environment, app.Configuration, app.Services)]
    });

    // Register Hangfire Recurring Jobs
    var auditRetentionSettings = app.Configuration.GetSection(AuditRetentionSettings.SectionName).Get<AuditRetentionSettings>()
        ?? new AuditRetentionSettings();

    if (auditRetentionSettings.Enabled)
    {
        RecurringJob.AddOrUpdate<AuditRetentionJob>(
            "audit-retention",
            job => job.ExecuteAsync(CancellationToken.None),
            auditRetentionSettings.CronSchedule);
    }
}

// Map Health Checks (under /api to avoid React routing conflicts)
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// SPA Fallback - serve index.html for client-side routing
// Excludes /api/* routes so they return 404 properly
app.MapFallbackToFile("{*path:regex(^(?!api/).*$)}", "index.html");

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }
