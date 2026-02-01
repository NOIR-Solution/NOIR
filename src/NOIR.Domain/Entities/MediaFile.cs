namespace NOIR.Domain.Entities;

/// <summary>
/// Represents an uploaded media file with all processing metadata.
/// Stores file variants, placeholders, and metadata for responsive image rendering.
/// </summary>
public class MediaFile : TenantAggregateRoot<Guid>
{
    /// <summary>
    /// Short unique identifier (8 chars) for quick lookups.
    /// Example: "a1b2c3d4"
    /// </summary>
    public string ShortId { get; private set; } = default!;

    /// <summary>
    /// SEO-friendly slug identifier with unique suffix (e.g., "hero-banner_a1b2c3d4").
    /// The ShortId is appended after underscore for easy extraction.
    /// </summary>
    public string Slug { get; private set; } = default!;

    /// <summary>
    /// Original filename as uploaded by user.
    /// </summary>
    public string OriginalFileName { get; private set; } = default!;

    /// <summary>
    /// Storage folder (e.g., "blog", "avatars", "content").
    /// </summary>
    public string Folder { get; private set; } = default!;

    /// <summary>
    /// Default URL for backward compatibility (typically large WebP).
    /// </summary>
    public string DefaultUrl { get; private set; } = default!;

    /// <summary>
    /// Base64-encoded ThumbHash for blur placeholder during loading.
    /// </summary>
    public string? ThumbHash { get; private set; }

    /// <summary>
    /// Dominant color as hex string (e.g., "#FF5733") for placeholder.
    /// </summary>
    public string? DominantColor { get; private set; }

    #region Original Image Metadata

    /// <summary>
    /// Original image width in pixels.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Original image height in pixels.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Original image format (e.g., "jpeg", "png", "webp").
    /// </summary>
    public string Format { get; private set; } = default!;

    /// <summary>
    /// MIME type (e.g., "image/jpeg").
    /// </summary>
    public string MimeType { get; private set; } = default!;

    /// <summary>
    /// Original file size in bytes.
    /// </summary>
    public long SizeBytes { get; private set; }

    /// <summary>
    /// Whether the image has transparency (alpha channel).
    /// </summary>
    public bool HasTransparency { get; private set; }

    /// <summary>
    /// Aspect ratio (width / height).
    /// </summary>
    public double AspectRatio => Height > 0 ? (double)Width / Height : 0;

    #endregion

    /// <summary>
    /// JSON-serialized list of image variants with their URLs and dimensions.
    /// </summary>
    public string VariantsJson { get; private set; } = "[]";

    /// <summary>
    /// JSON-serialized srcset dictionary (format -> srcset string).
    /// </summary>
    public string SrcsetsJson { get; private set; } = "{}";

    /// <summary>
    /// Alt text for accessibility (optional, can be set by referencing entity).
    /// </summary>
    public string? AltText { get; private set; }

    /// <summary>
    /// User ID who uploaded this file.
    /// </summary>
    public string UploadedBy { get; private set; } = default!;

    // Private constructor for EF Core
    private MediaFile() : base() { }

    /// <summary>
    /// Creates a new MediaFile from processing result.
    /// </summary>
    public static MediaFile Create(
        string shortId,
        string slug,
        string originalFileName,
        string folder,
        string defaultUrl,
        string? thumbHash,
        string? dominantColor,
        int width,
        int height,
        string format,
        string mimeType,
        long sizeBytes,
        bool hasTransparency,
        string variantsJson,
        string srcsetsJson,
        string uploadedBy,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shortId);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadedBy);

        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        if (sizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "SizeBytes must be positive.");

        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShortId = shortId,
            Slug = slug,
            OriginalFileName = originalFileName,
            Folder = folder,
            DefaultUrl = defaultUrl,
            ThumbHash = thumbHash,
            DominantColor = dominantColor,
            Width = width,
            Height = height,
            Format = format,
            MimeType = mimeType,
            SizeBytes = sizeBytes,
            HasTransparency = hasTransparency,
            VariantsJson = variantsJson,
            SrcsetsJson = srcsetsJson,
            UploadedBy = uploadedBy,
            TenantId = tenantId
        };

        mediaFile.AddDomainEvent(new Events.Media.MediaFileUploadedEvent(
            mediaFile.Id,
            originalFileName,
            mimeType,
            folder,
            sizeBytes));

        return mediaFile;
    }

    /// <summary>
    /// Updates alt text for the media file.
    /// </summary>
    public void UpdateAltText(string? altText)
    {
        AltText = altText;

        AddDomainEvent(new Events.Media.MediaFileAltTextUpdatedEvent(Id, altText));
    }

    /// <summary>
    /// Marks the media file as deleted (soft delete).
    /// </summary>
    public void MarkAsDeleted()
    {
        AddDomainEvent(new Events.Media.MediaFileDeletedEvent(Id, OriginalFileName));
    }
}
