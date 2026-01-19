namespace NOIR.Application.Features.Media.Dtos;

/// <summary>
/// DTO for MediaFile entity with all metadata needed for rendering.
/// </summary>
public sealed record MediaFileDto
{
    /// <summary>
    /// Unique identifier of the media file.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Short unique identifier (8 chars) for quick lookups.
    /// Can be extracted from slug (after underscore): "hero-banner_a1b2c3d4" → "a1b2c3d4"
    /// </summary>
    public string ShortId { get; init; } = string.Empty;

    /// <summary>
    /// SEO-friendly slug identifier with unique suffix (e.g., "hero-banner_a1b2c3d4").
    /// </summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>
    /// Original filename as uploaded.
    /// </summary>
    public string OriginalFileName { get; init; } = string.Empty;

    /// <summary>
    /// Storage folder (blog, avatars, content).
    /// </summary>
    public string Folder { get; init; } = string.Empty;

    /// <summary>
    /// Default URL for simple image display.
    /// </summary>
    public string DefaultUrl { get; init; } = string.Empty;

    /// <summary>
    /// Base64-encoded ThumbHash for blur placeholder.
    /// </summary>
    public string? ThumbHash { get; init; }

    /// <summary>
    /// Dominant color as hex string for placeholder background.
    /// </summary>
    public string? DominantColor { get; init; }

    /// <summary>
    /// Original image width in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Original image height in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Aspect ratio (width / height).
    /// </summary>
    public double AspectRatio { get; init; }

    /// <summary>
    /// Image format (jpeg, png, webp).
    /// </summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>
    /// MIME type.
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Original file size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Whether image has transparency.
    /// </summary>
    public bool HasTransparency { get; init; }

    /// <summary>
    /// Alt text for accessibility.
    /// </summary>
    public string? AltText { get; init; }

    /// <summary>
    /// Available image variants with their URLs and dimensions.
    /// </summary>
    public IReadOnlyList<MediaVariantDto> Variants { get; init; } = [];

    /// <summary>
    /// Pre-generated srcset strings by format.
    /// Key: format (avif, webp, jpeg), Value: srcset string.
    /// </summary>
    public IReadOnlyDictionary<string, string> Srcsets { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// When the file was uploaded.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
