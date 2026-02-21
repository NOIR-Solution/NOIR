using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Generates placeholder images for seed data using SixLabors.ImageSharp.
/// Creates solid-color WebP rectangles — no external image assets required.
/// </summary>
public static class SeedImageHelper
{
    /// <summary>
    /// Generates a solid-color placeholder image as a WebP-encoded MemoryStream.
    /// Uses direct pixel manipulation (no SixLabors.ImageSharp.Drawing dependency).
    /// </summary>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="hexColor">Fill color in hex format (e.g., "#4A90D9").</param>
    /// <param name="text">Optional text label (reserved for future use — SixLabors.Fonts not available).</param>
    /// <returns>A MemoryStream positioned at 0 containing WebP image data.</returns>
    public static MemoryStream GeneratePlaceholder(int width, int height, string hexColor, string? text = null)
    {
        var color = Color.ParseHex(hexColor).ToPixel<Rgba32>();

        using var image = new Image<Rgba32>(width, height);

        // Fill all pixels with the solid color via direct buffer access
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                row.Fill(color);
            }
        });

        var memoryStream = new MemoryStream();
        image.SaveAsWebp(memoryStream, new WebpEncoder { Quality = 75 });
        memoryStream.Position = 0;

        return memoryStream;
    }

    /// <summary>
    /// Generates a placeholder image, processes it through IImageProcessor, and returns the result.
    /// Shared between CatalogSeedModule and BlogSeedModule to avoid duplication.
    /// Returns null if image processing fails or imageProcessor is null.
    /// </summary>
    public static async Task<ImageProcessingResult?> GenerateAndProcessAsync(
        IImageProcessor? imageProcessor,
        int width, int height, string hexColor, string altText,
        string slug, string storageFolder,
        ILogger logger, CancellationToken ct)
    {
        if (imageProcessor == null) return null;

        try
        {
            using var imageStream = GeneratePlaceholder(width, height, hexColor, altText);
            var result = await imageProcessor.ProcessAsync(imageStream, $"{slug}.webp", new ImageProcessingOptions
            {
                StorageFolder = storageFolder,
                Variants = [ImageVariant.Thumb, ImageVariant.Medium, ImageVariant.Large],
                Formats = [OutputFormat.WebP],
                GenerateThumbHash = true,
                ExtractDominantColor = true,
                PreserveOriginal = false
            }, ct);

            return result.Success && result.Variants.Count > 0 ? result : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[SeedData] Failed to generate image for {Slug}, skipping", slug);
            return null;
        }
    }

    /// <summary>
    /// Extracts the primary URL from an image processing result (prefers Large variant).
    /// </summary>
    public static string? GetPrimaryUrl(ImageProcessingResult result) =>
        result.Variants.FirstOrDefault(v => v.Variant == ImageVariant.Large)?.Url
        ?? result.Variants[0].Url;
}
