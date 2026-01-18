namespace NOIR.Infrastructure.Media;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ThumbHashes;

/// <summary>
/// Generates ThumbHash placeholders for images.
/// ThumbHash is better than BlurHash: 28 bytes vs 34, preserves aspect ratio, supports transparency.
/// </summary>
public static class ThumbHashGenerator
{
    /// <summary>
    /// Generate a ThumbHash for an image.
    /// </summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Base64-encoded ThumbHash string.</returns>
    public static async Task<string> GenerateAsync(Stream inputStream, CancellationToken ct = default)
    {
        // Reset stream position
        if (inputStream.CanSeek)
            inputStream.Position = 0;

        using var image = await Image.LoadAsync<Rgba32>(inputStream, ct);

        // ThumbHash works best with small images (100x100 max)
        var maxSize = 100;
        var (width, height) = CalculateSize(image.Width, image.Height, maxSize);

        image.Mutate(x => x.Resize(width, height));

        // Extract RGBA pixels
        var rgba = new byte[width * height * 4];
        image.ProcessPixelRows(accessor =>
        {
            var index = 0;
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var pixel = row[x];
                    rgba[index++] = pixel.R;
                    rgba[index++] = pixel.G;
                    rgba[index++] = pixel.B;
                    rgba[index++] = pixel.A;
                }
            }
        });

        // Generate ThumbHash using the library
        var thumbHash = ThumbHash.FromImage(width, height, rgba);

        // Hash property returns ReadOnlyMemory<byte>, convert to array
        return Convert.ToBase64String(thumbHash.Hash.ToArray());
    }

    /// <summary>
    /// Decode a ThumbHash to data URL.
    /// </summary>
    /// <param name="thumbHashBase64">Base64-encoded ThumbHash.</param>
    /// <returns>Data URL for the placeholder image.</returns>
    public static string DecodeToDataUrl(string thumbHashBase64)
    {
        var hashBytes = Convert.FromBase64String(thumbHashBase64);
        var thumbHash = new ThumbHash(hashBytes);
        var (w, h, rgbaData) = thumbHash.ToImage();

        // Create a small PNG from the RGBA data
        using var image = new Image<Rgba32>(w, h);
        image.ProcessPixelRows(accessor =>
        {
            var index = 0;
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgba32(
                        rgbaData[index],
                        rgbaData[index + 1],
                        rgbaData[index + 2],
                        rgbaData[index + 3]);
                    index += 4;
                }
            }
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());
        return $"data:image/png;base64,{base64}";
    }

    /// <summary>
    /// Get average color from a ThumbHash by decoding and computing average from RGBA data.
    /// </summary>
    /// <param name="thumbHashBase64">Base64-encoded ThumbHash.</param>
    /// <returns>Hex color string.</returns>
    public static string GetAverageColor(string thumbHashBase64)
    {
        var hashBytes = Convert.FromBase64String(thumbHashBase64);
        var thumbHash = new ThumbHash(hashBytes);
        var (_, _, rgbaData) = thumbHash.ToImage();

        // Compute average color from RGBA data
        long totalR = 0, totalG = 0, totalB = 0;
        var pixelCount = rgbaData.Length / 4;

        for (var i = 0; i < rgbaData.Length; i += 4)
        {
            totalR += rgbaData[i];
            totalG += rgbaData[i + 1];
            totalB += rgbaData[i + 2];
        }

        var red = (byte)(totalR / pixelCount);
        var green = (byte)(totalG / pixelCount);
        var blue = (byte)(totalB / pixelCount);

        return $"#{red:X2}{green:X2}{blue:X2}";
    }

    /// <summary>
    /// Get approximate dimensions from a ThumbHash by decoding it.
    /// </summary>
    /// <param name="thumbHashBase64">Base64-encoded ThumbHash.</param>
    /// <returns>Tuple of (width, height, aspectRatio).</returns>
    public static (int width, int height, float aspectRatio) GetApproximateDimensions(string thumbHashBase64)
    {
        var hashBytes = Convert.FromBase64String(thumbHashBase64);
        var thumbHash = new ThumbHash(hashBytes);
        var (w, h, _) = thumbHash.ToImage();

        var ratio = (float)w / h;

        // Scale up from thumbnail size to approximate dimensions
        const int baseSize = 100;
        int width, height;

        if (ratio >= 1)
        {
            width = baseSize;
            height = (int)(baseSize / ratio);
        }
        else
        {
            height = baseSize;
            width = (int)(baseSize * ratio);
        }

        return (width, height, ratio);
    }

    /// <summary>
    /// Calculate size preserving aspect ratio.
    /// </summary>
    private static (int width, int height) CalculateSize(int originalWidth, int originalHeight, int maxSize)
    {
        if (originalWidth <= maxSize && originalHeight <= maxSize)
            return (originalWidth, originalHeight);

        var ratio = (double)originalWidth / originalHeight;

        if (originalWidth > originalHeight)
        {
            return (maxSize, (int)(maxSize / ratio));
        }
        else
        {
            return ((int)(maxSize * ratio), maxSize);
        }
    }
}
