namespace NOIR.Web.Endpoints;

/// <summary>
/// Endpoints for serving uploaded files (avatars, etc.)
/// </summary>
public static class FileEndpoints
{
    public static void MapFileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/files")
            .WithTags("Files");

        // Serve files from storage (publicly accessible)
        group.MapGet("/{*path}", async (
            string path,
            [FromServices] IFileStorage fileStorage,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Security: Only allow specific folders to be served publicly
            var allowedPrefixes = new[] { "avatars/" };
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
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };

            // Set cache headers for images (1 year since avatars have unique GUIDs)
            context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");

            return Results.File(stream, contentType);
        })
        .WithName("GetFile")
        .WithSummary("Get uploaded file")
        .WithDescription("Serves publicly accessible uploaded files like avatars.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}
