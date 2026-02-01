namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for image processing operations including resizing, format conversion,
/// placeholder generation, and metadata extraction.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Process an uploaded image: validate, resize, optimize, generate multi-format variants,
    /// extract ThumbHash placeholder and dominant color, create SEO-friendly filenames.
    /// </summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <param name="fileName">Original filename for SEO slug generation.</param>
    /// <param name="options">Processing options (variants, formats, quality).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Processing result with all generated variants and metadata.</returns>
    Task<ImageProcessingResult> ProcessAsync(
        Stream inputStream,
        string fileName,
        ImageProcessingOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a specific variant of an existing image.
    /// </summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <param name="variant">The variant to generate (thumb, sm, md, lg, xl, original).</param>
    /// <param name="format">The output format (AVIF, WebP, JPEG).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Stream containing the processed image.</returns>
    Task<Stream> GenerateVariantAsync(
        Stream inputStream,
        ImageVariant variant,
        OutputFormat format,
        CancellationToken ct = default);

    /// <summary>
    /// Extract metadata from an image without processing.
    /// </summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Image metadata including dimensions, format, and color info.</returns>
    Task<ImageMetadata> GetMetadataAsync(
        Stream inputStream,
        CancellationToken ct = default);

    /// <summary>
    /// Generate a ThumbHash placeholder for an image.
    /// </summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Base64-encoded ThumbHash string.</returns>
    Task<string> GenerateThumbHashAsync(
        Stream inputStream,
        CancellationToken ct = default);

    /// <summary>
    /// Extract the dominant color from an image.
    /// </summary>
    /// <param name="inputStream">The input image stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Hex color string (e.g., "#FF5733").</returns>
    Task<string> ExtractDominantColorAsync(
        Stream inputStream,
        CancellationToken ct = default);

    /// <summary>
    /// Generate responsive srcset markup for an image.
    /// </summary>
    /// <param name="baseUrl">Base URL for the image.</param>
    /// <param name="variants">Available variants with their URLs.</param>
    /// <returns>Srcset string for use in img tag.</returns>
    string GenerateSrcset(string baseUrl, IEnumerable<ImageVariantInfo> variants);

    /// <summary>
    /// Validate if a file is a supported image format.
    /// </summary>
    /// <param name="inputStream">The input stream to validate.</param>
    /// <param name="fileName">The filename to check extension.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if valid image format.</returns>
    Task<bool> IsValidImageAsync(Stream inputStream, string fileName, CancellationToken ct = default);
}

/// <summary>
/// Image processing options.
/// </summary>
public sealed record ImageProcessingOptions
{
    /// <summary>
    /// Variants to generate. If null, generates all standard variants.
    /// </summary>
    public IReadOnlyList<ImageVariant>? Variants { get; init; }

    /// <summary>
    /// Output formats to generate. If null, generates AVIF + WebP + JPEG.
    /// </summary>
    public IReadOnlyList<OutputFormat>? Formats { get; init; }

    /// <summary>
    /// Quality for lossy formats (1-100). Default: 80.
    /// </summary>
    public int Quality { get; init; } = 80;

    /// <summary>
    /// Generate ThumbHash placeholder. Default: true.
    /// </summary>
    public bool GenerateThumbHash { get; init; } = true;

    /// <summary>
    /// Extract dominant color. Default: true.
    /// </summary>
    public bool ExtractDominantColor { get; init; } = true;

    /// <summary>
    /// Preserve original image. Default: true.
    /// </summary>
    public bool PreserveOriginal { get; init; } = true;

    /// <summary>
    /// Maximum dimension for original preservation (0 = no limit).
    /// </summary>
    public int MaxOriginalDimension { get; init; } = 2560;

    /// <summary>
    /// Storage folder for processed images.
    /// </summary>
    public string? StorageFolder { get; init; }
}

/// <summary>
/// Result of image processing operation.
/// </summary>
public sealed record ImageProcessingResult
{
    /// <summary>
    /// Whether processing was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// SEO-friendly slug generated from filename.
    /// </summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>
    /// Generated variants with their storage paths.
    /// </summary>
    public IReadOnlyList<ImageVariantInfo> Variants { get; init; } = [];

    /// <summary>
    /// Image metadata.
    /// </summary>
    public ImageMetadata? Metadata { get; init; }

    /// <summary>
    /// Base64-encoded ThumbHash for placeholder.
    /// </summary>
    public string? ThumbHash { get; init; }

    /// <summary>
    /// Dominant color as hex string.
    /// </summary>
    public string? DominantColor { get; init; }

    /// <summary>
    /// Total processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; init; }

    /// <summary>
    /// Create a failed result.
    /// </summary>
    public static ImageProcessingResult Failure(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };
}

/// <summary>
/// Information about a generated image variant.
/// </summary>
public sealed record ImageVariantInfo
{
    /// <summary>
    /// The variant type.
    /// </summary>
    public ImageVariant Variant { get; init; }

    /// <summary>
    /// The output format.
    /// </summary>
    public OutputFormat Format { get; init; }

    /// <summary>
    /// Storage path for this variant.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Public URL if available.
    /// </summary>
    public string? Url { get; init; }

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

/// <summary>
/// Image metadata extracted from an image.
/// </summary>
public sealed record ImageMetadata
{
    /// <summary>
    /// Original width in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Original height in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Detected format.
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
    /// Aspect ratio (width / height).
    /// </summary>
    public double AspectRatio => Height > 0 ? (double)Width / Height : 0;

    /// <summary>
    /// Whether the image has transparency (alpha channel).
    /// </summary>
    public bool HasTransparency { get; init; }

    /// <summary>
    /// Whether the image is animated (GIF, APNG, WebP).
    /// </summary>
    public bool IsAnimated { get; init; }

    /// <summary>
    /// Color depth in bits per pixel.
    /// </summary>
    public int ColorDepth { get; init; }

    /// <summary>
    /// EXIF orientation if available (1-8).
    /// </summary>
    public int? Orientation { get; init; }
}

/// <summary>
/// Image variant sizes.
/// SEO-optimized: 3 variants for optimal performance and storage.
/// </summary>
public enum ImageVariant
{
    /// <summary>Thumbnail: 150px - avatars, list thumbnails</summary>
    Thumb = 150,

    /// <summary>Medium: 640px - cards, mobile, content images</summary>
    Medium = 640,

    /// <summary>Large: 1280px - hero images, desktop full display</summary>
    Large = 1280
}

/// <summary>
/// Output image formats.
/// </summary>
public enum OutputFormat
{
    /// <summary>AVIF - Best compression, modern browsers</summary>
    Avif,

    /// <summary>WebP - Good compression, wide support</summary>
    WebP,

    /// <summary>JPEG - Universal fallback</summary>
    Jpeg,

    /// <summary>PNG - Lossless, preserves transparency</summary>
    Png
}
