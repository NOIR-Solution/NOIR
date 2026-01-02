# Content-Security-Policy Best Practices for .NET APIs (2024-2025)

**Research Date:** 2025-12-30
**Context:** NOIR Project - Enterprise .NET 10 + React SaaS with Scalar API Documentation
**Status:** ✅ IMPLEMENTED - Path-specific CSP in `src/NOIR.Web/Middleware/SecurityHeadersMiddleware.cs`

---

## Executive Summary

**Key Findings:**
1. **Pure JSON APIs don't strictly need CSP** - but OWASP recommends it for defense-in-depth
2. **Recommended minimal CSP for APIs:** `Content-Security-Policy: default-src 'none'; frame-ancestors 'none';`
3. **API documentation tools (Swagger/Scalar) DO need CSP** with either nonces or `unsafe-inline`
4. **'unsafe-inline' is NOT acceptable** for production - use nonce-based approach instead

---

## 1. Should APIs Have CSP Headers at All?

### OWASP Official Guidance

According to the [OWASP REST Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html):

> "Security headers are only intended to provide additional security when responses are rendered as HTML. As such, if the API will never return HTML in responses, then these headers may not be necessary. **However, if there is any uncertainty about the function of the headers, or the types of information that the API returns (or may return in future), then it is recommended to include them as part of a defence-in-depth approach.**"

The [OWASP HTTP Headers Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html) states:

> "CSP header is relevant to be applied in pages which can load and interpret scripts and code, but **might be meaningless in the response of a REST API that returns content that is not going to be rendered**."

### When CSP Becomes Relevant for APIs

**CSP is relevant when:**
- API returns any HTML content (error pages, documentation)
- API serves Swagger/Scalar/OpenAPI documentation
- Uncertainty exists about future response types
- Defense-in-depth strategy is desired

**CSP may not be necessary when:**
- API exclusively returns JSON with `application/json` content-type
- No HTML rendering ever occurs
- No documentation UI is served

### The "JSON Alone Won't Save You" Problem

From [XSS in APIs: Why JSON Alone Won't Save You](https://medium.com/@goguzgungor59/xss-in-apis-why-json-alone-wont-save-you-a3a8faa7a3b7):

> "One of the most dangerous misconceptions in API development is: 'I return JSON, so XSS isn't my problem.' XSS risks emerge at three different points: Your API might occasionally return HTML error pages. When your framework's default error handler kicks in, what happens? Often, it returns an error page with text/html content type."

**Recommendation:** Even for JSON-only APIs, implement CSP as a safety net against framework default behaviors and future changes.

---

## 2. Best CSP Configuration for API-First Architecture

### Minimal Secure Configuration (JSON-Only APIs)

```http
Content-Security-Policy: default-src 'none'; frame-ancestors 'none';
```

**Rationale:**
- `default-src 'none'` - Most restrictive, blocks all resource loading by default
- `frame-ancestors 'none'` - Prevents API responses from being embedded in iframes (similar to `X-Frame-Options: DENY`)

From [API Security HTTP Headers - VulnAPI Documentation](https://vulnapi.cerberauth.com/docs/best-practices/security-headers):

> "For APIs, a recommended CSP policy is: `Content-Security-Policy: default-src 'none'; frame-ancestors 'none';` - the frame-ancestors 'none' ensures that the API response cannot be embedded in iframes, preventing clickjacking attacks."

### Why frame-ancestors Matters for APIs

From [frame-ancestors in CSP](https://content-security-policy.com/frame-ancestors/):

> "Even though API responses are not intended to be rendered in frames, nothing prevents that from happening. To ensure API responses are not vulnerable to obscure attacks, it is recommended to set both framing headers on each API response as well."

**Important:** `frame-ancestors` does NOT inherit from `default-src` and must be explicitly specified. It also cannot be set via `<meta>` tag - only via HTTP header.

### Defense-in-Depth Configuration

```http
Content-Security-Policy: default-src 'none';
                         frame-ancestors 'none';
                         base-uri 'self';
                         form-action 'self';
```

Additional directives:
- `base-uri 'self'` - Restricts `<base>` element URLs
- `form-action 'self'` - Restricts form submission targets
- `upgrade-insecure-requests` - Forces HTTPS (optional)

---

## 3. CSP for APIs with Swagger/Scalar Documentation

### The Challenge

Both Swagger UI and Scalar use inline `<script>` and `<style>` tags, which violate strict CSP policies:

- Swagger UI uses inline scripts and styles extensively
- Scalar loads assets from CDN (`cdn.jsdelivr.net`) by default
- Without proper CSP configuration, documentation pages appear blank

### Swagger UI CSP Issues

From [Swagger-ui requires 'unsafe-eval' in CSP Headers (Issue #5817)](https://github.com/swagger-api/swagger-ui/issues/5817):

> "Swagger-ui uses inline `<script>` and `<style>` tags, which are considered insecure as they can allow attackers to run arbitrary code in an application through a cross-site scripting (XSS) vulnerability."

Error when CSP is enabled:
```
Refused to execute inline script because it violates the following
Content Security Policy directive: 'script-src self'. Either the
'unsafe-inline' keyword, a hash, or a nonce is required to enable
inline execution.
```

### Scalar Documentation CSP Issues

From [Scalar API Documentation - Modern Alternative to Swagger UI for .NET](https://www.mykolaaleksandrov.dev/posts/2025/11/scalar-api-documentation/):

**Symptoms:**
- Navigating to `/scalar/v1` shows blank white page or loading spinner
- Browser console shows CSP violations

**Scalar-Specific Considerations:**
- Assets loaded from CDN by default: `https://cdn.jsdelivr.net/npm/@scalar/api-reference`
- Fonts loaded from Google Fonts CDN by default
- Can disable CDN fonts with `DisableDefaultFonts()`

### Solution 1: Nonce-Based Approach (RECOMMENDED)

**Implementation using NetEscapades.AspNetCore.SecurityHeaders:**

```csharp
// Install: NetEscapades.AspNetCore.SecurityHeaders
// Install: NetEscapades.AspNetCore.SecurityHeaders.TagHelpers

// Program.cs
app.UseSecurityHeaders(policies =>
{
    policies.AddContentSecurityPolicy(builder =>
    {
        builder.AddDefaultSrc().Self();
        builder.AddScriptSrc()
            .Self()
            .WithNonce()  // Enable nonce for scripts
            .From("https://cdn.jsdelivr.net");  // Scalar CDN
        builder.AddStyleSrc()
            .Self()
            .WithNonce()  // Enable nonce for styles
            .From("https://cdn.jsdelivr.net");  // Scalar CDN
        builder.AddFontSrc()
            .Self()
            .From("https://fonts.googleapis.com")
            .From("https://fonts.gstatic.com");
        builder.AddImgSrc()
            .Self()
            .Data();  // For data: URIs
        builder.AddFrameAncestors().None();
    });
});

// For Swagger/Scalar custom index.html
services.AddHttpContextAccessor();

services
    .AddOptions<SwaggerUIOptions>()
    .Configure<IHttpContextAccessor>((options, httpContextAccessor) =>
    {
        var originalIndexStreamFactory = options.IndexStream;

        options.IndexStream = () =>
        {
            using var originalStream = originalIndexStreamFactory();
            using var reader = new StreamReader(originalStream);
            var contents = reader.ReadToEnd();

            var nonce = httpContextAccessor.HttpContext.GetNonce();
            var modified = contents
                .Replace("<script>", $"<script nonce=\"{nonce}\">",
                    StringComparison.OrdinalIgnoreCase)
                .Replace("<style>", $"<style nonce=\"{nonce}\">",
                    StringComparison.OrdinalIgnoreCase);

            return new MemoryStream(Encoding.UTF8.GetBytes(modified));
        };
    });
```

**Using Joonasw.AspNetCore.SecurityHeaders (Alternative):**

```csharp
// Install: Joonasw.AspNetCore.SecurityHeaders

app.UseCsp(csp =>
{
    csp.AllowScripts
        .FromSelf()
        .From("https://cdn.jsdelivr.net")
        .AddNonce();

    csp.AllowStyles
        .FromSelf()
        .From("https://cdn.jsdelivr.net")
        .AddNonce();

    csp.AllowFonts
        .FromSelf()
        .From("https://fonts.googleapis.com")
        .From("https://fonts.gstatic.com");
});
```

Then in `_ViewImports.cshtml`:
```cshtml
@addTagHelper *, Joonasw.AspNetCore.SecurityHeaders
```

And mark elements:
```html
<script asp-add-nonce="true">
    // Your inline script
</script>

<style asp-add-nonce="true">
    /* Your inline styles */
</style>
```

**How Nonces Work:**
1. Generate cryptographic nonce per request (256-bit / 32 bytes recommended)
2. Add nonce to CSP header: `script-src 'nonce-{base64Value}'`
3. Add same nonce to inline `<script>` and `<style>` tags
4. Browser only executes inline code with correct nonce
5. Nonce changes on every request (cannot be cached)

From [Content Security Policy (CSP) in ASP.NET Core](https://joonasw.net/view/csp-in-aspnet-core):

> "One caveat: you can't cache all HTML output when using nonces since they are generated per-request."

### Solution 2: Hash-Based Approach

For static inline scripts/styles that don't change:

```csharp
builder.AddScriptSrc()
    .Self()
    .WithHash256("base64-encoded-sha256-hash");
```

Calculate hash:
```bash
echo -n "console.log('Hello');" | openssl dgst -sha256 -binary | openssl base64
```

**Pros:** Can cache HTML
**Cons:** Must recalculate hashes when scripts change

### Solution 3: 'unsafe-inline' (NOT RECOMMENDED)

```http
Content-Security-Policy: default-src 'self';
                         script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net;
                         style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net;
```

⚠️ **This defeats most CSP security benefits** - see next section.

### CSP Configuration for Scalar Specifically

```csharp
// Program.cs - .NET 9+
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

// Configure Scalar
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("NOIR API")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

    // Optional: Self-host assets to avoid CDN CSP issues
    // options.WithBundleUrl("/scalar-assets/api-reference.js");

    // Optional: Disable CDN fonts
    // options.DisableDefaultFonts();
});
```

---

## 4. 'unsafe-inline' for Styles - Is It Acceptable?

### Short Answer: NO

From [unsafe-inline CSP Guide](https://content-security-policy.com/unsafe-inline/):

> "The `unsafe-inline` keyword **annuls most of the security benefits** that Content-Security-Policy provides. Disallowing inline styles and inline scripts is **one of the biggest security wins CSP provides**."

From [Stay safe, no more unsafe-inline](https://centralcsp.com/articles/unsafe-inline):

> "Allowing inline scripts via 'unsafe-inline' opens the door for attackers to inject malicious code into your website, compromising user data and session integrity. The use of 'unsafe-inline' significantly diminishes the effectiveness of CSP, which is designed to block unauthorized scripts."

### When 'unsafe-inline' Might Be Tolerable (With Caveats)

From [Understanding and Mitigating Unsafe Inline](https://www.browserstack.com/guide/unsafe-inline):

> "It is only ok to use `unsafe-inline` when it is combined with the `strict-dynamic` CSP directive. On browsers that support `strict-dynamic` (CSP Level 3+), the `unsafe-inline` is ignored, and provides a route to backwards compatibility on browsers that support CSP Level 2 or lower."

**Example with strict-dynamic:**
```http
Content-Security-Policy: script-src 'nonce-{random}' 'strict-dynamic' 'unsafe-inline' https:;
                         object-src 'none';
                         base-uri 'none';
```

- Modern browsers (CSP Level 3): Use nonce, ignore `unsafe-inline`
- Older browsers (CSP Level 2): Fall back to `unsafe-inline`
- Oldest browsers (CSP Level 1): Allow HTTPS scripts

### Nonce vs. unsafe-inline

From [CSP Allow Inline Styles](https://content-security-policy.com/examples/allow-inline-style/):

> "When you want to allow inline scripts or styles on a page that uses CSP, there are two much better options: **nonce or hash**. Using nonce attribute is a safe and recommended way to bypass CSP restrictions for inline styles, as it does not compromise the security of your application."

**Browser behavior with nonces:**
> "If a directive contains a nonce and `unsafe-inline`, then the browser ignores `unsafe-inline`."

### The XSS Protection Argument

From [OWASP Content Security Policy Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Content_Security_Policy_Cheat_Sheet.html):

> "CSP should not be relied upon as the only defensive mechanism against XSS. You must still follow good development practices and then deploy CSP on top of that as a **bonus security layer**. A strong CSP provides an effective second layer of protection against various types of vulnerabilities, especially XSS."

**Key Point:** While CSP is not a silver bullet, using `unsafe-inline` removes a critical layer of defense against XSS attacks.

### Recommended Best Practices

From [OWASP Content Security Policy Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Content_Security_Policy_Cheat_Sheet.html):

1. **Avoid `unsafe-inline` and `unsafe-eval`:** These values reduce security by allowing inline scripts and dynamic code execution. If necessary, use nonces or hashes for inline scripts.

2. **Use nonces for inline resources:** Generate a random value for each request and apply it to both the CSP header and inline tags.

3. **Never use `unsafe-inline` in production environments** unless absolutely necessary with `strict-dynamic` fallback.

---

## Specific Recommendations for NOIR Project

### Current Setup
- .NET 10 Web API with Scalar documentation (`/api/docs`)
- React SPA will be hosted by same .NET app
- API endpoints under `/api` prefix
- JWT authentication with `/api/auth/*` endpoints

### Recommended CSP Strategy

#### Phase 1: API-Only (Current State)

**For API endpoints (`/api/*`):**
```csharp
// Apply strict CSP to all API responses
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        !context.Request.Path.StartsWithSegments("/api/docs"))
    {
        context.Response.Headers.Append(
            "Content-Security-Policy",
            "default-src 'none'; frame-ancestors 'none';"
        );
    }

    await next();
});
```

**For Scalar documentation (`/api/docs`):**
```csharp
// Install: NetEscapades.AspNetCore.SecurityHeaders
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/docs"),
    appBuilder =>
    {
        appBuilder.UseSecurityHeaders(policies =>
        {
            policies.AddContentSecurityPolicy(builder =>
            {
                builder.AddDefaultSrc().Self();
                builder.AddScriptSrc()
                    .Self()
                    .WithNonce()
                    .From("https://cdn.jsdelivr.net");
                builder.AddStyleSrc()
                    .Self()
                    .WithNonce()
                    .From("https://cdn.jsdelivr.net");
                builder.AddFontSrc()
                    .Self()
                    .From("https://fonts.googleapis.com")
                    .From("https://fonts.gstatic.com");
                builder.AddImgSrc()
                    .Self()
                    .Data();
                builder.AddFrameAncestors().None();
            });
        });
    }
);
```

#### Phase 2: React SPA Integration

**For React app routes (not `/api/*`):**
```csharp
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    appBuilder =>
    {
        appBuilder.UseSecurityHeaders(policies =>
        {
            policies.AddContentSecurityPolicy(builder =>
            {
                builder.AddDefaultSrc().Self();
                builder.AddScriptSrc()
                    .Self()
                    .WithNonce();  // React will need nonces for inline scripts
                builder.AddStyleSrc()
                    .Self()
                    .WithNonce();  // React CSS-in-JS needs nonces
                builder.AddConnectSrc()
                    .Self();  // Allow API calls to same origin
                builder.AddImgSrc()
                    .Self()
                    .Data()
                    .From("https:");  // Allow external images if needed
                builder.AddFontSrc()
                    .Self()
                    .Data();
                builder.AddFrameAncestors().None();
            });
        });
    }
);
```

### Libraries to Use

**Option 1: NetEscapades.AspNetCore.SecurityHeaders (RECOMMENDED)**
- License: MIT
- Stars: ~950 on GitHub
- Last updated: 2024
- Author: Andrew Lock (well-known .NET blogger)
- Features: Fluent API, automatic nonce generation, tag helpers

```bash
dotnet add package NetEscapades.AspNetCore.SecurityHeaders
dotnet add package NetEscapades.AspNetCore.SecurityHeaders.TagHelpers
```

**Option 2: Joonasw.AspNetCore.SecurityHeaders**
- License: MIT
- Features: Simple fluent API, good for basic needs
- Smaller community than NetEscapades

```bash
dotnet add package Joonasw.AspNetCore.SecurityHeaders
```

**Option 3: NWebsec**
- License: Apache 2.0
- Features: Comprehensive security headers, mature library
- More complex API

```bash
dotnet add package NWebsec.AspNetCore.Middleware
```

### Testing CSP Configuration

1. **Browser DevTools:**
   - Open Console tab
   - CSP violations appear as errors with detailed information

2. **CSP Evaluator (Google):**
   - https://csp-evaluator.withgoogle.com/
   - Paste CSP header for security analysis

3. **Report-URI Service:**
   - Add `report-uri` directive to collect violation reports
   - Free tier available at https://report-uri.com/

```http
Content-Security-Policy: default-src 'self'; report-uri https://yourapp.report-uri.com/r/d/csp/enforce;
```

---

## Summary of Best Practices

### For JSON-Only API Endpoints
✅ **DO:**
- Use `default-src 'none'; frame-ancestors 'none';`
- Consider defense-in-depth even if "unnecessary"
- Ensure `Content-Type: application/json` is always set

❌ **DON'T:**
- Skip CSP entirely (framework errors may return HTML)
- Use `unsafe-inline` or `unsafe-eval`

### For API Documentation (Swagger/Scalar)
✅ **DO:**
- Implement nonce-based CSP
- Whitelist specific CDN domains if needed
- Use security headers library for automatic nonce generation
- Test thoroughly in browser console

❌ **DON'T:**
- Use `unsafe-inline` in production
- Allow `unsafe-eval` (not needed for Scalar)
- Forget about `frame-ancestors`

### For React SPA (Future)
✅ **DO:**
- Use nonce-based CSP for inline scripts/styles
- Configure `connect-src` for API calls
- Consider using build-time hash generation
- Test with React dev tools

❌ **DON'T:**
- Use `unsafe-inline` without `strict-dynamic` fallback
- Block same-origin API calls with overly strict CSP
- Forget to update CSP when adding third-party scripts

---

## References

### OWASP Official Resources
- [OWASP Content Security Policy Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Content_Security_Policy_Cheat_Sheet.html)
- [OWASP REST Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html)
- [OWASP HTTP Headers Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Headers_Cheat_Sheet.html)
- [OWASP Secure Headers Project](https://owasp.org/www-project-secure-headers/)

### Technical Documentation
- [MDN: Content-Security-Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CSP)
- [MDN: CSP script-src directive](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/script-src)
- [MDN: CSP style-src directive](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/style-src)
- [MDN: CSP frame-ancestors directive](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/frame-ancestors)

### CSP Guides and Tools
- [Content-Security-Policy.com - Quick Reference](https://content-security-policy.com/)
- [unsafe-inline CSP Guide](https://content-security-policy.com/unsafe-inline/)
- [frame-ancestors in CSP](https://content-security-policy.com/frame-ancestors/)
- [CSP Allow Inline Styles](https://content-security-policy.com/examples/allow-inline-style/)
- [Stay safe, no more unsafe-inline](https://centralcsp.com/articles/unsafe-inline)

### Implementation Guides
- [Content Security Policy (CSP) in ASP.NET Core - Joonas W](https://joonasw.net/view/csp-in-aspnet-core)
- [How to lock down your CSP when using Swashbuckle - Mickaël Derriey](https://mderriey.com/2020/12/14/how-to-lock-down-csp-using-swachbuckle/)
- [Scalar API Documentation - Modern Alternative to Swagger UI](https://www.mykolaaleksandrov.dev/posts/2025/11/scalar-api-documentation/)
- [Content-Security-Policy with Swagger UI (GitHub Gist)](https://gist.github.com/m-lukas/5bbf98e0dbade2d9f9428956512bdd31)

### Security Articles
- [XSS in APIs: Why JSON Alone Won't Save You](https://medium.com/@goguzgungor59/xss-in-apis-why-json-alone-wont-save-you-a3a8faa7a3b7)
- [Understanding and Mitigating Unsafe Inline - BrowserStack](https://www.browserstack.com/guide/unsafe-inline)
- [API Security HTTP Headers - VulnAPI Documentation](https://vulnapi.cerberauth.com/docs/best-practices/security-headers)

### .NET Specific Resources
- [NetEscapades.AspNetCore.SecurityHeaders - GitHub](https://github.com/andrewlock/NetEscapades.AspNetCore.SecurityHeaders)
- [Joonasw.AspNetCore.SecurityHeaders - GitHub](https://github.com/juunas11/aspnetcore-security-headers)
- [NWebsec Documentation](https://nwebsec.readthedocs.io/en/latest/nwebsec/Configuring-csp.html)
- [Microsoft: Enforce CSP for Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/content-security-policy?view=aspnetcore-10.0)

### Scalar Documentation
- [Scalar API Reference for .NET ASP.NET Core](https://guides.scalar.com/scalar/scalar-api-references/integrations/net-aspnet-core/integration)
- [GitHub: scalar/scalar](https://github.com/scalar/scalar)
- [How .NET 9 and Scalar solve API documentation](https://blog.scalar.com/p/how-net-9-and-scalar-solve-the-problem)

### GitHub Issues
- [Swagger-ui requires 'unsafe-eval' in CSP (Issue #5817)](https://github.com/swagger-api/swagger-ui/issues/5817)

---

## Conclusion

**For NOIR Project:**

1. **API endpoints** - Use strict CSP: `default-src 'none'; frame-ancestors 'none';`
2. **Scalar documentation** - Implement nonce-based CSP with `NetEscapades.AspNetCore.SecurityHeaders`
3. **Future React SPA** - Plan for nonce-based CSP with appropriate directives
4. **NEVER use `unsafe-inline`** in production without `strict-dynamic` fallback

CSP is a powerful defense-in-depth mechanism that should be implemented from day one, even for JSON-only APIs. The investment in nonce-based configuration pays off in security resilience against XSS and injection attacks.
