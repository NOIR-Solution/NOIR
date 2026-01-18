namespace NOIR.Web.Endpoints;

/// <summary>
/// Unified media upload endpoint with image processing.
/// All image uploads (blog, avatars, content) go through this single API.
/// </summary>
public static class MediaEndpoints
{
    private static readonly string[] AllowedFolders = ["blog", "content", "avatars"];
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

                // Create response (includes "location" alias for TinyMCE compatibility)
                var response = MediaUploadResultDto.FromProcessingResult(result, absoluteUrl);

                logger.LogInformation(
                    "Image uploaded and processed: {Slug} -> {VariantCount} variants in {Ms}ms",
                    result.Slug,
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
            "blog" or "content" => new ImageProcessingOptions
            {
                // Blog/content: Thumb + Full HD, WebP only (97% browser support)
                Variants = [ImageVariant.Thumb, ImageVariant.ExtraLarge],
                Formats = [OutputFormat.WebP], // Skip JPEG - faster upload
                GenerateThumbHash = true,
                ExtractDominantColor = false,
                PreserveOriginal = false,
                StorageFolder = storageFolder
            },
            _ => new ImageProcessingOptions
            {
                // Default: Thumb + Full HD with both formats
                Variants = [ImageVariant.Thumb, ImageVariant.ExtraLarge],
                Formats = [OutputFormat.WebP, OutputFormat.Jpeg],
                GenerateThumbHash = true,
                ExtractDominantColor = false,
                PreserveOriginal = false,
                StorageFolder = storageFolder
            }
        };
    }
}
