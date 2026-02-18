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

// Create LoggingLevelSwitch for dynamic log level control
var loggingLevelSwitch = new Serilog.Core.LoggingLevelSwitch(Serilog.Events.LogEventLevel.Information);

// Create deferred SignalR sink (configured now, initialized after app.Build())
var deferredSignalRSink = new DeferredSignalRLogSink();

// Configure Serilog with dynamic level control, file logging, and deferred SignalR streaming
builder.Host.UseSerilog((context, services, configuration) =>
{
    var devLogSettings = context.Configuration
        .GetSection(DeveloperLogSettings.SectionName)
        .Get<DeveloperLogSettings>()
        ?? new DeveloperLogSettings();

    // Set initial level from configuration
    if (Enum.TryParse<Serilog.Events.LogEventLevel>(devLogSettings.DefaultMinimumLevel, true, out var initialLevel))
    {
        loggingLevelSwitch.MinimumLevel = initialLevel;
    }

    configuration
        .MinimumLevel.ControlledBy(loggingLevelSwitch)
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Hangfire", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("MachineName", Environment.MachineName)
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console();

    // Add SignalR streaming sink (deferred - initialized after app.Build())
    if (devLogSettings.Enabled && devLogSettings.EnableRealTimeStreaming)
    {
        configuration.WriteTo.DeferredSignalRLogStream(deferredSignalRSink);
    }

    // Add file logging if enabled
    if (devLogSettings.EnableFileLogging)
    {
        var logPath = Path.Combine(context.HostingEnvironment.ContentRootPath, devLogSettings.LogFilePath);
        configuration.WriteTo.File(
            new Serilog.Formatting.Json.JsonFormatter(),
            logPath,
            rollingInterval: Serilog.RollingInterval.Day,
            retainedFileCountLimit: devLogSettings.RetainedFileCountLimit,
            fileSizeLimitBytes: devLogSettings.FileSizeLimitMb * 1024 * 1024,
            rollOnFileSizeLimit: true,
            shared: true);
    }
});

// Register LoggingLevelSwitch as singleton for runtime level control
builder.Services.AddSingleton(loggingLevelSwitch);

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
    // Serialize enums as strings for JavaScript compatibility (consistent with SignalR)
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
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
}).AddJsonProtocol(options =>
{
    // Serialize enums as strings for JavaScript compatibility
    options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Add Application and Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

// Configure Platform settings (for seeding platform admin and default tenant)
builder.Services.Configure<PlatformSettings>(builder.Configuration.GetSection(PlatformSettings.SectionName));

// Configure AuditRetention settings
builder.Services.Configure<AuditRetentionSettings>(builder.Configuration.GetSection(AuditRetentionSettings.SectionName));

// Configure DeveloperLog settings
builder.Services.Configure<NOIR.Infrastructure.Logging.DeveloperLogSettings>(
    builder.Configuration.GetSection(NOIR.Infrastructure.Logging.DeveloperLogSettings.SectionName));

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
    // Add FluentValidation schema transformer to enrich Scalar docs with validation rules
    options.AddSchemaTransformer<NOIR.Web.OpenApi.FluentValidationSchemaTransformer>();

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

            ## Validation

            All request schemas include validation constraints from FluentValidation.
            Look for `minLength`, `maxLength`, `pattern`, and `required` properties.
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

// Initialize deferred SignalR log sink now that service provider is available
// This enables real-time log streaming to the Developer Logs UI
deferredSignalRSink.Initialize(app.Services);

// Seed database
await ApplicationDbContextSeeder.SeedDatabaseAsync(app.Services);

// Configure the HTTP request pipeline
// Log 4xx/5xx responses at ERROR level so they appear in "Errors only" filter
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex is not null) return Serilog.Events.LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 500) return Serilog.Events.LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 400) return Serilog.Events.LogEventLevel.Warning;
        return Serilog.Events.LogEventLevel.Information;
    };
});

// Custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Security Headers
app.UseSecurityHeaders();

// CORS must be before HTTPS redirection so preflight (OPTIONS) responses
// get proper CORS headers before any redirect can intercept them
app.UseCors();

// HTTPS redirection and HSTS only in production
// In development, the Vite proxy handles HTTP→backend communication
// and HTTPS redirect causes cross-origin redirect failures on preflight requests
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

// Serve static files from wwwroot (React SPA build output)
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Development: Disable all caching for easier testing
        if (app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
            return;
        }

        // Production: Cache static assets for 1 year (they have content hashes)
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

// Authentication must run BEFORE multi-tenant for ClaimStrategy to work
// ClaimStrategy needs HttpContext.User populated with JWT claims
app.UseAuthentication();

// Set default tenant for feed routes (RSS, Sitemap) - must run BEFORE multi-tenant
// This allows anonymous access to public feed endpoints by injecting X-Tenant header
app.UseFeedTenantResolver();

// Multi-tenant middleware (reads tenant_id claim from HttpContext.User)
app.UseMultiTenant();

// Load complete user profile from database and cache for request lifetime
// Must run AFTER authentication (needs User.Identity) and AFTER multi-tenant (needs tenant context)
app.UseMiddleware<CurrentUserLoaderMiddleware>();

app.UseAuthorization();

// HTTP Request Audit Middleware (captures request/response for audit logging)
// Must be AFTER authentication to capture user identity, and after multi-tenant for tenant context
app.UseMiddleware<HttpRequestAuditMiddleware>();

// Map API Endpoints
app.MapAuthEndpoints();
app.MapFileEndpoints();
app.MapMediaEndpoints();
app.MapRoleEndpoints();
app.MapPermissionEndpoints();
app.MapTenantEndpoints();
app.MapUserEndpoints();
app.MapEmailTemplateEndpoints();
app.MapLegalPageEndpoints();
app.MapPublicLegalPageEndpoints();
app.MapPlatformSettingsEndpoints();
app.MapTenantSettingsEndpoints();
app.MapNotificationEndpoints();
app.MapAuditEndpoints();
app.MapDeveloperLogEndpoints();
app.MapBlogEndpoints();
app.MapFeedEndpoints();
app.MapPaymentEndpoints();
app.MapProductCategoryEndpoints();
app.MapProductEndpoints();
app.MapProductFilterEndpoints();
app.MapFilterAnalyticsEndpoints();
app.MapBrandEndpoints();
app.MapProductAttributeEndpoints();
app.MapCartEndpoints();
app.MapOrderEndpoints();
app.MapInventoryEndpoints();
app.MapDashboardEndpoints();
app.MapCheckoutEndpoints();
app.MapShippingProviderEndpoints();
app.MapShippingEndpoints();

// Dev-only endpoints for E2E testing (not available in production)
if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}

// Map SignalR Hubs
app.MapHub<NOIR.Infrastructure.Hubs.NotificationHub>("/hubs/notifications");
app.MapHub<NOIR.Infrastructure.Hubs.LogStreamHub>("/hubs/logstream");
app.MapHub<NOIR.Infrastructure.Hubs.PaymentHub>("/hubs/payments");

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
// Liveness probe - simple check that the app is running (no dependencies)
// Use for Kubernetes livenessProbe to restart unhealthy pods
app.MapHealthChecks("/api/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // Don't run any checks - just return healthy
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Readiness probe - checks all dependencies (database)
// Use for Kubernetes readinessProbe to stop traffic to unhealthy pods
app.MapHealthChecks("/api/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Full health check - includes all checks with detailed UI response
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
