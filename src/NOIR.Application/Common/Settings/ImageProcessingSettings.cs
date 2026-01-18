namespace NOIR.Application.Common.Settings;

/// <summary>
/// Image processing configuration settings.
/// Controls image variants, quality, formats, and size limits.
/// </summary>
public class ImageProcessingSettings
{
    public const string SectionName = "ImageProcessing";

    /// <summary>
    /// Maximum upload size in bytes. Default: 10MB.
    /// </summary>
    [Range(1024, 104857600, ErrorMessage = "MaxUploadSizeBytes must be between 1KB and 100MB")]
    public long MaxUploadSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Default quality for JPEG/WebP/AVIF (1-100). Default: 80.
    /// </summary>
    [Range(1, 100, ErrorMessage = "DefaultQuality must be between 1 and 100")]
    public int DefaultQuality { get; set; } = 80;

    /// <summary>
    /// AVIF quality (1-100). Lower than JPEG for similar visual quality. Default: 65.
    /// </summary>
    [Range(1, 100, ErrorMessage = "AvifQuality must be between 1 and 100")]
    public int AvifQuality { get; set; } = 65;

    /// <summary>
    /// WebP quality (1-100). Default: 80.
    /// </summary>
    [Range(1, 100, ErrorMessage = "WebPQuality must be between 1 and 100")]
    public int WebPQuality { get; set; } = 80;

    /// <summary>
    /// Maximum dimension for thumbnail variant. Default: 150px.
    /// </summary>
    [Range(50, 500, ErrorMessage = "ThumbSize must be between 50 and 500")]
    public int ThumbSize { get; set; } = 150;

    /// <summary>
    /// Maximum dimension for small variant. Default: 320px.
    /// </summary>
    [Range(100, 640, ErrorMessage = "SmallSize must be between 100 and 640")]
    public int SmallSize { get; set; } = 320;

    /// <summary>
    /// Maximum dimension for medium variant. Default: 640px.
    /// </summary>
    [Range(320, 1280, ErrorMessage = "MediumSize must be between 320 and 1280")]
    public int MediumSize { get; set; } = 640;

    /// <summary>
    /// Maximum dimension for large variant. Default: 1280px.
    /// </summary>
    [Range(640, 2560, ErrorMessage = "LargeSize must be between 640 and 2560")]
    public int LargeSize { get; set; } = 1280;

    /// <summary>
    /// Maximum dimension for extra large variant. Default: 1920px.
    /// </summary>
    [Range(1280, 3840, ErrorMessage = "ExtraLargeSize must be between 1280 and 3840")]
    public int ExtraLargeSize { get; set; } = 1920;

    /// <summary>
    /// Maximum dimension for original preservation. Default: 2560px.
    /// </summary>
    [Range(1920, 7680, ErrorMessage = "OriginalMaxSize must be between 1920 and 7680")]
    public int OriginalMaxSize { get; set; } = 2560;

    /// <summary>
    /// Generate AVIF format. Default: false (slow encoding).
    /// </summary>
    public bool GenerateAvif { get; set; } = false;

    /// <summary>
    /// Generate WebP format. Default: true.
    /// </summary>
    public bool GenerateWebP { get; set; } = true;

    /// <summary>
    /// Generate JPEG format as fallback. Default: true.
    /// </summary>
    public bool GenerateJpeg { get; set; } = true;

    /// <summary>
    /// Generate ThumbHash placeholder. Default: true.
    /// </summary>
    public bool GenerateThumbHash { get; set; } = true;

    /// <summary>
    /// Extract dominant color. Default: false (rarely used).
    /// </summary>
    public bool ExtractDominantColor { get; set; } = false;

    /// <summary>
    /// Preserve original image (resized to OriginalMaxSize if larger). Default: true.
    /// </summary>
    public bool PreserveOriginal { get; set; } = true;

    /// <summary>
    /// Strip EXIF metadata for privacy. Default: true.
    /// </summary>
    public bool StripExifMetadata { get; set; } = true;

    /// <summary>
    /// Auto-rotate based on EXIF orientation. Default: true.
    /// </summary>
    public bool AutoRotate { get; set; } = true;

    /// <summary>
    /// Allowed MIME types for upload.
    /// </summary>
    public List<string> AllowedMimeTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/avif",
        "image/heic",
        "image/heif"
    ];

    /// <summary>
    /// Allowed file extensions for upload.
    /// </summary>
    public List<string> AllowedExtensions { get; set; } =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp",
        ".avif",
        ".heic",
        ".heif"
    ];

    /// <summary>
    /// Storage folder for processed images. Default: "images".
    /// </summary>
    public string StorageFolder { get; set; } = "images";

    /// <summary>
    /// CDN base URL for images (optional).
    /// </summary>
    public string? CdnBaseUrl { get; set; }

    /// <summary>
    /// Get the size for a variant.
    /// </summary>
    public int GetVariantSize(ImageVariant variant) => variant switch
    {
        ImageVariant.Thumb => ThumbSize,
        ImageVariant.Small => SmallSize,
        ImageVariant.Medium => MediumSize,
        ImageVariant.Large => LargeSize,
        ImageVariant.ExtraLarge => ExtraLargeSize,
        ImageVariant.Original => OriginalMaxSize,
        _ => MediumSize
    };

    /// <summary>
    /// Get the quality for a format.
    /// </summary>
    public int GetFormatQuality(OutputFormat format) => format switch
    {
        OutputFormat.Avif => AvifQuality,
        OutputFormat.WebP => WebPQuality,
        OutputFormat.Jpeg => DefaultQuality,
        OutputFormat.Png => 100, // PNG is lossless
        _ => DefaultQuality
    };

    /// <summary>
    /// Get enabled output formats.
    /// </summary>
    public IEnumerable<OutputFormat> GetEnabledFormats()
    {
        if (GenerateAvif) yield return OutputFormat.Avif;
        if (GenerateWebP) yield return OutputFormat.WebP;
        if (GenerateJpeg) yield return OutputFormat.Jpeg;
    }
}
