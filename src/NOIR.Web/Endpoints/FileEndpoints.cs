namespace NOIR.Web.Endpoints;

/// <summary>
/// Endpoints for serving uploaded media files (avatars, blog images, etc.)
/// Route is configurable via Storage:MediaUrlPrefix setting (default: /media)
/// </summary>
public static class FileEndpoints
{
    public static void MapFileEndpoints(this WebApplication app)
    {
        // Get media URL prefix from settings (default: /media)
        var storageSettings = app.Configuration
            .GetSection(StorageSettings.SectionName)
            .Get<StorageSettings>() ?? new StorageSettings();

        var mediaPrefix = storageSettings.MediaUrlPrefix.TrimStart('/');

        var group = app.MapGroup($"/{mediaPrefix}")
            .WithTags("Media Files");

        // Serve files from storage (publicly accessible)
        group.MapGet("/{*path}", async (
            string path,
            [FromServices] IFileStorage fileStorage,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Security: Only allow specific folders to be served publicly
            var allowedPrefixes = new[] { "avatars/", "blog/", "content/", "images/" };
            if (!allowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                return Results.NotFound();
            }

            // Check if file exists
            if (!await fileStorage.ExistsAsync(path, cancellationToken))
            {
                return Results.NotFound();
            }

            // Get file stream
            var stream = await fileStorage.DownloadAsync(path, cancellationToken);
            if (stream is null)
            {
                return Results.NotFound();
            }

            // Determine content type
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".avif" => "image/avif",
                ".svg" => "image/svg+xml",
                ".heic" => "image/heic",
                ".heif" => "image/heif",
                _ => "application/octet-stream"
            };

            // Set cache headers for images (1 year since files have unique slugs/GUIDs)
            context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");

            return Results.File(stream, contentType);
        })
        .WithName("ServeMediaFile")
        .WithSummary("Serve uploaded media file")
        .WithDescription("Serves publicly accessible uploaded files like avatars and blog images.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
