namespace NOIR.Infrastructure.Media;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

/// <summary>
/// Extracts dominant color from images using color quantization.
/// </summary>
public static class ColorAnalyzer
{
    /// <summary>
    /// Extract the dominant color from an image.
    /// </summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Hex color string (e.g., "#FF5733").</returns>
    public static async Task<string> ExtractDominantColorAsync(Stream inputStream, CancellationToken ct = default)
    {
        // Reset stream position
        if (inputStream.CanSeek)
            inputStream.Position = 0;

        using var image = await Image.LoadAsync<Rgba32>(inputStream, ct);

        // Resize to small size for faster processing
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(50, 50),
            Mode = ResizeMode.Max,
            Sampler = KnownResamplers.NearestNeighbor
        }));

        // Count colors using a dictionary
        var colorCounts = new Dictionary<uint, int>();

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];

                    // Skip transparent pixels
                    if (pixel.A < 128) continue;

                    // Skip very dark or very light pixels (likely background)
                    var brightness = (pixel.R + pixel.G + pixel.B) / 3;
                    if (brightness < 20 || brightness > 235) continue;

                    // Quantize to reduce color space (group similar colors)
                    var quantized = QuantizeColor(pixel);
                    var key = (uint)(quantized.R << 16 | quantized.G << 8 | quantized.B);

                    colorCounts.TryGetValue(key, out var count);
                    colorCounts[key] = count + 1;
                }
            }
        });

        if (colorCounts.Count == 0)
            return "#808080"; // Gray fallback

        // Find most common color
        var dominantKey = colorCounts.MaxBy(x => x.Value).Key;

        var r = (byte)(dominantKey >> 16);
        var g = (byte)(dominantKey >> 8);
        var b = (byte)dominantKey;

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>
    /// Quantize a color to reduce color space.
    /// Groups similar colors together by reducing precision.
    /// </summary>
    private static Rgba32 QuantizeColor(Rgba32 color)
    {
        // Reduce to 32 levels per channel (5-bit color)
        const int levels = 32;
        const int step = 256 / levels;

        var r = (byte)(color.R / step * step + step / 2);
        var g = (byte)(color.G / step * step + step / 2);
        var b = (byte)(color.B / step * step + step / 2);

        return new Rgba32(r, g, b, 255);
    }

    /// <summary>
    /// Extract a color palette from an image.
    /// </summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <param name="paletteSize">Number of colors in palette. Default: 5.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of hex color strings.</returns>
    public static async Task<IReadOnlyList<string>> ExtractPaletteAsync(
        Stream inputStream,
        int paletteSize = 5,
        CancellationToken ct = default)
    {
        // Reset stream position
        if (inputStream.CanSeek)
            inputStream.Position = 0;

        using var image = await Image.LoadAsync<Rgba32>(inputStream, ct);

        // Resize to small size for faster processing
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(100, 100),
            Mode = ResizeMode.Max,
            Sampler = KnownResamplers.NearestNeighbor
        }));

        // Count colors
        var colorCounts = new Dictionary<uint, int>();

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    if (pixel.A < 128) continue;

                    var quantized = QuantizeColor(pixel);
                    var key = (uint)(quantized.R << 16 | quantized.G << 8 | quantized.B);

                    colorCounts.TryGetValue(key, out var count);
                    colorCounts[key] = count + 1;
                }
            }
        });

        // Get top colors
        return colorCounts
            .OrderByDescending(x => x.Value)
            .Take(paletteSize)
            .Select(x =>
            {
                var r = (byte)(x.Key >> 16);
                var g = (byte)(x.Key >> 8);
                var b = (byte)x.Key;
                return $"#{r:X2}{g:X2}{b:X2}";
            })
            .ToList();
    }
}
