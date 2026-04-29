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

// Add HttpClient for webhook delivery
builder.Services.AddHttpClient("WebhookDelivery", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "NOIR-Webhook/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AllowAutoRedirect = false,
    ConnectTimeout = TimeSpan.FromSeconds(10)
});

// Add Application and Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

// Add MCP (Model Context Protocol) server for AI agent integration
builder.Services.AddNoirMcpServer();

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

    // Add feature check middleware - gates commands/queries by [RequiresFeature] attribute
    opts.Policies.AddMiddleware<FeatureCheckMiddleware>();

    // Add logging middleware globally - logs before/after handler execution
    opts.Policies.AddMiddleware<LoggingMiddleware>();

    // Add performance tracking middleware - warns on slow handlers
    opts.Policies.AddMiddleware<PerformanceMiddleware>();

    // Add handler audit middleware - captures handler execution with DTO diff
    opts.Policies.AddMiddleware<HandlerAuditMiddleware>();

    // Code generation mode: Auto everywhere.
    //
    // Auto generates handler wrappers at runtime as needed. Adds a small first-request latency
    // per handler but eliminates the maintenance burden of keeping a pre-gen folder in sync
    // with source. Static mode previously caused ExpectedTypeMissingException 500s in Production
    // whenever a handler was added or changed without re-running `codegen write`.
    //
    // If you ever need Static mode for a Production cold-start optimization, regenerate
    // pre-gen artifacts as a CI step on every build, not by hand.
    opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Auto;
});

// Configure Cookie Settings for dual auth (JWT-in-Cookie support)
builder.Services.AddOptions<CookieSettings>()
    .Bind(builder.Configuration.GetSection(CookieSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register JwtCookieEvents for reading JWT from cookies
builder.Services.AddScoped<JwtCookieEvents>();

// Configure JWT + API Key dual authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services.AddAuthentication(options =>
{
    // Policy scheme dynamically selects JWT or API Key based on request headers
    options.DefaultAuthenticateScheme = "Smart";
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddPolicyScheme("Smart", "JWT or API Key", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // If API Key headers are present and no JWT, use API Key scheme
        if (context.Request.Headers.ContainsKey(NOIR.Web.Authentication.ApiKeyAuthenticationHandler.ApiKeyHeaderName) &&
            context.Request.Headers.ContainsKey(NOIR.Web.Authentication.ApiKeyAuthenticationHandler.ApiSecretHeaderName) &&
            !context.Request.Headers.ContainsKey("Authorization"))
        {
            return NOIR.Web.Authentication.ApiKeyAuthenticationHandler.SchemeName;
        }
        return JwtBearerDefaults.AuthenticationScheme;
    };
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
})
.AddScheme<NOIR.Web.Authentication.ApiKeyAuthenticationOptions, NOIR.Web.Authentication.ApiKeyAuthenticationHandler>(
    NOIR.Web.Authentication.ApiKeyAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

// Add API documentation
builder.Services.AddOpenApi(options =>
{
    // Schema transformer: enrich OpenAPI schemas with FluentValidation constraints
    options.AddSchemaTransformer<NOIR.Web.OpenApi.FluentValidationSchemaTransformer>();

    // Operation transformer: auto-add 401/403/422/429 responses based on endpoint metadata
    options.AddOperationTransformer<NOIR.Web.OpenApi.EndpointMetadataTransformer>();

    // Document transformer: add JWT security scheme, tag ordering, contact/license info
    options.AddDocumentTransformer<NOIR.Web.OpenApi.SecuritySchemeDocumentTransformer>();

    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "NOIR API";
        document.Info.Version = "v1";
        document.Info.Description = """
            Enterprise-ready .NET SaaS API with JWT authentication, API key support, and multi-tenancy.

            ## Authentication

            NOIR supports three authentication methods. Choose based on your use case:

            | Method | Best For | Header |
            |--------|----------|--------|
            | **JWT Bearer** | API clients, SPAs, mobile apps | `Authorization: Bearer <token>` |
            | **API Key + Secret** | External integrations, CI/CD, automation | `X-API-Key` + `X-API-Secret` |
            | **HttpOnly Cookie** | Browser-based web apps | Automatic (set by server) |

            ---

            ### 1. JWT Bearer Token (API Clients)

            **Step 1 — Obtain a token** by calling [`POST /api/auth/login`](#v1/tag/authentication):
            ```json
            POST /api/auth/login
            Content-Type: application/json
            X-Tenant-Id: default

            {
              "email": "admin@noir.local",
              "password": "123qwe"
            }
            ```
            The response includes `accessToken` (60-min TTL) and `refreshToken` (30-day TTL).

            **Step 2 — Use the token** in subsequent requests:
            ```
            Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
            X-Tenant-Id: default
            ```

            **Step 3 — Refresh** when the access token expires via [`POST /api/auth/refresh`](#v1/tag/authentication):
            ```json
            POST /api/auth/refresh
            Content-Type: application/json

            {
              "accessToken": "<expired-token>",
              "refreshToken": "<refresh-token>"
            }
            ```

            **Step 4 — Verify** your identity with [`GET /api/auth/me`](#v1/tag/authentication):
            ```
            GET /api/auth/me
            Authorization: Bearer <token>
            ```

            ---

            ### 2. API Key + API Secret (External Integrations)

            For external systems, automation scripts, CI/CD pipelines, and third-party integrations.
            No login flow required — just include two headers:
            ```
            X-API-Key: noir_key_xxxxxxxx
            X-API-Secret: noir_secret_xxxxxxxx
            ```

            **How to create API keys:**
            1. Log in to the NOIR portal at `/portal/settings?section=api-keys`
            2. Click **Create API Key** — provide a name, select permissions, set optional expiration
            3. Copy the API Key and API Secret (secret is shown **only once**)

            **Key features:**
            - Resolves user identity and tenant automatically — **no `X-Tenant-Id` header needed**
            - Each key has scoped permissions (subset of the creating user's permissions)
            - Keys can be rotated (new secret, same key ID) or revoked instantly
            - Tracks last used timestamp and IP for audit visibility

            ---

            ### 3. HttpOnly Cookie (Browser Clients)

            For browser-based applications (the NOIR portal uses this method):
            1. Call [`POST /api/auth/login?useCookies=true`](#v1/tag/authentication) with credentials
            2. The response sets HttpOnly cookies automatically
            3. All subsequent requests include cookies automatically — no manual header management

            **Login Page:** Visit `/login` to authenticate via the web interface.

            ## Multi-Tenancy

            NOIR is a multi-tenant platform. Include the tenant identifier in requests:
            ```
            X-Tenant-Id: default
            ```
            > **Note:** API Key authentication resolves the tenant automatically. This header is only needed for JWT/Cookie auth.

            ## Default Credentials

            | Account | Email | Password |
            |---------|-------|----------|
            | Platform Admin | `platform@noir.local` | `123qwe` |
            | Tenant Admin | `admin@noir.local` | `123qwe` |

            ## Validation

            All request schemas include validation constraints from FluentValidation.
            Look for `minLength`, `maxLength`, `pattern`, `required`, `minimum`, and `maximum` properties on schema fields.
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

// Inject custom navigation script into Scalar HTML (handles #v1/tag/... anchor links in description)
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api/docs") || context.Request.Path.Value?.EndsWith(".js") == true)
    {
        await next();
        return;
    }

    var originalBody = context.Response.Body;
    using var buffer = new MemoryStream();
    context.Response.Body = buffer;

    await next();

    buffer.Seek(0, SeekOrigin.Begin);
    var html = await new StreamReader(buffer).ReadToEndAsync();
    html = html.Replace("</body>", "<script src=\"/scalar-nav.js\"></script>\n</body>");

    context.Response.Body = originalBody;
    context.Response.ContentLength = null;
    await context.Response.WriteAsync(html);
});

app.MapScalarApiReference("/api/docs", options =>
{
    options
        .WithTitle("NOIR API")
        .WithTheme(ScalarTheme.None)
        .WithFavicon("/favicon.svg")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithOpenApiRoutePattern("/api/openapi/{documentName}.json")
        .DisableDefaultFonts()
        .ForceDarkMode()
        .WithCustomCss("""
            /* ================================================================
             * NOIR Brand Theme for Scalar API Reference
             *
             * Brand palette:
             *   Primary:  Sapphire Blue #2563EB / #60A5FA / #93C5FD
             *   Accent:   Amber Gold    #F59E0B / #FFCB6B / #FCD34D
             *   Surfaces: Blue-tinted   #080810 → #0E0E1A → #131320 → #1A1A2E
             *   Borders:                #282846 (subtle) / #363660 (visible)
             *   Text:                   #F2F2F8 / #A8A8C0 / #6E6E90 / #48485E
             * ================================================================ */

            /* --- Dark mode (forced) ---------------------------------------- */
            .dark-mode,
            :root {
                /* Text hierarchy */
                --scalar-color-1: #F2F2F8;
                --scalar-color-2: #A8A8C0;
                --scalar-color-3: #6E6E90;

                /* Accent — Sapphire Blue */
                --scalar-color-accent: #60A5FA;

                /* Surfaces — blue-tinted darks */
                --scalar-background-1: #080810;
                --scalar-background-2: #0E0E1A;
                --scalar-background-3: #131320;
                --scalar-background-accent: rgba(37, 99, 235, 0.08);

                /* Borders */
                --scalar-border-color: #282846;

                /* Buttons */
                --scalar-button-1: #2563EB;
                --scalar-button-1-hover: #1D4ED8;
                --scalar-button-1-color: #F2F2F8;

                /* Shadows */
                --scalar-shadow-1: 0 1px 3px 0 rgba(0, 0, 0, 0.3);
                --scalar-shadow-2: 0 0 0 0.5px #282846, 0 12px 24px rgba(0, 0, 0, 0.4);

                /* Scrollbar */
                --scalar-scrollbar-color: rgba(168, 168, 192, 0.15);
                --scalar-scrollbar-color-active: rgba(168, 168, 192, 0.3);
            }

            /* --- Sidebar --------------------------------------------------- */
            .dark-mode .sidebar,
            .sidebar {
                --scalar-sidebar-background-1: #0E0E1A;
                --scalar-sidebar-border-color: #282846;
                --scalar-sidebar-color-1: #F2F2F8;
                --scalar-sidebar-color-2: #A8A8C0;
                --scalar-sidebar-color-active: #60A5FA;
                --scalar-sidebar-item-hover-background: #1A1A2E;
                --scalar-sidebar-item-active-background: rgba(37, 99, 235, 0.12);
                --scalar-sidebar-search-background: #131320;
                --scalar-sidebar-search-border-color: #282846;
                --scalar-sidebar-search-color: #6E6E90;
            }

            /* --- Typography ------------------------------------------------ */
            :root {
                --scalar-font: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
                --scalar-font-code: 'JetBrains Mono', 'SF Mono', 'Cascadia Code', 'Fira Code', ui-monospace, monospace;
            }

            /* --- Code blocks — NOIR branded syntax theme ------------------- */
            .dark-mode pre,
            pre {
                background: #0E0E1A !important;
                border: 1px solid #282846;
                border-radius: 8px;
            }

            /* Inline code in markdown descriptions */
            .introduction-description .markdown code:not(pre code),
            .markdown code:not(pre code) {
                background: rgba(37, 99, 235, 0.1);
                color: #93C5FD;
                padding: 0.15em 0.4em;
                border-radius: 4px;
                font-size: 0.875em;
                border: 1px solid rgba(37, 99, 235, 0.15);
            }

            /* --- Links in API description ---------------------------------- */
            .introduction-description .markdown a {
                color: #60A5FA;
                text-decoration: none;
                border-bottom: 1px solid rgba(96, 165, 250, 0.3);
                transition: all 0.15s ease;
            }
            .introduction-description .markdown a:hover {
                color: #93C5FD;
                border-bottom-color: #60A5FA;
            }

            /* --- Tables in description ------------------------------------- */
            .introduction-description .markdown table {
                border-color: #282846;
            }
            .introduction-description .markdown th {
                background: #131320;
                color: #F2F2F8;
                border-color: #282846;
            }
            .introduction-description .markdown td {
                border-color: #282846;
            }

            /* --- HTTP method badges (sidebar + content) -------------------- */
            .scalar-api-reference [data-method="post"],
            .endpoint .post {
                color: #60A5FA;
            }
            .scalar-api-reference [data-method="get"],
            .endpoint .get {
                color: #A5D6A7;
            }
            .scalar-api-reference [data-method="put"],
            .endpoint .put {
                color: #FFCB6B;
            }
            .scalar-api-reference [data-method="delete"],
            .endpoint .delete {
                color: #FF9CAC;
            }

            /* --- Accent hover glow ---------------------------------------- */
            .scalar-api-reference button:focus-visible,
            .scalar-api-reference a:focus-visible {
                outline: 2px solid rgba(96, 165, 250, 0.5);
                outline-offset: 2px;
            }
            """);
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
app.MapApiKeyEndpoints();
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
app.MapFeatureManagementEndpoints();
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
app.MapCustomerEndpoints();
app.MapCustomerGroupEndpoints();
app.MapInventoryEndpoints();
app.MapDashboardEndpoints();
app.MapSearchEndpoints();
app.MapReportEndpoints();
app.MapCheckoutEndpoints();
app.MapShippingProviderEndpoints();
app.MapShippingEndpoints();
app.MapPromotionEndpoints();
app.MapWishlistEndpoints();
app.MapReviewEndpoints();
app.MapWebhookEndpoints();
app.MapSseEndpoints();
app.MapCrmContactEndpoints();
app.MapCrmCompanyEndpoints();
app.MapLeadEndpoints();
app.MapPipelineEndpoints();
app.MapCrmActivityEndpoints();
app.MapEmployeeEndpoints();
app.MapDepartmentEndpoints();
app.MapEmployeeTagEndpoints();
app.MapProjectEndpoints();
app.MapTaskEndpoints();

// Map MCP (Model Context Protocol) server for AI agent integration
// Uses existing auth (JWT + API Key) — AI agents authenticate via X-API-Key + X-API-Secret headers
app.MapMcp("/api/mcp")
    .RequireAuthorization()
    .RequireRateLimiting("fixed");

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

    RecurringJob.AddOrUpdate<CustomerSegmentationJob>(
        "customer-segmentation",
        job => job.ExecuteAsync(CancellationToken.None),
        Cron.Daily(2));
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
