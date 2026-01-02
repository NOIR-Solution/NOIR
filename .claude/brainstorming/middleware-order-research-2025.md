# ASP.NET Core Middleware Ordering Best Practices (2024-2025)

**Research Date:** December 30, 2025
**Target Framework:** .NET 8-10
**Focus Areas:** Security middleware (CORS, rate limiting, authentication), response compression
**Status:** ✅ IMPLEMENTED - See `src/NOIR.Web/Program.cs` for implementation

---

## Executive Summary

Microsoft provides official guidance on middleware ordering in ASP.NET Core. The order is **critical** because middleware executes in the sequence it's added to the pipeline for requests, and in **reverse order** for responses. Incorrect ordering can cause security vulnerabilities, performance issues, or runtime errors.

**Key Findings:**
1. **CORS must come BEFORE authentication/authorization** to handle preflight requests correctly
2. **Rate limiting placement depends on strategy**: After `UseRouting()` for endpoint-specific limiting, can be earlier for global limiting
3. **Response compression should be EARLY** in the pipeline, before middleware that compresses responses
4. **Authentication must ALWAYS come before authorization** (non-negotiable)

---

## Official Microsoft Recommended Middleware Order

Based on [Microsoft Learn - ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0), here is the complete recommended order:

### 1. Exception/Error Handling (FIRST)
**Development:**
```csharp
app.UseDeveloperExceptionPage();
app.UseDatabaseErrorPage();
```

**Production:**
```csharp
app.UseExceptionHandler("/Error");
app.UseHsts(); // HTTP Strict Transport Security
```

**Why first?** Must catch all exceptions from downstream middleware.

---

### 2. HTTPS Redirection
```csharp
app.UseHttpsRedirection();
```

**Why early?** Ensures all subsequent middleware work with HTTPS requests.

---

### 3. Static Files
```csharp
app.UseStaticFiles();
```

**Why early?** Short-circuits the pipeline for static assets, avoiding unnecessary processing.

**Security Note:** No authorization checks - files under `wwwroot` are publicly accessible.

---

### 4. Cookie Policy
```csharp
app.UseCookiePolicy();
```

**GDPR compliance** - Must come before session middleware.

---

### 5. Routing
```csharp
app.UseRouting();
```

**Critical position:** Must come BEFORE authentication, authorization, CORS, and rate limiting.

**Note:** In Minimal APIs, `WebApplication` automatically adds `UseRouting()` if endpoints are configured.

---

### 6. CORS (Cross-Origin Resource Sharing)
```csharp
app.UseCors();
```

**CRITICAL ORDERING RULES:**
- Must come **AFTER** `UseRouting()`
- Must come **BEFORE** `UseResponseCaching()` ([GitHub issue #23218](https://github.com/dotnet/aspnetcore/issues/23218))
- Must come **BEFORE** `UseAuthentication()` and `UseAuthorization()`

**Why?** CORS handles browser preflight requests from other origins. If run after authentication/authorization, cross-origin requests get blocked incorrectly.

**Source:** [ASP.NET Core Middleware - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)

---

### 7. Rate Limiting
```csharp
app.UseRateLimiter();
```

**PLACEMENT DEPENDS ON STRATEGY:**

#### Option A: Endpoint-Specific Rate Limiting (Recommended)
```csharp
app.UseRouting();
app.UseCors();
app.UseRateLimiter(); // AFTER UseRouting for endpoint-specific APIs
app.UseAuthentication();
app.UseAuthorization();
```

**When to use:** When using `[EnableRateLimiting]` attributes or `RequireRateLimiting()` on endpoints.

**Official Rule:** `UseRateLimiter` **MUST** be called after `UseRouting` when rate limiting endpoint-specific APIs are used.

**Source:** [Rate limiting middleware in ASP.NET Core - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)

#### Option B: Global Rate Limiting Only
```csharp
app.UseRateLimiter(); // CAN be before UseRouting for global limiters only
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
```

**When to use:** When using only global limiters configured via `GlobalLimiter`.

**Use case:** DDoS protection at the entry point, before any authentication.

---

### 8. Request Localization
```csharp
app.UseRequestLocalization();
```

**Placement:**
- Must come **AFTER** `UseRouting()` when using `RouteDataRequestCultureProvider`
- Must come **BEFORE** any middleware that checks request culture

---

### 9. Authentication
```csharp
app.UseAuthentication();
```

**CRITICAL RULE:** Must come **BEFORE** `UseAuthorization()`.

**Why?** Identifies who the user is before checking permissions.

**Note:** In Minimal APIs, automatically added after `UseRouting()` if `IAuthenticationSchemeProvider` is detected in the service provider.

---

### 10. Authorization
```csharp
app.UseAuthorization();
```

**CRITICAL RULE:** Must come **IMMEDIATELY AFTER** `UseAuthentication()`.

**Why?** Authorization relies on authenticated user identity to decide access rights.

---

### 11. Session
```csharp
app.UseSession();
```

**Placement:**
- Must come **AFTER** `UseCookiePolicy()`
- Must come **BEFORE** endpoint mapping

---

### 12. Response Compression
```csharp
app.UseResponseCompression();
```

**CRITICAL RULE:** Must be called **BEFORE** any middleware that compresses responses.

**Why early?** Needs to intercept responses before other middleware processes them.

**Configuration Note:** Response compression is **NOT** enabled for HTTPS by default. Must set `EnableForHttps = true`.

**Source:** [Response compression in ASP.NET Core - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-10.0)

---

### 13. Response Caching
```csharp
app.UseResponseCaching();
```

**Ordering with Compression:**

**Option 1:** Cache compressed responses (reduced CPU usage)
```csharp
app.UseResponseCompression();
app.UseResponseCaching();
```

**Option 2:** Cache then compress
```csharp
app.UseResponseCaching();
app.UseResponseCompression();
```

**Note:** Both orderings are valid; choice is scenario-specific.

**CRITICAL:** `UseCors()` must come **BEFORE** `UseResponseCaching()`.

---

### 14. Endpoints (LAST)
```csharp
app.MapRazorPages();
app.MapControllers();
app.MapDefaultControllerRoute();
```

**Why last?** Terminal middleware that executes the actual endpoint logic.

**Note:** In Minimal APIs, `WebApplication` automatically adds `UseEndpoints()` at the end if endpoints are configured.

---

## Complete Example for NOIR Project

Based on the NOIR project requirements (multi-tenant SaaS with authentication, rate limiting, and API-first design):

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* JWT config */);

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddRateLimiter(options =>
{
    // Rate limiting configuration
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.AddControllers();

var app = builder.Build();

// ===== MIDDLEWARE PIPELINE ORDER =====

// 1. Exception Handling (FIRST)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/api/error");
    app.UseHsts();
}

// 2. HTTPS Redirection
app.UseHttpsRedirection();

// 3. Static Files (for React SPA)
app.UseStaticFiles();

// 4. Response Compression (EARLY)
app.UseResponseCompression();

// 5. Routing
app.UseRouting();

// 6. CORS (AFTER routing, BEFORE auth)
app.UseCors();

// 7. Rate Limiting (AFTER routing for endpoint-specific)
app.UseRateLimiter();

// 8. Authentication (BEFORE authorization)
app.UseAuthentication();

// 9. Authorization (AFTER authentication)
app.UseAuthorization();

// 10. Response Caching (OPTIONAL, after CORS)
// app.UseResponseCaching();

// 11. Endpoints (LAST)
app.MapControllers();
app.MapFallbackToFile("index.html"); // React SPA fallback

app.Run();
```

---

## Security Middleware - Detailed Answers

### Question 1: Correct Order for Security Middleware

**Official Microsoft Order:**
```csharp
app.UseRouting();
app.UseCors();           // 1. CORS
app.UseRateLimiter();    // 2. Rate Limiting (for endpoint-specific)
app.UseAuthentication(); // 3. Authentication
app.UseAuthorization();  // 4. Authorization
```

**Sources:**
- [ASP.NET Core Middleware - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)
- [Rate limiting middleware - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [Correct Order for CORS, Authentication, and Authorization - C# Corner](https://www.c-sharpcorner.com/article/correct-order-for-cors-authentication-and-authorization-in-asp-net-core/)

---

### Question 2: Rate Limiter Placement Relative to CORS and Authentication

**Answer:** **AFTER CORS, BEFORE or AFTER Authentication** (depends on use case)

#### Strategy A: Rate Limit After Authentication (Per-User Limiting)
```csharp
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();     // Rate limit based on user identity
app.UseAuthorization();
```

**Use case:** Different rate limits per subscription tier (requires user identity).

**How it works:** Rate limiter retrieves user ID from authentication claims and applies user-specific limits.

**Source:** [Rate limiting middleware samples - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit-samples?view=aspnetcore-9.0)

#### Strategy B: Rate Limit Before Authentication (DDoS Protection)
```csharp
app.UseRouting();
app.UseCors();
app.UseRateLimiter();     // Rate limit by IP address
app.UseAuthentication();
app.UseAuthorization();
```

**Use case:** Protect authentication endpoints from brute-force attacks or DDoS.

**How it works:** Rate limiter uses IP address or anonymous partition key.

**Key Rule:** Must come **AFTER** `UseRouting()` when using endpoint-specific rate limiting attributes like `[EnableRateLimiting]`.

**Official Quote:**
> "UseRateLimiter must be called after UseRouting when rate limiting endpoint specific APIs are used. For example, if the [EnableRateLimiting] attribute is used, UseRateLimiter must be called after UseRouting."

**Source:** [Rate limiting middleware in ASP.NET Core - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)

---

### Question 3: Response Compression Placement

**Answer:** **EARLY in the pipeline, BEFORE any middleware that compresses responses**

**Recommended Position:**
```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseResponseCompression(); // EARLY, before routing
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
```

**Why early?** Response compression needs to intercept responses as they flow back through the middleware stack.

**Official Quote:**
> "app.UseResponseCompression must be called before any middleware that compresses responses."

**Important Configuration:**
```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // NOT enabled by default for HTTPS
});
```

**Ordering with Caching:**

Both orderings are valid (scenario-specific):

**Option 1:** Compression → Caching (cache compressed responses, reduce CPU)
```csharp
app.UseResponseCompression();
app.UseResponseCaching();
```

**Option 2:** Caching → Compression
```csharp
app.UseResponseCaching();
app.UseResponseCompression();
```

**Sources:**
- [Response compression in ASP.NET Core - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-10.0)
- [ASP.NET Core Middleware - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)

---

## Key Principles to Remember

1. **Exception handlers go FIRST** - Must catch all downstream exceptions
2. **Authentication before Authorization** - Non-negotiable order
3. **CORS before Authentication** - Handles preflight requests correctly
4. **UseRouting before endpoint-specific middleware** - Rate limiting, CORS need routing context
5. **Static files early** - Avoid unnecessary processing for assets
6. **Response compression early** - Must intercept responses before other middleware
7. **No response writes after next()** - Causes exceptions/protocol violations
8. **Terminal middleware short-circuits** - Prevents further downstream processing

---

## Common Mistakes to Avoid

### Mistake 1: Authorization Before Authentication
```csharp
// ❌ WRONG
app.UseAuthorization();
app.UseAuthentication();
```

**Impact:** Authorization fails because user identity is not established.

### Mistake 2: CORS After Authentication
```csharp
// ❌ WRONG
app.UseAuthentication();
app.UseCors();
```

**Impact:** Browser preflight requests get blocked before CORS headers are added.

**Source:** [Medium - ASP.NET Core in .NET 9 Middleware Order](https://medium.com/@vivek-baliyan/asp-net-core-in-net-9-middleware-order-the-setup-that-actually-works-0e02e690d270)

### Mistake 3: Rate Limiting Before Routing (with endpoint-specific APIs)
```csharp
// ❌ WRONG (when using [EnableRateLimiting])
app.UseRateLimiter();
app.UseRouting();
```

**Impact:** Endpoint-specific rate limiting attributes don't work.

### Mistake 4: Response Compression After Endpoints
```csharp
// ❌ WRONG
app.MapControllers();
app.UseResponseCompression();
```

**Impact:** Compression never executes because endpoints are terminal middleware.

### Mistake 5: CORS Before Routing
```csharp
// ❌ WRONG
app.UseCors();
app.UseRouting();
```

**Impact:** CORS policies may not apply correctly to routed endpoints.

---

## Special Considerations for NOIR Project

### Multi-Tenancy with Finbuckle
```csharp
// Finbuckle multi-tenancy should come AFTER routing
app.UseRouting();
app.UseMultiTenant(); // Finbuckle middleware
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
```

**Why?** Tenant resolution may depend on route data.

### API-First Design with `/api` Prefix
All endpoints under `/api` prefix work seamlessly with the recommended middleware order. No special considerations needed.

### JWT Authentication
JWT authentication works best with rate limiting **AFTER** authentication to enable per-user rate limiting based on JWT claims.

```csharp
app.UseRouting();
app.UseCors();
app.UseAuthentication(); // Extract JWT claims
app.UseRateLimiter();     // Rate limit based on 'sub' claim
app.UseAuthorization();
```

---

## Automatic Middleware in Minimal APIs

**WebApplication** automatically adds middleware if user code doesn't explicitly call them:

1. **UseDeveloperExceptionPage** - Added first when `HostingEnvironment` is "Development"
2. **UseRouting** - Added if endpoints are configured and user didn't call `UseRouting()`
3. **UseEndpoints** - Added at the end if endpoints are configured
4. **UseAuthentication** - Added immediately after `UseRouting` if `IAuthenticationSchemeProvider` is detected in the service provider

**Source:** [Middleware with Minimal API applications - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/middleware?view=aspnetcore-9.0)

**Best Practice:** Explicitly call middleware methods to maintain clarity and control.

---

## Additional Resources

### Official Microsoft Documentation
- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)
- [Rate limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [Response compression](https://learn.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-10.0)
- [Middleware with Minimal API applications](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/middleware?view=aspnetcore-9.0)

### Community Resources
- [Medium - ASP.NET Core in .NET 9 Middleware Order](https://medium.com/@vivek-baliyan/asp-net-core-in-net-9-middleware-order-the-setup-that-actually-works-0e02e690d270)
- [C# Corner - Correct Order for CORS, Authentication, and Authorization](https://www.c-sharpcorner.com/article/correct-order-for-cors-authentication-and-authorization-in-asp-net-core/)
- [Dev Leader - Ultimate Starter Guide to Middleware](https://www.devleader.ca/2024/02/01/ultimate-starter-guide-to-middleware-in-asp-net-core-everything-you-need-to-know/)

### GitHub Issues
- [GitHub #23218 - UseCors must appear before UseResponseCaching](https://github.com/dotnet/aspnetcore/issues/23218)
- [GitHub #63082 - Minimal API middleware order is confusing](https://github.com/dotnet/aspnetcore/issues/63082)

---

## Conclusion

The correct middleware order for ASP.NET Core applications, especially for NOIR project requirements:

```csharp
// Exception handling
app.UseExceptionHandler("/api/error");
app.UseHsts();

// HTTPS & Static Files
app.UseHttpsRedirection();
app.UseStaticFiles();

// Response compression (early)
app.UseResponseCompression();

// Routing
app.UseRouting();

// Security middleware (in order)
app.UseCors();           // 1. CORS first
app.UseRateLimiter();    // 2. Rate limiting (after routing)
app.UseAuthentication(); // 3. Authentication
app.UseAuthorization();  // 4. Authorization

// Endpoints (last)
app.MapControllers();
```

This order ensures:
- Security middleware executes in the correct sequence
- CORS preflight requests are handled properly
- Rate limiting works with endpoint-specific attributes
- Authentication happens before authorization
- Response compression applies to all responses

**All recommendations based on official Microsoft documentation current as of December 2025.**
