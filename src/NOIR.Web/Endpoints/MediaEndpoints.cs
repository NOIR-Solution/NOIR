namespace NOIR.Web.Endpoints;

/// <summary>
/// Unified media upload endpoint with image processing.
/// All image uploads (blog, avatars, content) go through this single API.
/// </summary>
public static class MediaEndpoints
{
    private static readonly string[] AllowedFolders = ["blog", "content", "avatars", "branding"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public static void MapMediaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/media")
            .WithTags("Media")
            .RequireAuthorization();

        // Unified image upload with processing
        group.MapPost("/upload", async (
            HttpContext httpContext,
            IFormFile file,
            [FromQuery] string? folder,
            [FromQuery] string? entityId,
            [FromServices] IImageProcessor imageProcessor,
            [FromServices] IRepository<MediaFile, Guid> mediaFileRepository,
            [FromServices] IUnitOfWork unitOfWork,
            [FromServices] IMultiTenantContextAccessor multiTenantContext,
            [FromServices] ICurrentUser currentUser,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            // Validate user is authenticated
            if (string.IsNullOrEmpty(currentUser.UserId))
            {
                return Results.Unauthorized();
            }

            // Validate file
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(MediaUploadResultDto.Failure("No file provided"));
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return Results.BadRequest(MediaUploadResultDto.Failure("File size exceeds 10 MB limit"));
            }

            // Validate folder (default to content for editor uploads)
            var targetFolder = folder?.ToLowerInvariant() ?? "content";
            if (!AllowedFolders.Contains(targetFolder))
            {
                return Results.BadRequest(MediaUploadResultDto.Failure(
                    $"Invalid folder. Allowed: {string.Join(", ", AllowedFolders)}"));
            }

            // Determine storage folder
            var storageFolder = targetFolder;
            if (targetFolder == "avatars")
            {
                // Use entityId if provided, otherwise current user
                var userId = !string.IsNullOrEmpty(entityId) ? entityId : currentUser.UserId;
                storageFolder = $"{targetFolder}/{userId}";
            }
            else if (targetFolder == "branding")
            {
                // Use tenant ID for branding assets
                var tenantId = multiTenantContext.MultiTenantContext?.TenantInfo?.Id ?? "default";
                storageFolder = $"{targetFolder}/{tenantId}";
            }

            try
            {
                await using var stream = file.OpenReadStream();

                // Validate it's a supported image
                if (!await imageProcessor.IsValidImageAsync(stream, file.FileName))
                {
                    return Results.BadRequest(MediaUploadResultDto.Failure(
                        "Invalid or unsupported image format. Allowed: JPG, PNG, GIF, WebP, AVIF"));
                }

                // Reset stream position for processing
                stream.Position = 0;

                // Configure processing options based on folder
                var options = GetProcessingOptions(targetFolder, storageFolder);

                // Process the image
                var result = await imageProcessor.ProcessAsync(
                    stream,
                    file.FileName,
                    options,
                    cancellationToken);

                if (!result.Success)
                {
                    logger.LogError("Image processing failed for {FileName}: {Error}",
                        file.FileName, result.ErrorMessage);
                    return Results.BadRequest(MediaUploadResultDto.Failure(
                        result.ErrorMessage ?? "Image processing failed"));
                }

                // Determine default URL (prefer WebP large, fallback to first available)
                var defaultVariant = result.Variants
                    .Where(v => v.Format == OutputFormat.WebP)
                    .OrderByDescending(v => v.Width)
                    .FirstOrDefault()
                    ?? result.Variants.FirstOrDefault();

                var relativeUrl = defaultVariant?.Url ?? defaultVariant?.Path ?? string.Empty;

                // Build absolute URL to prevent path resolution issues in rich text editors
                var request = httpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";
                var absoluteUrl = string.IsNullOrEmpty(relativeUrl) ? string.Empty : $"{baseUrl}{relativeUrl}";

                // Serialize variants and srcsets for database storage
                var variantsJson = System.Text.Json.JsonSerializer.Serialize(
                    result.Variants.Select(v => new
                    {
                        variant = v.Variant.ToString().ToLowerInvariant(),
                        format = v.Format.ToString().ToLowerInvariant(),
                        url = v.Url ?? v.Path,
                        width = v.Width,
                        height = v.Height,
                        sizeBytes = v.SizeBytes
                    }));

                // Generate srcsets dictionary
                var srcsets = new Dictionary<string, string>();
                var variantsByFormat = result.Variants.GroupBy(v => v.Format);
                foreach (var formatGroup in variantsByFormat)
                {
                    var formatName = formatGroup.Key.ToString().ToLowerInvariant();
                    var srcsetParts = formatGroup
                        .OrderBy(v => v.Width)
                        .Select(v => $"{v.Url ?? v.Path} {v.Width}w");
                    srcsets[formatName] = string.Join(", ", srcsetParts);
                }
                var srcsetsJson = System.Text.Json.JsonSerializer.Serialize(srcsets);

                // Create MediaFile entity
                // Extract shortId from slug (after underscore): "hero-banner_a1b2c3d4" → "a1b2c3d4"
                var shortId = NOIR.Infrastructure.Media.SlugGenerator.ExtractShortId(result.Slug) ??
                    NOIR.Infrastructure.Media.SlugGenerator.GenerateShortId();
                var tenantId = multiTenantContext.MultiTenantContext?.TenantInfo?.Id;
                var mediaFile = MediaFile.Create(
                    shortId: shortId,
                    slug: result.Slug,
                    originalFileName: file.FileName,
                    folder: targetFolder,
                    defaultUrl: relativeUrl, // Store relative URL in DB
                    thumbHash: result.ThumbHash,
                    dominantColor: result.DominantColor,
                    width: result.Metadata?.Width ?? 0,
                    height: result.Metadata?.Height ?? 0,
                    format: result.Metadata?.Format ?? "unknown",
                    mimeType: result.Metadata?.MimeType ?? "application/octet-stream",
                    sizeBytes: result.Metadata?.SizeBytes ?? file.Length,
                    hasTransparency: result.Metadata?.HasTransparency ?? false,
                    variantsJson: variantsJson,
                    srcsetsJson: srcsetsJson,
                    uploadedBy: currentUser.UserId!,
                    tenantId: tenantId);

                // Save to database
                await mediaFileRepository.AddAsync(mediaFile, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                // Create response (includes "location" alias for TinyMCE compatibility)
                var response = MediaUploadResultDto.FromProcessingResult(result, absoluteUrl, mediaFile.Id, shortId);

                logger.LogInformation(
                    "Image uploaded and processed: {Slug} (ID: {MediaFileId}) -> {VariantCount} variants in {Ms}ms",
                    result.Slug,
                    mediaFile.Id,
                    result.Variants.Count,
                    result.ProcessingTimeMs);

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Upload failed for {FileName}", file.FileName);
                return Results.Problem($"Upload failed: {ex.Message}");
            }
        })
        .WithName("UploadMedia")
        .WithSummary("Upload and process image")
        .WithDescription("""
            Unified image upload endpoint with automatic processing.
            - Generates multiple size variants (thumb, small, medium, large, xl)
            - Generates multiple formats (AVIF, WebP, JPEG)
            - Creates ThumbHash placeholder for loading states
            - Extracts dominant color
            - Creates SEO-friendly filenames

            Response includes "location" for TinyMCE compatibility.

            Folders:
            - blog: Blog post images (full processing)
            - content: General content images (full processing)
            - avatars: User profile pictures (optimized - fewer variants)
            """)
        .DisableAntiforgery()
        .Produces<MediaUploadResultDto>(StatusCodes.Status200OK)
        .Produces<MediaUploadResultDto>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);

        // Get MediaFile by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] IRepository<MediaFile, Guid> repository,
            CancellationToken cancellationToken) =>
        {
            var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByIdSpec(id);
            var mediaFile = await repository.FirstOrDefaultAsync(spec, cancellationToken);

            if (mediaFile is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(ToMediaFileDto(mediaFile));
        })
        .WithName("GetMediaFile")
        .WithSummary("Get media file by ID")
        .WithDescription("Returns full metadata for a media file including variants, srcsets, and placeholders.")
        .Produces<MediaFileDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Get MediaFile by slug (for lookup from URL)
        group.MapGet("/by-slug/{slug}", async (
            string slug,
            [FromServices] IRepository<MediaFile, Guid> repository,
            CancellationToken cancellationToken) =>
        {
            var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileBySlugSpec(slug);
            var mediaFile = await repository.FirstOrDefaultAsync(spec, cancellationToken);

            if (mediaFile is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(ToMediaFileDto(mediaFile));
        })
        .WithName("GetMediaFileBySlug")
        .WithSummary("Get media file by slug")
        .WithDescription("""
            Lookup media file metadata by slug.
            The slug can be extracted from the image URL filename.
            
            Example: URL `/media/blog/hero-banner-xl.webp` → slug = `hero-banner`
            """)
        .Produces<MediaFileDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Lookup MediaFile by URL (most convenient for frontend)
        group.MapGet("/by-url", async (
            [FromQuery] string url,
            [FromServices] IRepository<MediaFile, Guid> repository,
            CancellationToken cancellationToken) =>
        {
            // Extract slug from URL: /media/blog/hero-banner-xl.webp → hero-banner
            var slug = ExtractSlugFromUrl(url);
            if (string.IsNullOrEmpty(slug))
            {
                return Results.BadRequest("Could not extract slug from URL");
            }

            var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileBySlugSpec(slug);
            var mediaFile = await repository.FirstOrDefaultAsync(spec, cancellationToken);

            if (mediaFile is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(ToMediaFileDto(mediaFile));
        })
        .WithName("GetMediaFileByUrl")
        .WithSummary("Get media file by URL")
        .WithDescription("""
            Lookup media file metadata by its URL.
            Pass the full or relative URL and the API will extract the slug and return metadata.
            
            Example: `?url=/media/blog/hero-banner-xl.webp`
            """)
        .Produces<MediaFileDto>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // Get MediaFile by shortId (8-char unique identifier)
        group.MapGet("/by-short-id/{shortId}", async (
            string shortId,
            [FromServices] IRepository<MediaFile, Guid> repository,
            CancellationToken cancellationToken) =>
        {
            var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByShortIdSpec(shortId);
            var mediaFile = await repository.FirstOrDefaultAsync(spec, cancellationToken);

            if (mediaFile is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(ToMediaFileDto(mediaFile));
        })
        .WithName("GetMediaFileByShortId")
        .WithSummary("Get media file by short ID")
        .WithDescription("""
            Lookup media file metadata by its 8-character short ID.
            The short ID can be extracted from the slug (after the underscore).

            Example: slug `hero-banner_a1b2c3d4` → shortId = `a1b2c3d4`
            """)
        .Produces<MediaFileDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Batch query: Get multiple MediaFiles by IDs
        group.MapPost("/batch/by-ids", async (
            [FromBody] BatchMediaIdsRequest request,
            [FromServices] IRepository<MediaFile, Guid> repository,
            CancellationToken cancellationToken) =>
        {
            var validationError = ValidateBatchRequest(request.Ids, "ID");
            if (validationError is not null) return validationError;

            var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByIdsSpec(request.Ids);
            var mediaFiles = await repository.ListAsync(spec, cancellationToken);

            var result = mediaFiles.Select(ToMediaFileDto).ToList();
            return Results.Ok(result);
        })
        .WithName("GetMediaFilesByIds")
        .WithSummary("Get multiple media files by IDs")
        .WithDescription("Batch lookup of media files by their GUIDs. Maximum 100 IDs per request.")
        .Produces<List<MediaFileDto>>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // Batch query: Get multiple MediaFiles by slugs
        group.MapPost("/batch/by-slugs", async (
            [FromBody] BatchMediaSlugsRequest request,
            [FromServices] IRepository<MediaFile, Guid> repository,
            CancellationToken cancellationToken) =>
        {
            var validationError = ValidateBatchRequest(request.Slugs, "slug");
            if (validationError is not null) return validationError;

            var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesBySlugsSpec(request.Slugs);
            var mediaFiles = await repository.ListAsync(spec, cancellationToken);

            var result = mediaFiles.Select(ToMediaFileDto).ToList();
            return Results.Ok(result);
        })
        .WithName("GetMediaFilesBySlugs")
        .WithSummary("Get multiple media files by slugs")
        .WithDescription("Batch lookup of media files by their slugs. Maximum 100 slugs per request.")
        .Produces<List<MediaFileDto>>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest);

        // Batch query: Get multiple MediaFiles by short IDs
        group.MapPost("/batch/by-short-ids", async (
            [FromBody] BatchMediaShortIdsRequest request,
            [FromServices] IRepository<MediaFile, Guid> repository,
            CancellationToken cancellationToken) =>
        {
            var validationError = ValidateBatchRequest(request.ShortIds, "short ID");
            if (validationError is not null) return validationError;

            var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByShortIdsSpec(request.ShortIds);
            var mediaFiles = await repository.ListAsync(spec, cancellationToken);

            var result = mediaFiles.Select(ToMediaFileDto).ToList();
            return Results.Ok(result);
        })
        .WithName("GetMediaFilesByShortIds")
        .WithSummary("Get multiple media files by short IDs")
        .WithDescription("""
            Batch lookup of media files by their 8-character short IDs.
            Maximum 100 short IDs per request.

            Example: Extract short IDs from URLs or slugs, then batch query:
            - slug `hero-banner_a1b2c3d4` → shortId `a1b2c3d4`
            - URL `/media/blog/hero-banner_a1b2c3d4-xl.webp` → shortId `a1b2c3d4`
            """)
        .Produces<List<MediaFileDto>>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Convert MediaFile entity to DTO.
    /// </summary>
    private static MediaFileDto ToMediaFileDto(MediaFile mediaFile)
    {
        var variants = System.Text.Json.JsonSerializer.Deserialize<List<MediaVariantDto>>(
            mediaFile.VariantsJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var srcsets = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
            mediaFile.SrcsetsJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        return new MediaFileDto
        {
            Id = mediaFile.Id,
            ShortId = mediaFile.ShortId,
            Slug = mediaFile.Slug,
            OriginalFileName = mediaFile.OriginalFileName,
            Folder = mediaFile.Folder,
            DefaultUrl = mediaFile.DefaultUrl,
            ThumbHash = mediaFile.ThumbHash,
            DominantColor = mediaFile.DominantColor,
            Width = mediaFile.Width,
            Height = mediaFile.Height,
            AspectRatio = mediaFile.AspectRatio,
            Format = mediaFile.Format,
            MimeType = mediaFile.MimeType,
            SizeBytes = mediaFile.SizeBytes,
            HasTransparency = mediaFile.HasTransparency,
            AltText = mediaFile.AltText,
            Variants = variants,
            Srcsets = srcsets,
            CreatedAt = mediaFile.CreatedAt
        };
    }

    /// <summary>
    /// Extract slug from media URL.
    /// Example: /media/blog/hero-banner_a1b2c3d4-xl.webp → hero-banner_a1b2c3d4
    /// The slug contains the unique shortId after underscore.
    /// </summary>
    private static string? ExtractSlugFromUrl(string url)
    {
        try
        {
            // Handle both absolute and relative URLs
            var path = url;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                path = uri.AbsolutePath;
            }

            // Get filename: hero-banner_a1b2c3d4-xl.webp → hero-banner_a1b2c3d4-xl
            var filename = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(filename))
            {
                return null;
            }

            // Remove variant suffix: hero-banner_a1b2c3d4-xl → hero-banner_a1b2c3d4
            // Variants: thumb, small, medium, large, xl, extralarge, original
            var variantSuffixes = new[] { "-thumb", "-small", "-medium", "-large", "-xl", "-extralarge", "-original" };
            foreach (var suffix in variantSuffixes)
            {
                if (filename.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return filename[..^suffix.Length];
                }
            }

            // No variant suffix found, return as-is (slug is the full filename)
            return filename;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validate batch request collection size.
    /// </summary>
    private static IResult? ValidateBatchRequest<T>(IReadOnlyList<T>? collection, string itemName, int maxItems = 100)
    {
        if (collection is null || collection.Count == 0)
        {
            return Results.BadRequest($"At least one {itemName} is required");
        }

        if (collection.Count > maxItems)
        {
            return Results.BadRequest($"Maximum {maxItems} {itemName}s per request");
        }

        return null;
    }

    /// <summary>
    /// Get processing options based on the target folder.
    /// Optimized for speed: minimal processing for fast uploads.
    /// </summary>
    private static ImageProcessingOptions GetProcessingOptions(string folder, string storageFolder)
    {
        return folder switch
        {
            "avatars" => new ImageProcessingOptions
            {
                // Avatars: thumb (lists) + medium (profile display), WebP only
                Variants = [ImageVariant.Thumb, ImageVariant.Medium],
                Formats = [OutputFormat.WebP], // WebP only (97% support)
                GenerateThumbHash = false,
                ExtractDominantColor = false,
                PreserveOriginal = false,
                StorageFolder = storageFolder
            },
            "branding" => new ImageProcessingOptions
            {
                // Branding: medium for display, preserve original for favicon
                Variants = [ImageVariant.Thumb, ImageVariant.Medium],
                Formats = [OutputFormat.WebP],
                GenerateThumbHash = false,
                ExtractDominantColor = false,
                PreserveOriginal = true, // Keep original for favicon (ICO format)
                StorageFolder = storageFolder
            },
            "blog" or "content" => new ImageProcessingOptions
            {
                // Blog/content: Thumb + Full HD, WebP only (97% browser support)
                Variants = [ImageVariant.Thumb, ImageVariant.Large],
                Formats = [OutputFormat.WebP], // Skip JPEG - faster upload
                GenerateThumbHash = true,
                ExtractDominantColor = false,
                PreserveOriginal = false,
                StorageFolder = storageFolder
            },
            _ => new ImageProcessingOptions
            {
                // Default: Thumb + Full HD with both formats
                Variants = [ImageVariant.Thumb, ImageVariant.Large],
                Formats = [OutputFormat.WebP, OutputFormat.Jpeg],
                GenerateThumbHash = true,
                ExtractDominantColor = false,
                PreserveOriginal = false,
                StorageFolder = storageFolder
            }
        };
    }
}

/// <summary>
/// Request for batch lookup by media file IDs.
/// </summary>
public sealed record BatchMediaIdsRequest(IReadOnlyList<Guid> Ids);

/// <summary>
/// Request for batch lookup by media file slugs.
/// </summary>
public sealed record BatchMediaSlugsRequest(IReadOnlyList<string> Slugs);

/// <summary>
/// Request for batch lookup by media file short IDs.
/// </summary>
public sealed record BatchMediaShortIdsRequest(IReadOnlyList<string> ShortIds);
