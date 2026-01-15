# Cache Busting Best Practices Research

> Research Date: January 2026
> Stack: ASP.NET Core + Vite/React SPA

## Executive Summary

Cache busting ensures users receive the latest version of static assets (JS, CSS, images) while still benefiting from browser caching for performance. The **current NOIR configuration is already well-implemented** with content hashing for production builds. This document covers additional strategies for development and edge cases.

## Current NOIR Implementation (Already Good)

### Vite Build Configuration
Location: `src/NOIR.Web/frontend/vite.config.ts`

```typescript
build: {
  rollupOptions: {
    output: {
      // Content hashes ensure cache busting
      entryFileNames: 'assets/[name].[hash].js',
      chunkFileNames: 'assets/[name].[hash].js',
      assetFileNames: 'assets/[name].[hash].[ext]',
    },
  },
},
```

**What this does:** Every time you build, files with changed content get new hash values in their filenames (e.g., `main.a3f8c2d.js`). Unchanged files keep the same hash, so users only download what changed.

### ASP.NET Core Static File Headers
Location: `src/NOIR.Web/Program.cs`

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Hashed assets: Cache forever (immutable)
        if (ctx.File.Name.Contains('.') && !ctx.File.Name.EndsWith(".html"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control",
                "public, max-age=31536000, immutable");
        }
        else
        {
            // HTML: Never cache
            ctx.Context.Response.Headers.Append("Cache-Control",
                "no-cache, no-store, must-revalidate");
        }
    }
});
```

**What this does:**
- **Hashed JS/CSS/assets**: Cached for 1 year with `immutable` (browser won't even check for updates)
- **HTML files**: Never cached (always fetches fresh `index.html` which references the latest hashed assets)

## Cache Busting Strategies Comparison

| Strategy | Pros | Cons | Best For |
|----------|------|------|----------|
| **Content Hash** (current) | Only changed files redownload | Requires build step | Production |
| **Query String** (`?v=1.0`) | Easy to implement | Some CDNs ignore query strings | Simple deployments |
| **Filename Version** (`app-1.0.js`) | Works everywhere | Manual updates | Legacy systems |
| **asp-append-version** | Auto hash for Razor views | Only for .NET served files | MVC/Razor apps |

## Development-Time Cache Prevention

### Issue
During development with `npm run dev` (Vite dev server), browsers can still cache assets, causing stale content issues.

### Solution 1: Vite Dev Server (Already Working)
Vite's dev server automatically handles HMR (Hot Module Replacement) and doesn't produce hashed files during development. It uses WebSocket connections to push updates instantly.

### Solution 2: Browser DevTools (Manual)
- **Chrome/Edge**: DevTools (F12) → Network tab → Check "Disable cache"
- **Firefox**: DevTools → Settings → "Disable HTTP Cache"

Note: Only works while DevTools is open.

### Solution 3: Development Headers (Optional Enhancement)
For the ASP.NET backend serving files during development, you can conditionally disable caching:

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (app.Environment.IsDevelopment())
        {
            // Development: No caching at all
            ctx.Context.Response.Headers.Append("Cache-Control",
                "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
        else
        {
            // Production: Keep current logic
            // ... existing code ...
        }
    }
});
```

### Solution 4: ASP.NET Core 8+ Built-in Support
ASP.NET Core 8+ automatically disables caching in Development environment. You can verify this is enabled or explicitly configure it:

```json
// appsettings.Development.json
{
  "EnableStaticAssetsDevelopmentCaching": false  // Default is false
}
```

## Common Issues & Fixes

### Problem 1: Users See Old Version After Deployment
**Cause:** `index.html` was cached

**Fix:** Already handled - NOIR sets `no-cache` for HTML files.

### Problem 2: Browser Shows Old JS/CSS
**Cause:** User has old `index.html` pointing to old hashed files

**Fix:** Have users do a hard refresh (Ctrl+Shift+R) or clear cache. Long-term fix is already in place with no-cache on HTML.

### Problem 3: CDN Caching Old Files
**Cause:** CDN edge servers have cached old versions

**Fix:**
1. Purge CDN cache after deployment
2. Use content hash (already implemented) - new hash = new URL = CDN fetch

### Problem 4: Service Worker Caching
**Cause:** PWA service worker serving stale assets

**Fix:** Update service worker versioning or use `skipWaiting()` strategy.

## Additional Recommendations

### 1. Consider Chunk Splitting for Vendor Code
Separate rarely-changing vendor code (React, etc.) from frequently-changing app code:

```typescript
// vite.config.ts
build: {
  rollupOptions: {
    output: {
      manualChunks: {
        vendor: ['react', 'react-dom', 'react-router-dom'],
      },
    },
  },
},
```

**Benefit:** Users only redownload vendor code when dependencies update, not on every app change.

### 2. Add ETag Support (Optional)
For APIs and dynamic content:

```csharp
// Add to Program.cs
app.UseResponseCaching();

// On specific endpoints
[ResponseCache(Duration = 60, VaryByHeader = "Accept-Encoding")]
```

### 3. Consider MapStaticAssets (ASP.NET Core 9+)
Modern alternative that auto-handles ETags and cache headers:

```csharp
// Instead of UseStaticFiles
app.MapStaticAssets();
```

## Testing Cache Busting

### Verify Production Build
```bash
# Build the frontend
cd src/NOIR.Web/frontend
npm run build

# Check output files have hashes
ls ../wwwroot/assets/
# Should see: main.a3f8c2d.js, vendor.b2c4e5f.js, etc.
```

### Verify Response Headers
```bash
# Check headers for a hashed asset
curl -I https://localhost:5001/assets/main.a3f8c2d.js
# Should see: Cache-Control: public, max-age=31536000, immutable

# Check headers for index.html
curl -I https://localhost:5001/index.html
# Should see: Cache-Control: no-cache, no-store, must-revalidate
```

### Browser Network Tab
1. Open DevTools → Network
2. Reload page
3. Check `Cache-Control` header on responses
4. `(from disk cache)` or `(from memory cache)` = cached
5. `200` with content = fresh fetch

## Sources

- [MDN HTTP Caching Guide](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Caching)
- [MDN Cache-Control Header](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Cache-Control)
- [ASP.NET Core Static Files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files)
- [Adding Cache-Control Headers in ASP.NET Core](https://andrewlock.net/adding-cache-control-headers-to-static-files-in-asp-net-core/)
- [ASP.NET Core Tag Helpers - Cache Busting](https://kmatyaszek.github.io/2020/04/08/ASP.NET-Core-Tag-Helpers-Cache-Busting.html)
- [Cache Busting for React Applications](https://maxtsh.medium.com/the-ultimate-guide-to-cache-busting-for-react-production-applications-d583e4248f02)
- [Cache Busting Techniques](https://nestify.io/blog/cache-busting-techniques/)

## Conclusion

**Your current NOIR setup is already following best practices:**
1. Vite content hashing for JS/CSS/assets
2. Long-term caching (1 year + immutable) for hashed assets
3. No-cache for HTML to ensure fresh asset references

**For development convenience**, consider:
1. Using browser DevTools "Disable cache" during development
2. Optionally adding development-only no-cache headers to Program.cs
3. Adding vendor chunk splitting for better production caching efficiency
