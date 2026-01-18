namespace NOIR.Application.Features.Media.Dtos;

/// <summary>
/// Result of a media upload operation.
/// Contains all generated variants, placeholders, and metadata.
/// </summary>
public sealed record MediaUploadResultDto
{
    /// <summary>
    /// Whether the upload and processing was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if upload failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// SEO-friendly slug identifier for the image.
    /// </summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>
    /// Base64-encoded ThumbHash for placeholder display.
    /// </summary>
    public string? ThumbHash { get; init; }

    /// <summary>
    /// Dominant color as hex string (e.g., "#FF5733").
    /// </summary>
    public string? DominantColor { get; init; }

    /// <summary>
    /// Original image metadata.
    /// </summary>
    public MediaMetadataDto? Metadata { get; init; }

    /// <summary>
    /// All generated image variants.
    /// </summary>
    public IReadOnlyList<MediaVariantDto> Variants { get; init; } = [];

    /// <summary>
    /// Pre-generated srcset strings for each format.
    /// Key: format (avif, webp, jpeg), Value: srcset string
    /// </summary>
    public IReadOnlyDictionary<string, string> Srcsets { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Default URL to use (typically the large WebP variant).
    /// This is for backward compatibility with simple image fields.
    /// </summary>
    public string? DefaultUrl { get; init; }

    /// <summary>
    /// URL for TinyMCE compatibility (same as DefaultUrl).
    /// TinyMCE expects "location" in the response.
    /// </summary>
    public string? Location => DefaultUrl;

    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>
    /// Create a successful result.
    /// </summary>
    public static MediaUploadResultDto FromProcessingResult(
        ImageProcessingResult result,
        string defaultUrl)
    {
        // Group variants by format for srcset generation
        var srcsets = new Dictionary<string, string>();
        var variantsByFormat = result.Variants.GroupBy(v => v.Format);

        foreach (var formatGroup in variantsByFormat)
        {
            var formatName = formatGroup.Key.ToString().ToLowerInvariant();
            var srcsetParts = formatGroup
                .OrderBy(v => v.Width)
                .Select(v => $"{v.Url} {v.Width}w");
            srcsets[formatName] = string.Join(", ", srcsetParts);
        }

        return new MediaUploadResultDto
        {
            Success = true,
            Slug = result.Slug,
            ThumbHash = result.ThumbHash,
            DominantColor = result.DominantColor,
            Metadata = result.Metadata != null
                ? new MediaMetadataDto
                {
                    Width = result.Metadata.Width,
                    Height = result.Metadata.Height,
                    Format = result.Metadata.Format,
                    MimeType = result.Metadata.MimeType,
                    SizeBytes = result.Metadata.SizeBytes,
                    AspectRatio = result.Metadata.AspectRatio,
                    HasTransparency = result.Metadata.HasTransparency
                }
                : null,
            Variants = result.Variants.Select(v => new MediaVariantDto
            {
                Variant = v.Variant.ToString().ToLowerInvariant(),
                Format = v.Format.ToString().ToLowerInvariant(),
                Url = v.Url ?? v.Path,
                Width = v.Width,
                Height = v.Height,
                SizeBytes = v.SizeBytes
            }).ToList(),
            Srcsets = srcsets,
            DefaultUrl = defaultUrl,
            ProcessingTimeMs = result.ProcessingTimeMs
        };
    }

    /// <summary>
    /// Create a failure result.
    /// </summary>
    public static MediaUploadResultDto Failure(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

/// <summary>
/// Metadata about the original uploaded image.
/// </summary>
public sealed record MediaMetadataDto
{
    public int Width { get; init; }
    public int Height { get; init; }
    public string Format { get; init; } = string.Empty;
    public string MimeType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public double AspectRatio { get; init; }
    public bool HasTransparency { get; init; }
}

/// <summary>
/// Information about a single image variant.
/// </summary>
public sealed record MediaVariantDto
{
    /// <summary>
    /// Variant name (thumb, small, medium, large, extralarge, original).
    /// </summary>
    public string Variant { get; init; } = string.Empty;

    /// <summary>
    /// Format (avif, webp, jpeg, png).
    /// </summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>
    /// Public URL to access this variant.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Width in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Height in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }
}
