# NOIR Feature Roadmap - Comprehensive Implementation Plan

> **Status:** Approved - Ready for Implementation
> **Author:** Claude AI Assistant
> **Date:** January 2026
> **Estimated Phases:** 5

### Decisions Made

| Question | Decision |
|----------|----------|
| Caching | In-memory with Redis-ready interface |
| Image Variants | thumb, sm, md, lg, xl (1920), original (2560) |
| Schema.org JSON-LD | Yes - include for rich snippets |
| Multi-language | No - single language only |
| Comments | No - not needed |
| RSS/Sitemap | Yes - auto-generate |

---

## Executive Summary

This document outlines the implementation plan for 5 major features to make NOIR the best enterprise template:

| Phase | Feature | Priority | Dependencies |
|-------|---------|----------|--------------|
| 1 | Caching Infrastructure | Critical | None |
| 2 | Image Processing Service | High | Phase 1 |
| 3 | BlockNote Editor | High | Phase 2 |
| 4 | Blog/CMS Feature | High | Phase 2, 3 |
| 5 | Performance Hardening | Medium | Phase 1-4 |

---

## Phase 1: Caching Infrastructure (FusionCache)

### 1.1 Overview

Add a caching layer using **FusionCache** - the most production-ready hybrid caching library for .NET. FusionCache provides:
- L1 (in-memory) + L2 (distributed) hybrid caching
- **Stampede protection** across multiple replicas (not just process-level)
- **Fail-safe**: Return stale data during cache/database outages
- **Soft/hard timeouts**: Don't wait forever for slow backends
- **Auto-recovery**: Automatic cache refresh when backends recover
- **Backplane support**: Cache invalidation across multiple app instances

Starting with in-memory for zero-setup, with Redis/backplane ready when needed.

### 1.2 NuGet Packages

```xml
<!-- src/NOIR.Infrastructure/NOIR.Infrastructure.csproj -->
<PackageReference Include="ZiggyCreatures.FusionCache" Version="2.0.0" />
<!-- Optional: For Redis L2 cache -->
<PackageReference Include="ZiggyCreatures.FusionCache.Serialization.SystemTextJson" Version="2.0.0" />
<!-- Optional: For backplane (multi-replica invalidation) -->
<PackageReference Include="ZiggyCreatures.FusionCache.Backplane.Memory" Version="2.0.0" />
```

### 1.3 New Files

```
src/NOIR.Infrastructure/
├── Caching/
│   ├── CacheKeys.cs                            # Centralized cache key definitions
│   ├── CacheSettings.cs                        # Configuration options
│   ├── FusionCacheRegistration.cs              # DI setup for FusionCache
│   └── CacheInvalidationService.cs             # Event-driven invalidation

tests/NOIR.Application.UnitTests/
├── Infrastructure/
│   └── Caching/
│       ├── CacheInvalidationServiceTests.cs
│       └── CacheKeysTests.cs
```

### 1.4 Using FusionCache (No Custom Interface Needed)

FusionCache provides `IFusionCache` which is injected directly. No custom abstraction needed.

```csharp
// Usage in handlers/services - inject IFusionCache directly
public class GetUserPermissionsQueryHandler
{
    private readonly IFusionCache _cache;
    private readonly IRepository<Permission, Guid> _permissionRepo;

    public async Task<List<string>> Handle(GetUserPermissionsQuery query, CancellationToken ct)
    {
        return await _cache.GetOrSetAsync(
            CacheKeys.UserPermissions(query.UserId),
            async token => await LoadPermissionsFromDb(query.UserId, token),
            options => options
                .SetDuration(TimeSpan.FromMinutes(60))
                .SetFailSafe(true, TimeSpan.FromHours(2))  // Return stale for 2h if DB down
                .SetFactorySoftTimeout(TimeSpan.FromMilliseconds(100))  // Don't wait >100ms
                .SetFactoryHardTimeout(TimeSpan.FromSeconds(2)),  // Absolute max 2s
            ct);
    }
}
```

### 1.5 Cache Keys Structure

```csharp
// src/NOIR.Infrastructure/Caching/CacheKeys.cs
public static class CacheKeys
{
    // Permissions (most critical - queried every request)
    public static string UserPermissions(string userId) => $"permissions:user:{userId}";
    public static string RolePermissions(string roleId) => $"permissions:role:{roleId}";
    public const string AllPermissions = "permissions:all";

    // User profiles
    public static string UserProfile(string userId) => $"user:profile:{userId}";
    public static string UserById(Guid id) => $"user:id:{id}";

    // Tenants
    public static string TenantSettings(string tenantId) => $"tenant:settings:{tenantId}";
    public static string TenantBranding(string tenantId) => $"tenant:branding:{tenantId}";

    // Email templates
    public static string EmailTemplate(string key) => $"email:template:{key}";

    // Blog (Phase 4)
    public static string Post(string slug) => $"blog:post:{slug}";
    public static string PostList(int page, int size) => $"blog:posts:{page}:{size}";
    public const string Categories = "blog:categories";
    public const string Tags = "blog:tags";

    // Prefixes for bulk invalidation
    public static class Prefixes
    {
        public const string Permissions = "permissions:";
        public const string User = "user:";
        public const string Tenant = "tenant:";
        public const string Blog = "blog:";
        public const string Email = "email:";
    }
}
```

### 1.6 Configuration

```csharp
// src/NOIR.Infrastructure/Caching/CacheSettings.cs
public class CacheSettings
{
    public const string SectionName = "Cache";

    public int DefaultExpirationMinutes { get; set; } = 30;
    public int PermissionExpirationMinutes { get; set; } = 60;
    public int UserProfileExpirationMinutes { get; set; } = 15;
    public int BlogPostExpirationMinutes { get; set; } = 5;

    // FusionCache-specific settings
    public int FailSafeMaxDurationMinutes { get; set; } = 120;  // Return stale data for up to 2h
    public int FactorySoftTimeoutMs { get; set; } = 100;        // Start returning stale after 100ms
    public int FactoryHardTimeoutMs { get; set; } = 2000;       // Absolute max wait 2s
    public bool EnableBackplane { get; set; } = false;          // Enable for multi-replica
    public string? RedisConnectionString { get; set; }          // For L2 cache + backplane
}
```

### 1.7 FusionCache Registration

**Default: In-Memory Only (No Redis Required)**

FusionCache uses `MemoryCache` as L1 by default. You get all the benefits (stampede protection, fail-safe, timeouts) without any external dependencies. Redis is optional for when you scale.

```csharp
// src/NOIR.Infrastructure/Caching/FusionCacheRegistration.cs
public static class FusionCacheRegistration
{
    public static IServiceCollection AddFusionCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>()
            ?? new CacheSettings();

        // DEFAULT: In-memory only (L1 cache)
        // This already gives you:
        // - Stampede protection (process-level)
        // - Fail-safe (return stale data)
        // - Soft/hard timeouts
        // - Tag-based invalidation
        services.AddFusionCache()
            .WithDefaultEntryOptions(options => options
                .SetDuration(TimeSpan.FromMinutes(settings.DefaultExpirationMinutes))
                .SetFailSafe(true, TimeSpan.FromMinutes(settings.FailSafeMaxDurationMinutes))
                .SetFactorySoftTimeout(TimeSpan.FromMilliseconds(settings.FactorySoftTimeoutMs))
                .SetFactoryHardTimeout(TimeSpan.FromMilliseconds(settings.FactoryHardTimeoutMs))
            );

        // OPTIONAL: Add Redis L2 cache for distributed scenarios (multi-replica)
        // Only enable when you need shared cache across app instances
        if (!string.IsNullOrEmpty(settings.RedisConnectionString))
        {
            services.AddFusionCache()
                .WithSystemTextJsonSerializer()  // Required for L2 serialization
                .WithDistributedCache(
                    new RedisCache(new RedisCacheOptions
                    {
                        Configuration = settings.RedisConnectionString
                    }));

            // Enable backplane for cross-replica cache invalidation
            if (settings.EnableBackplane)
            {
                services.AddFusionCacheStackExchangeRedisBackplane(options =>
                {
                    options.Configuration = settings.RedisConnectionString;
                });
            }
        }

        return services;
    }
}

// Register in DependencyInjection.cs:
// services.AddFusionCaching(configuration);
```

### 1.8 Tag-Based Invalidation (FusionCache v2 Feature)

```csharp
// FusionCache v2 supports tagging for bulk invalidation
public class CacheInvalidationService : IScopedService
{
    private readonly IFusionCache _cache;

    // Invalidate all user-related cache entries
    public async Task InvalidateUserCacheAsync(string userId, CancellationToken ct)
    {
        // Remove specific keys
        await _cache.RemoveAsync(CacheKeys.UserPermissions(userId), token: ct);
        await _cache.RemoveAsync(CacheKeys.UserProfile(userId), token: ct);

        // Or use tags (FusionCache v2)
        await _cache.RemoveByTagAsync($"user:{userId}", token: ct);
    }

    // Invalidate all blog cache
    public async Task InvalidateBlogCacheAsync(CancellationToken ct)
    {
        await _cache.RemoveByTagAsync("blog", token: ct);
    }
}

// When setting cache, add tags:
await _cache.SetAsync(
    CacheKeys.Post(slug),
    post,
    options => options.SetDuration(TimeSpan.FromMinutes(5)),
    tags: ["blog", $"post:{post.Id}"],
    ct);
```

### 1.9 Integration Points

**Permission Caching (Critical Path):**
```csharp
// Update: src/NOIR.Infrastructure/Authorization/PermissionAuthorizationHandler.cs
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IFusionCache _cache;
    private readonly IRepository<Permission, Guid> _permissionRepo;

    protected override async Task HandleRequirementAsync(...)
    {
        var userId = context.User.GetUserId();
        var permissions = await _cache.GetOrSetAsync(
            CacheKeys.UserPermissions(userId),
            async ct => await LoadUserPermissionsAsync(userId, ct),
            options => options
                .SetDuration(TimeSpan.FromMinutes(60))
                .SetFailSafe(true)  // Return stale if DB is slow
                .SetFactorySoftTimeout(TimeSpan.FromMilliseconds(50)),  // Fast path
            tags: [$"user:{userId}", "permissions"]);

        // Check permissions...
    }
}
```

**Cache Invalidation via Wolverine Events:**
```csharp
// src/NOIR.Infrastructure/Caching/CacheInvalidationService.cs
public class CacheInvalidationService :
    IWolverineHandler<UserRolesChangedEvent>,
    IWolverineHandler<PermissionUpdatedEvent>,
    IWolverineHandler<UserProfileUpdatedEvent>,
    IScopedService
{
    private readonly IFusionCache _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public async Task Handle(UserRolesChangedEvent @event, CancellationToken ct)
    {
        // Tag-based invalidation removes all related entries
        await _cache.RemoveByTagAsync($"user:{@event.UserId}", token: ct);
        _logger.LogInformation("Invalidated cache for user {UserId}", @event.UserId);
    }

    public async Task Handle(PermissionUpdatedEvent @event, CancellationToken ct)
    {
        // Invalidate all permission caches
        await _cache.RemoveByTagAsync("permissions", token: ct);
    }

    public async Task Handle(UserProfileUpdatedEvent @event, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync($"user:{@event.UserId}", token: ct);
    }
}
```

### 1.10 appsettings.json

```json
{
  "Cache": {
    "DefaultExpirationMinutes": 30,
    "PermissionExpirationMinutes": 60,
    "UserProfileExpirationMinutes": 15,
    "BlogPostExpirationMinutes": 5,
    "FailSafeMaxDurationMinutes": 120,
    "FactorySoftTimeoutMs": 100,
    "FactoryHardTimeoutMs": 2000,
    "EnableBackplane": false,
    "RedisConnectionString": null
  }
}

// For production with Redis (multi-replica):
{
  "Cache": {
    "DefaultExpirationMinutes": 30,
    "FailSafeMaxDurationMinutes": 120,
    "FactorySoftTimeoutMs": 100,
    "FactoryHardTimeoutMs": 2000,
    "EnableBackplane": true,
    "RedisConnectionString": "localhost:6379,abortConnect=false"
  }
}
```

### 1.11 Tests Required

| Test File | Coverage |
|-----------|----------|
| `CacheKeysTests.cs` | Key format validation, tag structure |
| `CacheInvalidationServiceTests.cs` | Event handling, tag-based invalidation |
| `FusionCacheIntegrationTests.cs` | GetOrSet, fail-safe behavior, timeout handling |

### 1.12 Phase 1 Checklist

**Core Implementation:**
- [ ] Add FusionCache NuGet packages to Infrastructure project
- [ ] Create `CacheKeys` static class
- [ ] Create `CacheSettings` configuration
- [ ] Create `FusionCacheRegistration.cs` DI setup
- [ ] Implement `CacheInvalidationService` with Wolverine handlers
- [ ] Integrate with permission authorization handler (use IFusionCache)

**NOIR Patterns (CRITICAL):**
- [ ] Add `IScopedService` marker to `CacheInvalidationService`
- [ ] Update `GlobalUsings.cs` with `ZiggyCreatures.Caching.Fusion`
- [ ] Update `DependencyInjection.cs` to call `AddFusionCaching()`

**Testing:**
- [ ] Add unit tests for cache key validation
- [ ] Add integration tests for cache invalidation events

**Configuration:**
- [ ] Add `appsettings.json` configuration
- [ ] Add `appsettings.Development.json` with shorter timeouts for dev

**Observability (Recommended):**
- [ ] Add cache health check endpoint (`/health/cache`)
- [ ] Add cache metrics logging (hit/miss ratio) for monitoring

---

## Phase 2: Image Processing Service

### 2.1 Overview

Add image processing capabilities for resizing, optimization, multi-format generation (AVIF + WebP + JPEG), **ThumbHash placeholders** (better than BlurHash - smaller size, preserves aspect ratio, supports transparency), and SEO-friendly filenames with dominant color extraction. Essential for blog featured images, user avatars, and any uploaded content.

**Why ThumbHash over BlurHash:**
- **28 bytes** vs 34 bytes (20% smaller)
- **Better visual quality** at same size
- **Preserves aspect ratio** in the hash itself
- **Supports alpha/transparency** for PNGs

### 2.2 NuGet Packages

```xml
<!-- src/NOIR.Infrastructure/NOIR.Infrastructure.csproj -->
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.5" />
<PackageReference Include="SixLabors.ImageSharp.Web" Version="3.1.3" />
<PackageReference Include="NeoSolve.ImageSharp.AVIF" Version="1.3.0" />  <!-- Real AVIF encoding -->
<PackageReference Include="Thumbhash" Version="1.0.0" />  <!-- Better than BlurHash -->
```

```json
// src/NOIR.Web/frontend/package.json
{
  "dependencies": {
    "thumbhash": "^0.1.1"  // ThumbHash decoder for frontend
  }
}
```

### 2.3 New Files

```
src/NOIR.Application/
├── Common/
│   └── Interfaces/
│       └── IImageProcessor.cs                  # Image processing abstraction

src/NOIR.Infrastructure/
├── Media/
│   ├── ImageProcessingSettings.cs              # Configuration
│   ├── ImageProcessorService.cs                # ImageSharp + AVIF implementation
│   ├── ImageVariant.cs                         # Variant definitions
│   ├── ImageMetadata.cs                        # Extracted metadata
│   ├── OutputFormat.cs                         # Output format enum
│   ├── SlugGenerator.cs                        # SEO-friendly filename generator
│   ├── ColorAnalyzer.cs                        # Dominant color extraction
│   ├── ThumbHashGenerator.cs                   # ThumbHash placeholder generation
│   └── SrcsetGenerator.cs                      # Responsive srcset builder

src/NOIR.Web/
├── Endpoints/
│   └── MediaEndpoints.cs                       # Upload with processing

src/NOIR.Web/frontend/
├── src/components/
│   └── ThumbHashImage.tsx                      # ThumbHash placeholder component

tests/
├── NOIR.Application.UnitTests/
│   └── Infrastructure/
│       └── Media/
│           ├── ImageProcessorServiceTests.cs
│           ├── SlugGeneratorTests.cs
│           ├── ThumbHashGeneratorTests.cs
│           └── ColorAnalyzerTests.cs
```

### 2.4 Interface Design

```csharp
// src/NOIR.Application/Common/Interfaces/IImageProcessor.cs
public interface IImageProcessor
{
    /// <summary>
    /// Process an uploaded image: validate, resize, optimize, generate multi-format variants,
    /// extract ThumbHash and dominant color, create SEO-friendly filenames.
    /// </summary>
    Task<ImageProcessingResult> ProcessAsync(
        Stream inputStream,
        string fileName,
        ImageProcessingOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a specific variant of an existing image.
    /// </summary>
    Task<Stream> GenerateVariantAsync(
        Stream inputStream,
        ImageVariant variant,
        OutputFormat format,
        CancellationToken ct = default);

    /// <summary>
    /// Extract metadata from an image.
    /// </summary>
    Task<ImageMetadata> GetMetadataAsync(
        Stream inputStream,
        CancellationToken ct = default);

    /// <summary>
    /// Validate if file is a supported image format.
    /// </summary>
    bool IsValidImage(Stream inputStream);

    /// <summary>
    /// Generate ThumbHash for an image (better than BlurHash).
    /// Returns base64-encoded hash that can be decoded to a placeholder image.
    /// </summary>
    Task<string> GenerateThumbHashAsync(
        Stream inputStream,
        CancellationToken ct = default);

    /// <summary>
    /// Extract dominant color from an image.
    /// </summary>
    Task<string> ExtractDominantColorAsync(
        Stream inputStream,
        CancellationToken ct = default);
}
```

### 2.5 Models

```csharp
// src/NOIR.Infrastructure/Media/OutputFormat.cs
public enum OutputFormat
{
    Avif,   // Best compression, modern browsers
    WebP,   // Good compression, wide support
    Jpeg    // Fallback for all browsers
}

// src/NOIR.Infrastructure/Media/ImageVariant.cs
public record ImageVariant(string Name, int MaxWidth, int MaxHeight, int Quality = 85)
{
    public static readonly ImageVariant Thumbnail = new("thumb", 150, 150, 80);
    public static readonly ImageVariant Small = new("sm", 320, 320, 85);
    public static readonly ImageVariant Medium = new("md", 640, 640, 85);
    public static readonly ImageVariant Large = new("lg", 1280, 1280, 85);
    public static readonly ImageVariant ExtraLarge = new("xl", 1920, 1920, 85);  // Full HD
    public static readonly ImageVariant Original = new("original", 2560, 2560, 90);  // 2K

    public static readonly ImageVariant[] BlogVariants = [Thumbnail, Small, Medium, Large, ExtraLarge];
    public static readonly ImageVariant[] AvatarVariants = [Thumbnail, Small, Medium];
    public static readonly ImageVariant[] FullVariants = [Thumbnail, Small, Medium, Large, ExtraLarge, Original];
}

// src/NOIR.Infrastructure/Media/ImageProcessingResult.cs
public record ImageProcessingResult
{
    public required string Slug { get; init; }              // SEO-friendly slug from filename
    public required string ShortId { get; init; }           // Unique short ID (e.g., "x7k9m2")
    public required string DominantColor { get; init; }     // Hex color without # (e.g., "ff6b35")
    public required string ThumbHash { get; init; }          // ThumbHash base64 for placeholder (better than BlurHash)
    public required ImageMetadata Metadata { get; init; }
    public required long OriginalSize { get; init; }
    public required long ProcessedSize { get; init; }
    
    /// <summary>
    /// All generated variants organized by format and size.
    /// Key format: "{format}/{variant}" e.g., "avif/lg", "webp/md", "jpeg/sm"
    /// </summary>
    public required Dictionary<string, ImageVariantResult> Variants { get; init; }
    
    public string? Error { get; init; }
    public bool IsSuccess => Error is null;
    
    /// <summary>
    /// Get the best quality URL (prefers AVIF > WebP > JPEG).
    /// </summary>
    public string GetBestUrl(string variantName)
    {
        if (Variants.TryGetValue($"avif/{variantName}", out var avif)) return avif.Url;
        if (Variants.TryGetValue($"webp/{variantName}", out var webp)) return webp.Url;
        if (Variants.TryGetValue($"jpeg/{variantName}", out var jpeg)) return jpeg.Url;
        return Variants.Values.First().Url;
    }
}

// src/NOIR.Infrastructure/Media/ImageVariantResult.cs
public record ImageVariantResult
{
    public required string VariantName { get; init; }       // e.g., "lg"
    public required OutputFormat Format { get; init; }      // AVIF, WebP, or JPEG
    public required string Path { get; init; }              // Storage path
    public required string Url { get; init; }               // Public URL
    public required string FileName { get; init; }          // e.g., "sunset-beach-x7k9m2-ff6b35-lg.avif"
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long FileSize { get; init; }
}

// src/NOIR.Infrastructure/Media/ImageMetadata.cs
public record ImageMetadata
{
    public int Width { get; init; }
    public int Height { get; init; }
    public string Format { get; init; } = default!;
    public string? MimeType { get; init; }
    public long FileSize { get; init; }
    public string? ColorSpace { get; init; }
    public bool HasAlpha { get; init; }
    public double AspectRatio => Height > 0 ? (double)Width / Height : 0;
}

// src/NOIR.Infrastructure/Media/ImageProcessingOptions.cs
public record ImageProcessingOptions
{
    public string Folder { get; init; } = "uploads";
    public ImageVariant[] Variants { get; init; } = ImageVariant.BlogVariants;
    
    /// <summary>
    /// Output formats to generate. Default: AVIF + WebP + JPEG for maximum compatibility.
    /// </summary>
    public OutputFormat[] OutputFormats { get; init; } = [OutputFormat.Avif, OutputFormat.WebP, OutputFormat.Jpeg];
    
    public int MaxFileSizeMB { get; init; } = 10;
    public string[] AllowedExtensions { get; init; } = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif"];
    
    /// <summary>
    /// Custom slug override. If null, auto-generated from original filename.
    /// </summary>
    public string? CustomSlug { get; init; }
    
    /// <summary>
    /// Generate ThumbHash placeholder.
    /// </summary>
    public bool GenerateThumbHash { get; init; } = true;
    
    /// <summary>
    /// Extract dominant color for SEO-friendly filenames.
    /// </summary>
    public bool ExtractDominantColor { get; init; } = true;
}
```

### 2.6 SEO-Friendly Filename Generation

```csharp
// src/NOIR.Infrastructure/Media/SlugGenerator.cs
public static class SlugGenerator
{
    /// <summary>
    /// Generate SEO-friendly slug from original filename.
    /// "My Beautiful Sunset-Beach_Photo.jpg" -> "my-beautiful-sunset-beach-photo"
    /// </summary>
    public static string FromFileName(string fileName)
    {
        // Remove extension
        var name = Path.GetFileNameWithoutExtension(fileName);
        
        // Convert to lowercase
        name = name.ToLowerInvariant();
        
        // Replace underscores and multiple hyphens with single hyphen
        name = Regex.Replace(name, @"[_\s]+", "-");
        
        // Remove any characters that aren't letters, numbers, or hyphens
        name = Regex.Replace(name, @"[^a-z0-9\-]", "");
        
        // Remove multiple consecutive hyphens
        name = Regex.Replace(name, @"-+", "-");
        
        // Trim hyphens from start and end
        name = name.Trim('-');
        
        // Limit length (SEO best practice: 50-60 chars max)
        if (name.Length > 50)
        {
            name = name[..50].TrimEnd('-');
        }
        
        return string.IsNullOrEmpty(name) ? "image" : name;
    }

    /// <summary>
    /// Generate short unique ID (6 chars alphanumeric).
    /// </summary>
    public static string GenerateShortId()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    /// <summary>
    /// Build the SEO-friendly filename.
    /// Format: {slug}-{shortId}-{dominantColor}-{size}.{format}
    /// Example: sunset-beach-x7k9m2-ff6b35-lg.webp
    /// </summary>
    public static string BuildFileName(
        string slug,
        string shortId,
        string dominantColor,
        string variantName,
        OutputFormat format)
    {
        var extension = format switch
        {
            OutputFormat.Avif => "avif",
            OutputFormat.WebP => "webp",
            OutputFormat.Jpeg => "jpg",
            _ => "jpg"
        };
        
        return $"{slug}-{shortId}-{dominantColor}-{variantName}.{extension}";
    }
}

// src/NOIR.Infrastructure/Media/ColorAnalyzer.cs
public static class ColorAnalyzer
{
    /// <summary>
    /// Extract dominant color from image using K-means clustering.
    /// Returns hex color without # (e.g., "ff6b35").
    /// </summary>
    public static string ExtractDominantColor(Image<Rgba32> image)
    {
        // Sample pixels from the image (resize to small for performance)
        using var sampled = image.Clone();
        sampled.Mutate(x => x.Resize(50, 50));
        
        var colors = new Dictionary<string, int>();
        
        sampled.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                foreach (var pixel in row)
                {
                    // Skip very light or very dark pixels
                    var brightness = (pixel.R + pixel.G + pixel.B) / 3;
                    if (brightness < 20 || brightness > 235) continue;
                    
                    // Quantize to reduce unique colors
                    var r = (pixel.R / 32) * 32;
                    var g = (pixel.G / 32) * 32;
                    var b = (pixel.B / 32) * 32;
                    
                    var key = $"{r:x2}{g:x2}{b:x2}";
                    colors[key] = colors.GetValueOrDefault(key) + 1;
                }
            }
        });
        
        if (colors.Count == 0)
            return "808080"; // Default gray
            
        return colors.MaxBy(x => x.Value).Key;
    }
}
```

### 2.7 Implementation

```csharp
// src/NOIR.Infrastructure/Media/ImageProcessorService.cs
public class ImageProcessorService : IImageProcessor, IScopedService
{
    private readonly IFileStorage _storage;
    private readonly ImageProcessingSettings _settings;
    private readonly ILogger<ImageProcessorService> _logger;

    public async Task<ImageProcessingResult> ProcessAsync(
        Stream inputStream,
        string fileName,
        ImageProcessingOptions options,
        CancellationToken ct = default)
    {
        // Validate extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!options.AllowedExtensions.Contains(extension))
        {
            return CreateErrorResult($"File type {extension} not allowed");
        }

        // Validate size
        if (inputStream.Length > options.MaxFileSizeMB * 1024 * 1024)
        {
            return CreateErrorResult($"File exceeds {options.MaxFileSizeMB}MB limit");
        }

        // Load image
        inputStream.Position = 0;
        using var image = await Image.LoadAsync<Rgba32>(inputStream, ct);
        var originalSize = inputStream.Length;
        var metadata = ExtractMetadata(image, inputStream.Length);

        // Generate identifiers
        var slug = options.CustomSlug ?? SlugGenerator.FromFileName(fileName);
        var shortId = SlugGenerator.GenerateShortId();
        
        // Extract dominant color
        var dominantColor = options.ExtractDominantColor 
            ? ColorAnalyzer.ExtractDominantColor(image)
            : "000000";

        // Generate ThumbHash
        inputStream.Position = 0;
        var thumbhash = options.GenerateThumbHash
            ? await GenerateThumbHashAsync(inputStream, ct: ct)
            : "";

        var variants = new Dictionary<string, ImageVariantResult>();
        long totalProcessedSize = 0;

        // Process each variant for each output format
        foreach (var variant in options.Variants)
        {
            using var resizedImage = image.Clone();
            
            // Resize maintaining aspect ratio
            resizedImage.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(variant.MaxWidth, variant.MaxHeight),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Lanczos3
            }));

            // Generate each format
            foreach (var format in options.OutputFormats)
            {
                var (variantResult, size) = await SaveVariantAsync(
                    resizedImage,
                    slug,
                    shortId,
                    dominantColor,
                    variant,
                    format,
                    options.Folder,
                    ct);

                var key = $"{format.ToString().ToLower()}/{variant.Name}";
                variants[key] = variantResult;
                totalProcessedSize += size;
            }
        }

        return new ImageProcessingResult
        {
            Slug = slug,
            ShortId = shortId,
            DominantColor = dominantColor,
            ThumbHash = thumbhash,
            Metadata = metadata,
            OriginalSize = originalSize,
            ProcessedSize = totalProcessedSize,
            Variants = variants
        };
    }

    private async Task<(ImageVariantResult result, long size)> SaveVariantAsync(
        Image image,
        string slug,
        string shortId,
        string dominantColor,
        ImageVariant variant,
        OutputFormat format,
        string folder,
        CancellationToken ct)
    {
        var fileName = SlugGenerator.BuildFileName(slug, shortId, dominantColor, variant.Name, format);
        var storagePath = $"{folder}/{fileName}";
        
        using var outputStream = new MemoryStream();

        switch (format)
        {
            case OutputFormat.Avif:
                // Real AVIF encoding via NeoSolve.ImageSharp.AVIF
                await image.SaveAsAvifAsync(outputStream, new AvifEncoder
                {
                    Quality = variant.Quality
                }, ct);
                break;

            case OutputFormat.WebP:
                await image.SaveAsWebpAsync(outputStream, new WebpEncoder
                {
                    Quality = variant.Quality
                }, ct);
                break;

            case OutputFormat.Jpeg:
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder
                {
                    Quality = variant.Quality
                }, ct);
                break;
        }

        outputStream.Position = 0;
        await _storage.UploadAsync(fileName, outputStream, folder, ct);

        return (new ImageVariantResult
        {
            VariantName = variant.Name,
            Format = format,
            Path = storagePath,
            Url = _storage.GetPublicUrl(storagePath) ?? storagePath,
            FileName = fileName,
            Width = image.Width,
            Height = image.Height,
            FileSize = outputStream.Length
        }, outputStream.Length);
    }

    public async Task<string> GenerateThumbHashAsync(
        Stream inputStream,
        CancellationToken ct = default)
    {
        inputStream.Position = 0;
        using var image = await Image.LoadAsync<Rgba32>(inputStream, ct);

        // Resize for ThumbHash (max 100x100 recommended)
        using var small = image.Clone();
        small.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(100, 100),
            Mode = ResizeMode.Max
        }));

        // Extract RGBA bytes
        var width = small.Width;
        var height = small.Height;
        var rgba = new byte[width * height * 4];

        small.ProcessPixelRows(accessor =>
        {
            var index = 0;
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                foreach (var pixel in row)
                {
                    rgba[index++] = pixel.R;
                    rgba[index++] = pixel.G;
                    rgba[index++] = pixel.B;
                    rgba[index++] = pixel.A;
                }
            }
        });

        // Generate ThumbHash
        var hash = ThumbHash.RgbaToThumbHash(width, height, rgba);
        return Convert.ToBase64String(hash);
    }

    public async Task<string> ExtractDominantColorAsync(
        Stream inputStream,
        CancellationToken ct = default)
    {
        inputStream.Position = 0;
        using var image = await Image.LoadAsync<Rgba32>(inputStream, ct);
        return ColorAnalyzer.ExtractDominantColor(image);
    }

    private static ImageMetadata ExtractMetadata(Image image, long fileSize)
    {
        return new ImageMetadata
        {
            Width = image.Width,
            Height = image.Height,
            Format = image.Metadata.DecodedImageFormat?.Name ?? "Unknown",
            MimeType = image.Metadata.DecodedImageFormat?.DefaultMimeType,
            FileSize = fileSize,
            HasAlpha = image.PixelType.AlphaRepresentation != PixelAlphaRepresentation.None
        };
    }

    private static ImageProcessingResult CreateErrorResult(string error) => new()
    {
        Error = error,
        Slug = "",
        ShortId = "",
        DominantColor = "",
        ThumbHash = "",
        Metadata = new ImageMetadata(),
        OriginalSize = 0,
        ProcessedSize = 0,
        Variants = new()
    };
}
```

### 2.8 Srcset Generation Helper (for Blog Renderer)

```csharp
// src/NOIR.Infrastructure/Media/SrcsetGenerator.cs
public static class SrcsetGenerator
{
    /// <summary>
    /// Generate responsive srcset HTML attribute from image variants.
    /// Returns srcset for AVIF, WebP, and JPEG with picture element support.
    /// </summary>
    public static string GeneratePictureElement(
        ImageProcessingResult image,
        string alt,
        string? cssClass = null,
        bool lazyLoad = true)
    {
        var classAttr = cssClass != null ? $" class=\"{cssClass}\"" : "";
        var loadingAttr = lazyLoad ? " loading=\"lazy\"" : "";
        
        // Build srcset for each format
        var avifSrcset = BuildSrcset(image, OutputFormat.Avif);
        var webpSrcset = BuildSrcset(image, OutputFormat.WebP);
        var jpegSrcset = BuildSrcset(image, OutputFormat.Jpeg);
        
        // Fallback image (largest JPEG)
        var fallbackUrl = image.GetBestUrl("lg");
        
        // ThumbHash inline style for placeholder
        var thumbhashStyle = !string.IsNullOrEmpty(image.ThumbHash)
            ? $" style=\"background: linear-gradient(#{image.DominantColor}, #{image.DominantColor})\""
            : "";

        return $"""
            <picture>
                <source type="image/avif" srcset="{avifSrcset}" />
                <source type="image/webp" srcset="{webpSrcset}" />
                <img 
                    src="{fallbackUrl}"
                    srcset="{jpegSrcset}"
                    alt="{alt}"
                    width="{image.Metadata.Width}"
                    height="{image.Metadata.Height}"
                    data-thumbhash="{image.ThumbHash}"
                    {classAttr}{loadingAttr}{thumbhashStyle}
                />
            </picture>
            """;
    }

    /// <summary>
    /// Build srcset string for a specific format.
    /// Example: "image-sm.webp 320w, image-md.webp 640w, image-lg.webp 1280w"
    /// </summary>
    public static string BuildSrcset(ImageProcessingResult image, OutputFormat format)
    {
        var formatKey = format.ToString().ToLower();
        var entries = new List<string>();

        foreach (var (key, variant) in image.Variants)
        {
            if (key.StartsWith($"{formatKey}/"))
            {
                entries.Add($"{variant.Url} {variant.Width}w");
            }
        }

        return string.Join(", ", entries.OrderBy(e => 
            int.Parse(e.Split(' ')[1].TrimEnd('w'))));
    }

    /// <summary>
    /// Generate sizes attribute for common layouts.
    /// </summary>
    public static string GetSizesAttribute(string layout = "content")
    {
        return layout switch
        {
            "full" => "100vw",
            "content" => "(max-width: 768px) 100vw, (max-width: 1200px) 80vw, 1200px",
            "thumbnail" => "150px",
            "card" => "(max-width: 640px) 100vw, 320px",
            _ => "100vw"
        };
    }
}
```

### 2.9 API Endpoint

```csharp
// src/NOIR.Web/Endpoints/MediaEndpoints.cs
public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/media")
            .WithTags("Media")
            .RequireAuthorization();

        // Upload image with full processing
        group.MapPost("/upload", async (
            IFormFile file,
            [FromQuery] string? folder,
            [FromQuery] string? customSlug,
            [FromServices] IImageProcessor imageProcessor,
            [FromServices] IFileStorage storage,
            CancellationToken ct) =>
        {
            if (file.Length == 0)
                return Results.BadRequest("No file uploaded");

            await using var stream = file.OpenReadStream();

            var result = await imageProcessor.ProcessAsync(
                stream,
                file.FileName,
                new ImageProcessingOptions
                {
                    Folder = folder ?? "uploads",
                    Variants = ImageVariant.BlogVariants,
                    OutputFormats = [OutputFormat.Avif, OutputFormat.WebP, OutputFormat.Jpeg],
                    CustomSlug = customSlug,
                    GenerateThumbHash = true,
                    ExtractDominantColor = true
                },
                ct);

            if (!result.IsSuccess)
                return Results.BadRequest(result.Error);

            return Results.Ok(new UploadResponse
            {
                Slug = result.Slug,
                ShortId = result.ShortId,
                DominantColor = result.DominantColor,
                ThumbHash = result.ThumbHash,
                Url = result.GetBestUrl("lg"),
                Variants = result.Variants.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new VariantInfo
                    {
                        Url = kvp.Value.Url,
                        Width = kvp.Value.Width,
                        Height = kvp.Value.Height,
                        FileSize = kvp.Value.FileSize
                    }
                ),
                Srcset = new SrcsetInfo
                {
                    Avif = SrcsetGenerator.BuildSrcset(result, OutputFormat.Avif),
                    WebP = SrcsetGenerator.BuildSrcset(result, OutputFormat.WebP),
                    Jpeg = SrcsetGenerator.BuildSrcset(result, OutputFormat.Jpeg)
                },
                Metadata = result.Metadata
            });
        })
        .DisableAntiforgery()
        .WithName("UploadMedia")
        .Produces<UploadResponse>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // Upload for editor (simple response format for BlockNote)
        group.MapPost("/editor-upload", async (
            IFormFile file,
            [FromServices] IImageProcessor imageProcessor,
            [FromServices] IFileStorage storage,
            CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();

            var result = await imageProcessor.ProcessAsync(
                stream,
                file.FileName,
                new ImageProcessingOptions
                {
                    Folder = "content",
                    Variants = [ImageVariant.Medium, ImageVariant.Large, ImageVariant.ExtraLarge],
                    OutputFormats = [OutputFormat.Avif, OutputFormat.WebP, OutputFormat.Jpeg],
                    GenerateThumbHash = true,
                    ExtractDominantColor = true
                },
                ct);

            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });

            // Return format expected by BlockNote
            return Results.Ok(new
            {
                url = result.GetBestUrl("lg"),
                thumbhash = result.ThumbHash,
                dominantColor = result.DominantColor,
                width = result.Metadata.Width,
                height = result.Metadata.Height
            });
        })
        .DisableAntiforgery()
        .WithName("EditorUpload");
    }
}

public record UploadResponse
{
    public required string Slug { get; init; }
    public required string ShortId { get; init; }
    public required string DominantColor { get; init; }
    public required string ThumbHash { get; init; }
    public required string Url { get; init; }
    public required Dictionary<string, VariantInfo> Variants { get; init; }
    public required SrcsetInfo Srcset { get; init; }
    public required ImageMetadata Metadata { get; init; }
}

public record VariantInfo
{
    public required string Url { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required long FileSize { get; init; }
}

public record SrcsetInfo
{
    public required string Avif { get; init; }
    public required string WebP { get; init; }
    public required string Jpeg { get; init; }
}
```

### 2.10 Update FileEndpoints

```csharp
// Update: src/NOIR.Web/Endpoints/FileEndpoints.cs
// Add "content/" to allowed prefixes for blog images
var allowedPrefixes = new[] { "avatars/", "content/", "uploads/" };
```

### 2.11 Configuration

```json
{
  "ImageProcessing": {
    "MaxFileSizeMB": 10,
    "DefaultQuality": 85,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif"],
    "GenerateThumbHash": true,
    "ExtractDominantColor": true,
    "OutputFormats": ["Avif", "WebP", "Jpeg"],
    "Variants": {
      "Thumbnail": { "MaxWidth": 150, "MaxHeight": 150, "Quality": 80 },
      "Small": { "MaxWidth": 320, "MaxHeight": 320, "Quality": 85 },
      "Medium": { "MaxWidth": 640, "MaxHeight": 640, "Quality": 85 },
      "Large": { "MaxWidth": 1280, "MaxHeight": 1280, "Quality": 85 },
      "ExtraLarge": { "MaxWidth": 1920, "MaxHeight": 1920, "Quality": 85 },
      "Original": { "MaxWidth": 2560, "MaxHeight": 2560, "Quality": 90 }
    }
  }
}
```

### 2.12 Frontend ThumbHash Decoding

```typescript
// src/NOIR.Web/frontend/src/components/ThumbHashImage.tsx
import { thumbHashToDataURL } from 'thumbhash'
import { useState, useMemo } from 'react'

interface ThumbHashImageProps {
  src: string
  thumbhash: string  // Base64-encoded ThumbHash
  alt: string
  width: number
  height: number
  className?: string
}

export function ThumbHashImage({ src, thumbhash, alt, width, height, className }: ThumbHashImageProps) {
  const [loaded, setLoaded] = useState(false)

  // Decode ThumbHash to data URL for placeholder
  const placeholderUrl = useMemo(() => {
    if (!thumbhash) return null
    try {
      const hashArray = Uint8Array.from(atob(thumbhash), c => c.charCodeAt(0))
      return thumbHashToDataURL(hashArray)
    } catch {
      return null
    }
  }, [thumbhash])

  return (
    <div className={`relative overflow-hidden ${className}`} style={{ aspectRatio: `${width}/${height}` }}>
      {/* ThumbHash placeholder */}
      {!loaded && placeholderUrl && (
        <img
          src={placeholderUrl}
          alt=""
          className="absolute inset-0 w-full h-full object-cover blur-sm scale-110"
          aria-hidden="true"
        />
      )}
      {/* Actual image */}
      <img
        src={src}
        alt={alt}
        width={width}
        height={height}
        loading="lazy"
        onLoad={() => setLoaded(true)}
        className={`w-full h-full object-cover transition-opacity duration-300 ${loaded ? 'opacity-100' : 'opacity-0'}`}
      />
    </div>
  )
}

// Usage example:
// <ThumbHashImage
//   src="/images/sunset-beach-x7k9m2-lg.webp"
//   thumbhash="YTkGJwaRhWWBd2iAhoh4l4lw"
//   alt="Sunset on the beach"
//   width={1280}
//   height={720}
// />
```

### 2.13 Phase 2 Checklist

**Core Implementation:**
- [ ] Add SixLabors.ImageSharp NuGet packages
- [ ] Add NeoSolve.ImageSharp.AVIF NuGet package (real AVIF support)
- [ ] Add Thumbhash NuGet package for C# ThumbHash generation
- [ ] Create `IImageProcessor` interface with ThumbHash and color extraction
- [ ] Implement `ImageProcessorService` with AVIF/WebP/JPEG multi-format support
- [ ] Create `ThumbHashGenerator` service
- [ ] Create `SlugGenerator` for SEO-friendly filenames
- [ ] Create `ColorAnalyzer` for dominant color extraction
- [ ] Create `SrcsetGenerator` for responsive images
- [ ] Create `ImageVariant`, `ImageMetadata`, `ImageProcessingResult` models
- [ ] Create `ImageProcessingSettings` configuration
- [ ] Create `MediaEndpoints` with upload endpoints
- [ ] Update `FileEndpoints` allowed prefixes

**NOIR Patterns (CRITICAL):**
- [ ] Add `IScopedService` marker to `ImageProcessorService`
- [ ] Update `GlobalUsings.cs` with ImageSharp namespaces
- [ ] Update `appsettings.json` with image processing settings

**Security & Validation:**
- [ ] Add file size limit validation (e.g., 10MB max)
- [ ] Add file type validation (magic bytes, not just extension)
- [ ] Add image dimension limits (prevent memory exhaustion)
- [ ] Handle corrupt/malformed images gracefully with error response
- [ ] Sanitize filenames to prevent path traversal

**Frontend:**
- [ ] Create `ThumbHashImage` React component
- [ ] Install `thumbhash` npm package (decoder for frontend)
- [ ] Add loading skeleton while ThumbHash decodes

**Testing:**
- [ ] Add unit tests for image processing (including AVIF)
- [ ] Add unit tests for ThumbHash generator
- [ ] Add unit tests for slug generator
- [ ] Add unit tests for color analyzer
- [ ] Add integration tests for upload flow
- [ ] Test with various image formats (PNG, JPEG, GIF, WebP, AVIF)
- [ ] Test with edge cases (very large, very small, corrupt files)

**Maintenance:**
- [ ] Create cleanup job for orphaned images (optional, for later)
- [ ] Add image processing queue for large uploads (optional, for later)

---

## Phase 3: BlockNote Editor Component

### 3.1 Overview

Replace TinyMCE with BlockNote for a unified, modern editing experience across email templates and blog content.

### 3.2 NPM Packages

```json
{
  "dependencies": {
    "@blocknote/core": "^0.17.0",
    "@blocknote/react": "^0.17.0",
    "@blocknote/mantine": "^0.17.0"
  },
  "devDependencies": {
    // Remove TinyMCE
    // "@tinymce/tinymce-react": "REMOVE",
    // "tinymce": "REMOVE"
  }
}
```

### 3.3 New Frontend Files

```
src/NOIR.Web/frontend/src/
├── components/
│   └── editor/
│       ├── BlockEditor.tsx                    # Main reusable component
│       ├── blocks/
│       │   ├── VariableBlock.tsx              # {{variable}} for emails
│       │   ├── CalloutBlock.tsx               # Info/warning/tip boxes
│       │   └── index.ts                       # Block exports
│       ├── converters/
│       │   ├── toEmailHtml.ts                 # Blocks -> Email-safe HTML
│       │   ├── toHtml.ts                      # Blocks -> Standard HTML
│       │   └── fromHtml.ts                    # HTML -> Blocks (migration)
│       ├── hooks/
│       │   └── useBlockEditor.ts              # Editor hook with upload
│       └── index.ts                           # Public exports
├── types/
│   └── editor.ts                              # Editor types
```

### 3.4 Component Implementation

```tsx
// src/components/editor/BlockEditor.tsx
import { BlockNoteEditor, PartialBlock } from "@blocknote/core";
import { BlockNoteView } from "@blocknote/mantine";
import { useCreateBlockNote } from "@blocknote/react";
import "@blocknote/mantine/style.css";

interface BlockEditorProps {
  initialContent?: PartialBlock[];
  onChange?: (blocks: PartialBlock[]) => void;
  onHtmlChange?: (html: string) => void;
  editable?: boolean;
  uploadFolder?: string;
  placeholder?: string;
  // For email templates
  variables?: string[];
  showVariableInserter?: boolean;
}

export function BlockEditor({
  initialContent,
  onChange,
  onHtmlChange,
  editable = true,
  uploadFolder = "content",
  placeholder = "Start writing...",
  variables = [],
  showVariableInserter = false,
}: BlockEditorProps) {
  const editor = useCreateBlockNote({
    initialContent,
    uploadFile: async (file: File) => {
      const formData = new FormData();
      formData.append("file", file);

      const response = await fetch(`/api/media/editor-upload?folder=${uploadFolder}`, {
        method: "POST",
        body: formData,
        credentials: "include",
      });

      if (!response.ok) {
        throw new Error("Upload failed");
      }

      const { url } = await response.json();
      return url;
    },
  });

  const handleChange = () => {
    const blocks = editor.document;
    onChange?.(blocks);

    if (onHtmlChange) {
      const html = blocksToHtml(blocks);
      onHtmlChange(html);
    }
  };

  return (
    <div className="blocknote-wrapper rounded-md border">
      <BlockNoteView
        editor={editor}
        editable={editable}
        onChange={handleChange}
        theme="light"
      />

      {showVariableInserter && variables.length > 0 && (
        <VariableInserter
          variables={variables}
          onInsert={(variable) => {
            editor.insertInlineContent([
              { type: "text", text: `{{${variable}}}`, styles: { code: true } }
            ]);
          }}
        />
      )}
    </div>
  );
}
```

### 3.5 Variable Block for Emails

```tsx
// src/components/editor/blocks/VariableBlock.tsx
import { createReactBlockSpec } from "@blocknote/react";

export const VariableBlock = createReactBlockSpec(
  {
    type: "variable",
    propSchema: {
      name: { default: "" },
    },
    content: "none",
  },
  {
    render: (props) => (
      <span
        className="inline-flex items-center px-2 py-0.5 rounded bg-blue-100 text-blue-800 font-mono text-sm"
        contentEditable={false}
      >
        {`{{${props.block.props.name}}}`}
      </span>
    ),
  }
);
```

### 3.6 Email HTML Converter

```tsx
// src/components/editor/converters/toEmailHtml.ts
import { Block } from "@blocknote/core";

/**
 * Convert BlockNote blocks to email-safe HTML.
 * - Inline CSS (email clients need it)
 * - Table-based layout for Outlook
 * - Preserves {{variable}} placeholders
 */
export function blocksToEmailHtml(blocks: Block[]): string {
  const bodyContent = blocks.map(blockToEmailHtml).join("\n");

  return `
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; font-size: 16px; line-height: 1.5; color: #333333;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width: 600px; margin: 0 auto;">
    <tr>
      <td style="padding: 20px;">
        ${bodyContent}
      </td>
    </tr>
  </table>
</body>
</html>`.trim();
}

function blockToEmailHtml(block: Block): string {
  switch (block.type) {
    case "paragraph":
      return `<p style="margin: 0 0 16px 0;">${inlineToHtml(block.content)}</p>`;

    case "heading":
      const level = block.props.level || 1;
      const fontSize = { 1: "28px", 2: "24px", 3: "20px" }[level] || "20px";
      return `<h${level} style="margin: 0 0 16px 0; font-size: ${fontSize}; font-weight: 600;">${inlineToHtml(block.content)}</h${level}>`;

    case "bulletListItem":
      return `<li style="margin: 0 0 8px 0;">${inlineToHtml(block.content)}</li>`;

    case "numberedListItem":
      return `<li style="margin: 0 0 8px 0;">${inlineToHtml(block.content)}</li>`;

    case "image":
      return `<img src="${block.props.url}" alt="${block.props.caption || ''}" style="max-width: 100%; height: auto; display: block; margin: 16px 0;" />`;

    case "variable":
      return `{{${block.props.name}}}`;

    default:
      return `<div>${inlineToHtml(block.content)}</div>`;
  }
}

function inlineToHtml(content: any[]): string {
  if (!content || !Array.isArray(content)) return "";

  return content.map(item => {
    if (typeof item === "string") return escapeHtml(item);
    if (item.type === "text") {
      let text = escapeHtml(item.text);
      if (item.styles?.bold) text = `<strong>${text}</strong>`;
      if (item.styles?.italic) text = `<em>${text}</em>`;
      if (item.styles?.underline) text = `<u>${text}</u>`;
      if (item.styles?.code) text = `<code style="background: #f4f4f4; padding: 2px 4px; border-radius: 3px;">${text}</code>`;
      return text;
    }
    if (item.type === "link") {
      return `<a href="${item.href}" style="color: #0066cc; text-decoration: underline;">${inlineToHtml(item.content)}</a>`;
    }
    return "";
  }).join("");
}

function escapeHtml(text: string): string {
  return text
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}
```

### 3.7 Migration: Email Template Page

```tsx
// Update: src/pages/portal/email-templates/EmailTemplateEditPage.tsx

// REMOVE these imports:
// import { Editor } from '@tinymce/tinymce-react'
// import type { Editor as TinyMCEEditor } from 'tinymce'
// import 'tinymce/tinymce'
// ... all tinymce imports

// ADD:
import { BlockEditor } from '@/components/editor'
import { blocksToEmailHtml, htmlToBlocks } from '@/components/editor/converters'

// In the component:
const [blocks, setBlocks] = useState<Block[]>([]);

// Initialize from existing HTML (migration)
useEffect(() => {
  if (template?.htmlBody) {
    const initialBlocks = htmlToBlocks(template.htmlBody);
    setBlocks(initialBlocks);
  }
}, [template]);

// In JSX, replace <Editor> with:
<BlockEditor
  initialContent={blocks}
  onChange={setBlocks}
  onHtmlChange={(html) => setHtmlBody(html)}
  variables={template?.availableVariables || []}
  showVariableInserter={true}
  uploadFolder="email-assets"
/>
```

### 3.8 Phase 3 Checklist

**Core Implementation:**
- [ ] Install BlockNote npm packages (`@blocknote/core`, `@blocknote/react`, `@blocknote/mantine`)
- [ ] Remove TinyMCE packages from `package.json`
- [ ] Create `BlockEditor` component with proper TypeScript types
- [ ] Create `VariableBlock` custom block for email templates
- [ ] Create `toEmailHtml` converter (inline CSS, table layout for Outlook)
- [ ] Create `toHtml` converter (for blog rendering)
- [ ] Create `fromHtml` converter (migration helper)
- [ ] Create `useBlockEditor` hook for state management
- [ ] Update `EmailTemplateEditPage` to use BlockNote
- [ ] Remove TinyMCE assets from `public/tinymce/`

**UI/UX (NOIR Frontend Rules):**
- [ ] Add editor styles/theming matching NOIR design system
- [ ] Add `cursor-pointer` to all interactive editor elements
- [ ] Ensure mobile responsiveness (editor works on tablets)
- [ ] Use 21st.dev patterns for any supporting UI components

**Security (CRITICAL):**
- [ ] Sanitize HTML output (prevent XSS in blog posts)
- [ ] Validate BlockNote JSON schema before saving
- [ ] Escape user content in email HTML output

**Accessibility:**
- [ ] Test keyboard navigation in editor
- [ ] Ensure proper ARIA labels on toolbar buttons
- [ ] Test with screen reader (VoiceOver/NVDA)

**Testing:**
- [ ] Test email template editing flow
- [ ] Test image upload in editor
- [ ] Verify email HTML output renders correctly in:
  - [ ] Gmail (web)
  - [ ] Outlook (desktop & web)
  - [ ] Apple Mail
- [ ] Test copy/paste from Word/Google Docs
- [ ] Test undo/redo functionality

---

## Phase 4: Blog/CMS Feature

### 4.1 Overview

Full-featured blog/CMS with SEO optimization, using BlockNote editor and image processing.

### 4.2 Domain Entities

```
src/NOIR.Domain/Entities/
├── Post.cs                        # Blog post entity
├── PostCategory.cs                # Hierarchical categories
├── PostTag.cs                     # Tags (many-to-many)
├── PostRevision.cs                # Version history
├── PostMedia.cs                   # Media attachments
└── SeoMetadata.cs                 # Embedded SEO fields
```

### 4.3 Entity Definitions

```csharp
// src/NOIR.Domain/Entities/Post.cs
public class Post : AggregateRoot<Guid>
{
    // Basic fields
    public string Title { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Excerpt { get; private set; }
    public string ContentJson { get; private set; } = default!;  // BlockNote JSON
    public string? ContentHtml { get; private set; }              // Rendered HTML (cached)

    // Featured image
    public string? FeaturedImageUrl { get; private set; }
    public string? FeaturedImageAlt { get; private set; }

    // Status
    public PostStatus Status { get; private set; } = PostStatus.Draft;
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? ScheduledAt { get; private set; }

    // Author
    public Guid AuthorId { get; private set; }

    // SEO (embedded value object)
    public SeoMetadata Seo { get; private set; } = new();

    // Metrics
    public int ViewCount { get; private set; }
    public int ReadingTimeMinutes { get; private set; }

    // Relationships
    public Guid? CategoryId { get; private set; }
    public PostCategory? Category { get; private set; }
    private readonly List<PostTag> _tags = [];
    public IReadOnlyCollection<PostTag> Tags => _tags.AsReadOnly();

    // Multi-tenancy
    public string TenantId { get; private set; } = default!;

    private Post() { }

    public static Post Create(
        string title,
        string slug,
        string contentJson,
        Guid authorId,
        string tenantId)
    {
        return new Post
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug.ToLowerInvariant(),
            ContentJson = contentJson,
            AuthorId = authorId,
            TenantId = tenantId,
            Status = PostStatus.Draft,
            ReadingTimeMinutes = CalculateReadingTime(contentJson)
        };
    }

    public void Publish()
    {
        Status = PostStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateContent(string title, string contentJson, string? excerpt)
    {
        Title = title;
        ContentJson = contentJson;
        Excerpt = excerpt;
        ReadingTimeMinutes = CalculateReadingTime(contentJson);
        ContentHtml = null; // Clear cached HTML
    }

    public void UpdateSeo(SeoMetadata seo)
    {
        Seo = seo;
    }

    public void SetFeaturedImage(string url, string? alt)
    {
        FeaturedImageUrl = url;
        FeaturedImageAlt = alt;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    private static int CalculateReadingTime(string contentJson)
    {
        // Rough estimate: 200 words per minute
        var wordCount = System.Text.Json.JsonDocument.Parse(contentJson)
            .RootElement.GetRawText().Split(' ').Length;
        return Math.Max(1, wordCount / 200);
    }
}

public enum PostStatus
{
    Draft,
    Published,
    Scheduled,
    Archived
}
```

```csharp
// src/NOIR.Domain/Entities/SeoMetadata.cs (Value Object)
public class SeoMetadata
{
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? CanonicalUrl { get; private set; }
    public string? OgTitle { get; private set; }
    public string? OgDescription { get; private set; }
    public string? OgImage { get; private set; }
    public string? TwitterCard { get; private set; } = "summary_large_image";
    public bool NoIndex { get; private set; }
    public bool NoFollow { get; private set; }
    public string? FocusKeyword { get; private set; }

    public static SeoMetadata Create(
        string? metaTitle,
        string? metaDescription,
        string? focusKeyword = null)
    {
        return new SeoMetadata
        {
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            FocusKeyword = focusKeyword,
            OgTitle = metaTitle,
            OgDescription = metaDescription
        };
    }
}
```

```csharp
// src/NOIR.Domain/Entities/PostCategory.cs
public class PostCategory : AggregateRoot<Guid>
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }

    // Hierarchical
    public Guid? ParentId { get; private set; }
    public PostCategory? Parent { get; private set; }
    private readonly List<PostCategory> _children = [];
    public IReadOnlyCollection<PostCategory> Children => _children.AsReadOnly();

    // SEO
    public SeoMetadata Seo { get; private set; } = new();

    // Multi-tenancy
    public string TenantId { get; private set; } = default!;

    private PostCategory() { }

    public static PostCategory Create(string name, string slug, string tenantId, Guid? parentId = null)
    {
        return new PostCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            TenantId = tenantId,
            ParentId = parentId
        };
    }
}
```

```csharp
// src/NOIR.Domain/Entities/PostTag.cs
public class PostTag : Entity<Guid>
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string TenantId { get; private set; } = default!;

    // Many-to-many with Posts (join handled by EF)
    private readonly List<Post> _posts = [];
    public IReadOnlyCollection<Post> Posts => _posts.AsReadOnly();

    private PostTag() { }

    public static PostTag Create(string name, string tenantId)
    {
        return new PostTag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = name.ToLowerInvariant().Replace(" ", "-"),
            TenantId = tenantId
        };
    }
}
```

```csharp
// src/NOIR.Domain/Entities/PostRevision.cs
public class PostRevision : Entity<Guid>
{
    public Guid PostId { get; private set; }
    public Post Post { get; private set; } = default!;
    public int RevisionNumber { get; private set; }
    public string Title { get; private set; } = default!;
    public string ContentJson { get; private set; } = default!;
    public string? ChangeNote { get; private set; }
    public Guid ChangedById { get; private set; }

    private PostRevision() { }

    public static PostRevision Create(Post post, int revisionNumber, Guid changedById, string? changeNote = null)
    {
        return new PostRevision
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            RevisionNumber = revisionNumber,
            Title = post.Title,
            ContentJson = post.ContentJson,
            ChangeNote = changeNote,
            ChangedById = changedById
        };
    }
}
```

### 4.4 Schema.org JSON-LD (SEO Rich Snippets) - Enhanced

```typescript
// src/components/seo/BlogPostSchema.tsx
export function BlogPostSchema({ post }: { post: PostDetailDto }) {
  const baseUrl = window.location.origin;

  const schema = {
    "@context": "https://schema.org",
    "@type": "BlogPosting",
    "headline": post.seo?.metaTitle || post.title,
    "description": post.seo?.metaDescription || post.excerpt,
    "image": {
      "@type": "ImageObject",
      "url": post.featuredImageUrl,
      "width": post.featuredImageWidth || 1200,
      "height": post.featuredImageHeight || 630
    },
    "datePublished": post.publishedAt,
    "dateModified": post.modifiedAt,
    "author": {
      "@type": "Person",
      "name": post.authorName,
      "url": `${baseUrl}/author/${post.authorSlug}`
    },
    "publisher": {
      "@type": "Organization",
      "name": "NOIR",
      "logo": {
        "@type": "ImageObject",
        "url": `${baseUrl}/logo.png`,
        "width": 600,
        "height": 60
      }
    },
    "mainEntityOfPage": {
      "@type": "WebPage",
      "@id": `${baseUrl}/blog/${post.slug}`
    },
    "wordCount": post.wordCount,
    "timeRequired": `PT${post.readingTimeMinutes}M`,
    // Enhanced fields for better SEO (2025 best practices)
    "articleSection": post.categoryName,           // Category name
    "keywords": post.tags?.join(", "),             // From tags
    "isAccessibleForFree": true,                   // Paywall indicator
    "inLanguage": "en",                            // Language code
    // Speakable for voice assistants (Google Assistant, Alexa)
    "speakable": {
      "@type": "SpeakableSpecification",
      "cssSelector": [".article-title", ".article-excerpt"]
    }
  };

  // Remove undefined fields
  const cleanSchema = JSON.parse(JSON.stringify(schema));

  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(cleanSchema) }}
    />
  );
}
```

### 4.4.1 SEO Meta Tags Component (OpenGraph + Twitter)

```typescript
// src/components/seo/BlogPostMeta.tsx
import { Helmet } from 'react-helmet-async'

interface BlogPostMetaProps {
  post: PostDetailDto
  baseUrl: string
}

export function BlogPostMeta({ post, baseUrl }: BlogPostMetaProps) {
  const title = post.seo?.metaTitle || post.title
  const description = post.seo?.metaDescription || post.excerpt
  const imageUrl = post.featuredImageUrl
  const canonicalUrl = post.seo?.canonicalUrl || `${baseUrl}/blog/${post.slug}`

  return (
    <Helmet>
      {/* Basic SEO */}
      <title>{title}</title>
      <meta name="description" content={description} />
      <link rel="canonical" href={canonicalUrl} />

      {/* RSS Autodiscovery - CRITICAL for feed readers */}
      <link
        rel="alternate"
        type="application/rss+xml"
        title="NOIR Blog RSS Feed"
        href={`${baseUrl}/feed.xml`}
      />

      {/* OpenGraph (Facebook, LinkedIn) */}
      <meta property="og:type" content="article" />
      <meta property="og:title" content={post.seo?.ogTitle || title} />
      <meta property="og:description" content={post.seo?.ogDescription || description} />
      <meta property="og:url" content={canonicalUrl} />
      <meta property="og:site_name" content="NOIR" />
      {imageUrl && (
        <>
          <meta property="og:image" content={imageUrl} />
          {/* Image dimensions - REQUIRED for proper previews */}
          <meta property="og:image:width" content={String(post.featuredImageWidth || 1200)} />
          <meta property="og:image:height" content={String(post.featuredImageHeight || 630)} />
          <meta property="og:image:alt" content={post.featuredImageAlt || title} />
        </>
      )}
      <meta property="article:published_time" content={post.publishedAt} />
      <meta property="article:modified_time" content={post.modifiedAt} />
      <meta property="article:author" content={post.authorName} />
      <meta property="article:section" content={post.categoryName} />
      {post.tags?.map(tag => (
        <meta key={tag} property="article:tag" content={tag} />
      ))}

      {/* Twitter Card */}
      <meta name="twitter:card" content={post.seo?.twitterCard || "summary_large_image"} />
      <meta name="twitter:title" content={title} />
      <meta name="twitter:description" content={description} />
      {imageUrl && <meta name="twitter:image" content={imageUrl} />}

      {/* Robots */}
      {(post.seo?.noIndex || post.seo?.noFollow) && (
        <meta
          name="robots"
          content={[
            post.seo?.noIndex ? 'noindex' : 'index',
            post.seo?.noFollow ? 'nofollow' : 'follow'
          ].join(', ')}
        />
      )}
    </Helmet>
  )
}
```

### 4.4.2 RSS Autodiscovery in Layout

```typescript
// Add to src/layouts/PublicLayout.tsx or App.tsx
import { Helmet } from 'react-helmet-async'

export function PublicLayout({ children }: { children: React.ReactNode }) {
  return (
    <>
      <Helmet>
        {/* RSS Autodiscovery - allows browsers/readers to find RSS feed */}
        <link
          rel="alternate"
          type="application/rss+xml"
          title="NOIR Blog"
          href="/feed.xml"
        />
        {/* Sitemap hint for crawlers */}
        <link rel="sitemap" type="application/xml" href="/sitemap.xml" />
      </Helmet>
      {children}
    </>
  )
}
```

### 4.5 RSS Feed & Sitemap

```csharp
// src/NOIR.Web/Endpoints/FeedEndpoints.cs
public static class FeedEndpoints
{
    public static void MapFeedEndpoints(this WebApplication app)
    {
        // RSS Feed
        app.MapGet("/feed.xml", async (
            [FromServices] IRepository<Post, Guid> postRepo,
            [FromServices] IBaseUrlService baseUrl,
            CancellationToken ct) =>
        {
            var posts = await postRepo.ListAsync(
                new PublishedPostsSpec(page: 1, pageSize: 20), ct);

            var feed = new SyndicationFeed(
                "NOIR Blog",
                "Latest posts from NOIR",
                new Uri(baseUrl.GetBaseUrl()),
                posts.Select(p => new SyndicationItem(
                    p.Title,
                    p.Excerpt,
                    new Uri($"{baseUrl.GetBaseUrl()}/blog/{p.Slug}"),
                    p.Id.ToString(),
                    p.PublishedAt!.Value
                ))
            );

            using var sw = new StringWriter();
            using var xw = XmlWriter.Create(sw);
            new Rss20FeedFormatter(feed).WriteTo(xw);
            xw.Flush();

            return Results.Content(sw.ToString(), "application/rss+xml");
        })
        .CacheOutput("Feed")
        .WithName("RssFeed");

        // Sitemap
        app.MapGet("/sitemap.xml", async (
            [FromServices] IRepository<Post, Guid> postRepo,
            [FromServices] IRepository<PostCategory, Guid> categoryRepo,
            [FromServices] IBaseUrlService baseUrl,
            CancellationToken ct) =>
        {
            var posts = await postRepo.ListAsync(new PublishedPostsSpec(1, 1000), ct);
            var categories = await categoryRepo.ListAsync(new AllCategoriesSpec(), ct);

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            // Homepage
            sb.AppendLine($"  <url><loc>{baseUrl.GetBaseUrl()}</loc><priority>1.0</priority></url>");

            // Blog index
            sb.AppendLine($"  <url><loc>{baseUrl.GetBaseUrl()}/blog</loc><priority>0.9</priority></url>");

            // Categories
            foreach (var cat in categories)
            {
                sb.AppendLine($"  <url><loc>{baseUrl.GetBaseUrl()}/blog/category/{cat.Slug}</loc><priority>0.8</priority></url>");
            }

            // Posts
            foreach (var post in posts)
            {
                sb.AppendLine($"  <url>");
                sb.AppendLine($"    <loc>{baseUrl.GetBaseUrl()}/blog/{post.Slug}</loc>");
                sb.AppendLine($"    <lastmod>{post.ModifiedAt:yyyy-MM-dd}</lastmod>");
                sb.AppendLine($"    <priority>0.7</priority>");
                sb.AppendLine($"  </url>");
            }

            sb.AppendLine("</urlset>");

            return Results.Content(sb.ToString(), "application/xml");
        })
        .CacheOutput("Sitemap")
        .WithName("Sitemap");
    }
}
```

### 4.6 Application Layer Structure

```
src/NOIR.Application/Features/
└── Blog/
    ├── Commands/
    │   ├── CreatePost/
    │   │   ├── CreatePostCommand.cs
    │   │   ├── CreatePostCommandHandler.cs
    │   │   └── CreatePostCommandValidator.cs
    │   ├── UpdatePost/
    │   ├── PublishPost/
    │   ├── DeletePost/
    │   ├── CreateCategory/
    │   └── CreateTag/
    ├── Queries/
    │   ├── GetPosts/
    │   │   ├── GetPostsQuery.cs
    │   │   └── GetPostsQueryHandler.cs
    │   ├── GetPostBySlug/
    │   ├── GetCategories/
    │   └── GetTags/
    ├── Dtos/
    │   ├── PostDto.cs
    │   ├── PostListDto.cs
    │   ├── PostDetailDto.cs
    │   ├── CategoryDto.cs
    │   ├── TagDto.cs
    │   └── SeoMetadataDto.cs
    └── Specifications/
        ├── PublishedPostsSpec.cs
        ├── PostBySlugSpec.cs
        ├── PostsByCategorySpec.cs
        └── PostsByTagSpec.cs
```

### 4.5 Permissions

```csharp
// Add to: src/NOIR.Domain/Common/Permissions.cs
public static class Permissions
{
    // ... existing permissions ...

    // Blog
    public const string BlogRead = "blog:read";
    public const string BlogWrite = "blog:write";
    public const string BlogDelete = "blog:delete";
    public const string BlogPublish = "blog:publish";

    public static class Groups
    {
        // ... existing groups ...

        public static readonly IReadOnlyList<string> Blog =
            [BlogRead, BlogWrite, BlogDelete, BlogPublish];
    }
}
```

### 4.6 Frontend Structure

```
src/NOIR.Web/frontend/src/
├── pages/
│   └── portal/
│       └── blog/
│           ├── BlogDashboard.tsx          # List of posts with stats
│           ├── PostEditor.tsx             # Create/edit post
│           ├── CategoryManager.tsx        # Category CRUD
│           ├── TagManager.tsx             # Tag CRUD
│           └── components/
│               ├── PostList.tsx
│               ├── PostCard.tsx
│               ├── SeoPanel.tsx           # SEO fields sidebar
│               ├── FeaturedImagePicker.tsx
│               └── PublishPanel.tsx       # Status, scheduling
├── services/
│   └── blogApi.ts                         # API client functions
└── types/
    └── blog.ts                            # TypeScript types
```

### 4.7 SEO Panel Component

```tsx
// src/pages/portal/blog/components/SeoPanel.tsx
export function SeoPanel({ seo, onChange, title, excerpt, featuredImage }) {
  const [score, setScore] = useState(0);

  // Calculate SEO score
  useEffect(() => {
    let points = 0;
    if (seo.metaTitle?.length >= 30 && seo.metaTitle?.length <= 60) points += 20;
    if (seo.metaDescription?.length >= 120 && seo.metaDescription?.length <= 160) points += 20;
    if (seo.focusKeyword && title?.toLowerCase().includes(seo.focusKeyword.toLowerCase())) points += 20;
    if (featuredImage) points += 20;
    if (seo.metaDescription?.toLowerCase().includes(seo.focusKeyword?.toLowerCase() || '')) points += 20;
    setScore(points);
  }, [seo, title, featuredImage]);

  return (
    <div className="space-y-4 p-4 border rounded-lg">
      <div className="flex items-center justify-between">
        <h3 className="font-semibold">SEO</h3>
        <SeoScoreBadge score={score} />
      </div>

      <div className="space-y-3">
        <FormField label="Focus Keyword">
          <Input
            value={seo.focusKeyword || ''}
            onChange={(e) => onChange({ ...seo, focusKeyword: e.target.value })}
            placeholder="e.g., react hooks tutorial"
          />
        </FormField>

        <FormField label="Meta Title" hint={`${seo.metaTitle?.length || 0}/60`}>
          <Input
            value={seo.metaTitle || ''}
            onChange={(e) => onChange({ ...seo, metaTitle: e.target.value })}
            maxLength={60}
          />
        </FormField>

        <FormField label="Meta Description" hint={`${seo.metaDescription?.length || 0}/160`}>
          <Textarea
            value={seo.metaDescription || ''}
            onChange={(e) => onChange({ ...seo, metaDescription: e.target.value })}
            maxLength={160}
            rows={3}
          />
        </FormField>

        {/* Preview */}
        <GooglePreview
          title={seo.metaTitle || title}
          description={seo.metaDescription || excerpt}
          url={`/blog/${slug}`}
        />
      </div>
    </div>
  );
}
```

### 4.9 Phase 4 Checklist

**Domain & Infrastructure:**
- [ ] Create `Post` entity with factory methods
- [ ] Create `PostCategory` entity (hierarchical)
- [ ] Create `PostTag` entity
- [ ] Create `PostRevision` entity
- [ ] Create `SeoMetadata` value object
- [ ] Create EF configurations for all entities
- [ ] Add migration
- [ ] Create Blog permissions (Blog.Create, Blog.Edit, Blog.Publish, Blog.Delete)
- [ ] Seed Blog permissions

**Application Layer:**
- [ ] Create specifications (PublishedPostsSpec, PostBySlugSpec, etc.)
- [ ] Create Post Commands (Create, Update, Publish, Delete)
- [ ] Create Category/Tag Commands
- [ ] Create Queries (GetPosts, GetBySlug, etc.)
- [ ] Create DTOs and mappers
- [ ] Create SlugGenerator service

**NOIR Patterns (CRITICAL - See CLAUDE.md):**
- [ ] All specifications use `.TagWith("MethodName")` for SQL debugging
- [ ] Specs for mutations use `.AsTracking()` for change detection
- [ ] All Commands implement `IAuditableCommand` (Activity Timeline)
- [ ] All Commands have co-located FluentValidation validators
- [ ] Command handlers use `IUnitOfWork.SaveChangesAsync()` for persistence
- [ ] Use soft delete only (set `IsDeleted = true`, never hard delete)
- [ ] Add `IScopedService` marker to all services
- [ ] Frontend pages call `usePageContext('Blog')` for audit context

**API Endpoints:**
- [ ] Create BlogEndpoints (posts, categories, tags)
- [ ] Create FeedEndpoints (RSS, Sitemap)
- [ ] Integrate caching for blog queries

**Frontend:**
- [ ] Create BlogDashboard page (list posts with stats)
- [ ] Create PostEditor page (create/edit)
- [ ] Create SeoPanel component with score
- [ ] Create FeaturedImagePicker component
- [ ] Create BlogPostSchema component (enhanced JSON-LD with speakable, articleSection)
- [ ] Create BlogPostMeta component (OpenGraph + Twitter with image dimensions)
- [ ] Add RSS autodiscovery link to PublicLayout
- [ ] Install react-helmet-async for meta tag management
- [ ] Integrate BlockNote editor
- [ ] Update navigation/routing

**SEO Enhancements (2025 Best Practices):**
- [ ] Add `featuredImageWidth`, `featuredImageHeight`, `featuredImageAlt` to PostDetailDto
- [ ] Add `categoryName`, `authorSlug` to PostDetailDto for schema
- [ ] Implement `og:image:width` and `og:image:height` meta tags
- [ ] Implement RSS autodiscovery `<link rel="alternate">` in layout
- [ ] Implement sitemap hint `<link rel="sitemap">` in layout
- [ ] Add `speakable` schema for voice assistants
- [ ] Add `articleSection`, `keywords`, `isAccessibleForFree` to JSON-LD

**Testing:**
- [ ] Add unit tests (handlers, specifications)
- [ ] Add integration tests (endpoints)
- [ ] Test RSS/Sitemap generation
- [ ] Validate JSON-LD with Google Rich Results Test
- [ ] Validate OpenGraph with Facebook Debugger

**Future Enhancements (Optional - Consider for V2):**
- [ ] Scheduled publishing (future publish dates with background job)
- [ ] Draft auto-save (periodic save while editing)
- [ ] Related posts feature (by category/tags)
- [ ] Reading progress indicator
- [ ] Social sharing buttons component
- [ ] Comment system (or third-party integration like Disqus)
- [ ] Author bio/profile page
- [ ] Post series/collections feature
- [ ] Table of contents auto-generation from headings

---

## Phase 5: Performance Hardening

### 5.1 Overview

Final optimization pass to ensure production-ready performance.

### 5.2 Tasks

#### 5.2.1 Response Compression

```csharp
// Add to Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
        "application/json",
        "text/html",
        "text/css",
        "application/javascript"
    ]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// In pipeline
app.UseResponseCompression();
```

#### 5.2.2 Static File Caching

```csharp
// Add to Program.cs
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var headers = ctx.Context.Response.GetTypedHeaders();
        headers.CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromDays(365)
        };

        // Add ETag support
        var lastModified = ctx.File.LastModified;
        headers.LastModified = lastModified;
        headers.ETag = new EntityTagHeaderValue($"\"{lastModified.Ticks}\"");
    }
});
```

#### 5.2.3 Database Index Review

```csharp
// Create migration for missing indexes
public partial class AddPerformanceIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Posts - frequently queried columns
        migrationBuilder.CreateIndex(
            name: "IX_Posts_Status_PublishedAt",
            table: "Posts",
            columns: new[] { "Status", "PublishedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Posts_TenantId_Slug",
            table: "Posts",
            columns: new[] { "TenantId", "Slug" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Posts_AuthorId",
            table: "Posts",
            column: "AuthorId");

        // Categories
        migrationBuilder.CreateIndex(
            name: "IX_PostCategories_TenantId_Slug",
            table: "PostCategories",
            columns: new[] { "TenantId", "Slug" },
            unique: true);

        // Tags
        migrationBuilder.CreateIndex(
            name: "IX_PostTags_TenantId_Slug",
            table: "PostTags",
            columns: new[] { "TenantId", "Slug" },
            unique: true);
    }
}
```

#### 5.2.4 Load Testing Script

```javascript
// k6 load test script: tests/load/blog-api.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 },   // Ramp up
    { duration: '1m', target: 50 },    // Stay at 50 users
    { duration: '30s', target: 100 },  // Peak
    { duration: '30s', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],  // 95% of requests under 500ms
    http_req_failed: ['rate<0.01'],     // Less than 1% failures
  },
};

export default function () {
  // Get posts list
  const postsRes = http.get('http://localhost:4000/api/blog/public/posts');
  check(postsRes, {
    'posts status 200': (r) => r.status === 200,
    'posts response time < 200ms': (r) => r.timings.duration < 200,
  });

  // Get single post
  const postRes = http.get('http://localhost:4000/api/blog/public/posts/sample-post');
  check(postRes, {
    'post status 200': (r) => r.status === 200,
  });

  sleep(1);
}
```

### 5.3 Phase 5 Checklist

**Response Optimization:**
- [ ] Add response compression (Brotli + gzip)
- [ ] Configure static file caching with ETag and long max-age
- [ ] Add output caching for public blog endpoints
- [ ] Configure CDN headers (if using CDN like Cloudflare)

**Database Performance:**
- [ ] Review and add database indexes (check query plans)
- [ ] Optimize specifications with projections (select only needed columns)
- [ ] Review N+1 query issues (use `.AsSplitQuery()` where needed)
- [ ] Add index on `Post.Slug`, `Post.PublishedAt`, `Post.TenantId`
- [ ] Add composite index for common query patterns

**Load Testing:**
- [ ] Create k6 load test scripts for:
  - [ ] Blog list page (paginated)
  - [ ] Blog detail page
  - [ ] RSS/Sitemap endpoints
  - [ ] Image upload flow
- [ ] Run load tests and document baselines
- [ ] Test cache hit ratios under load

**Quality Assurance:**
- [ ] Run Stryker mutation testing
- [ ] Achieve >80% mutation score for critical paths
- [ ] Document performance benchmarks

**Production Readiness:**
- [ ] Add health check endpoints (`/health`, `/health/db`, `/health/cache`)
- [ ] Configure structured logging for performance metrics
- [ ] Set up error alerting (Sentry, Application Insights, etc.)
- [ ] Review connection pooling configuration
- [ ] Add request timeout middleware
- [ ] Test graceful shutdown behavior

**Security Hardening:**
- [ ] Enable HSTS headers
- [ ] Review CSP (Content Security Policy) headers
- [ ] Ensure rate limiting on upload endpoints
- [ ] Review CORS configuration

---

## Summary: Implementation Order

| Phase | Feature | Est. Files | Dependencies |
|-------|---------|------------|--------------|
| 1 | Caching Infrastructure | ~10 | None |
| 2 | Image Processing | ~8 | Phase 1 (cache results) |
| 3 | BlockNote Editor | ~15 | Phase 2 (image upload) |
| 4 | Blog/CMS | ~40 | Phase 2, 3 |
| 5 | Performance | ~5 | All phases |

---

## Next Steps

**Plan Status:** Approved and ready for implementation.

**Implementation Order:**
1. Phase 1: Caching Infrastructure
2. Phase 2: Image Processing Service
3. Phase 3: BlockNote Editor Component
4. Phase 4: Blog/CMS Feature
5. Phase 5: Performance Hardening

*Ready to begin Phase 1 implementation.*
