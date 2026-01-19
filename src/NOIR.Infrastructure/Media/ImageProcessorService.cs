namespace NOIR.Infrastructure.Media;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using NeoSolve.ImageSharp.AVIF;

// Alias to avoid ambiguity with SixLabors.ImageSharp.Web.ImageMetadata
using NoirImageMetadata = NOIR.Application.Common.Interfaces.ImageMetadata;

/// <summary>
/// Processes images by generating optimized variants in multiple formats.
/// Supports AVIF, WebP, and JPEG with configurable quality settings.
/// </summary>
public class ImageProcessorService : IImageProcessor, IScopedService
{
    private readonly ImageProcessingSettings _settings;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<ImageProcessorService> _logger;

    public ImageProcessorService(
        IOptions<ImageProcessingSettings> settings,
        IFileStorage fileStorage,
        ILogger<ImageProcessorService> logger)
    {
        _settings = settings.Value;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ImageProcessingResult> ProcessAsync(
        Stream inputStream,
        string fileName,
        ImageProcessingOptions options,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            // Reset stream position
            if (inputStream.CanSeek)
                inputStream.Position = 0;

            // Load and validate image
            using var image = await Image.LoadAsync<Rgba32>(inputStream, ct);

            // Auto-rotate based on EXIF if enabled
            if (_settings.AutoRotate)
            {
                image.Mutate(x => x.AutoOrient());
            }

            // Get original metadata
            var originalWidth = image.Width;
            var originalHeight = image.Height;

            // Generate slug for URL-friendly naming
            var slug = SlugGenerator.GenerateUnique(fileName);

            // Generate ThumbHash for placeholder
            string? thumbHash = null;
            if (options.GenerateThumbHash)
            {
                if (inputStream.CanSeek)
                    inputStream.Position = 0;
                thumbHash = await ThumbHashGenerator.GenerateAsync(inputStream, ct);
            }

            // Extract dominant color
            string? dominantColor = null;
            if (options.ExtractDominantColor)
            {
                if (inputStream.CanSeek)
                    inputStream.Position = 0;
                dominantColor = await ColorAnalyzer.ExtractDominantColorAsync(inputStream, ct);
            }

            // Determine which variants to generate
            var variantsToGenerate = options.Variants?.ToList() ?? GetDefaultVariants(originalWidth, originalHeight);

            // Determine which formats to generate
            var formatsToGenerate = options.Formats?.ToList() ?? _settings.GetEnabledFormats().ToList();

            // Generate all variants - process formats in parallel for speed
            var variants = new List<ImageVariantInfo>();
            var variantTasks = new List<Task<ImageVariantInfo?>>();

            foreach (var variant in variantsToGenerate)
            {
                var variantSize = _settings.GetVariantSize(variant);

                // Skip if variant is larger than original (no upscaling)
                if (variantSize > Math.Max(originalWidth, originalHeight))
                    continue;

                // Calculate dimensions maintaining aspect ratio
                var (width, height) = CalculateSize(originalWidth, originalHeight, variantSize);

                // Generate each format in parallel - each task gets its own image clone
                foreach (var format in formatsToGenerate)
                {
                    var capturedVariant = variant;
                    var capturedFormat = format;
                    var capturedWidth = width;
                    var capturedHeight = height;

                    variantTasks.Add(Task.Run(async () =>
                    {
                        // Clone image for this task (thread-safe)
                        using var resizedImage = image.Clone(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(capturedWidth, capturedHeight),
                            Mode = ResizeMode.Max,
                            Sampler = KnownResamplers.Lanczos3
                        }));

                        return await SaveVariantAsync(
                            resizedImage,
                            slug,
                            capturedVariant,
                            capturedFormat,
                            capturedWidth,
                            capturedHeight,
                            options.StorageFolder ?? _settings.StorageFolder,
                            ct);
                    }, ct));
                }
            }

            // Wait for all variants to complete
            var results = await Task.WhenAll(variantTasks);
            variants.AddRange(results.Where(r => r != null)!);

            sw.Stop();

            // Create metadata
            var metadata = new NoirImageMetadata
            {
                Width = originalWidth,
                Height = originalHeight,
                Format = DetectFormat(fileName),
                MimeType = GetMimeTypeFromFileName(fileName),
                HasTransparency = false // Would need more analysis to detect
            };

            return new ImageProcessingResult
            {
                Success = true,
                Slug = slug,
                Variants = variants,
                Metadata = metadata,
                ThumbHash = thumbHash,
                DominantColor = dominantColor,
                ProcessingTimeMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process image: {FileName}", fileName);
            sw.Stop();
            return ImageProcessingResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GenerateVariantAsync(
        Stream inputStream,
        ImageVariant variant,
        OutputFormat format,
        CancellationToken ct = default)
    {
        if (inputStream.CanSeek)
            inputStream.Position = 0;

        using var image = await Image.LoadAsync<Rgba32>(inputStream, ct);

        if (_settings.AutoRotate)
        {
            image.Mutate(x => x.AutoOrient());
        }

        var variantSize = _settings.GetVariantSize(variant);
        var (width, height) = CalculateSize(image.Width, image.Height, variantSize);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max,
            Sampler = KnownResamplers.Lanczos3
        }));

        var ms = new MemoryStream();

        switch (format)
        {
            case OutputFormat.Avif:
                // CQLevel: 0-63 where lower is higher quality. Convert from our 0-100 scale.
                var avifCqLevel = Math.Max(0, Math.Min(63, (100 - _settings.AvifQuality) * 63 / 100));
                var avifEncoder = new AVIFEncoder { CQLevel = avifCqLevel };
                await image.SaveAsync(ms, avifEncoder, ct);
                break;

            case OutputFormat.WebP:
                var webpEncoder = new WebpEncoder { Quality = _settings.WebPQuality, FileFormat = WebpFileFormatType.Lossy };
                await image.SaveAsync(ms, webpEncoder, ct);
                break;

            case OutputFormat.Jpeg:
                var jpegEncoder = new JpegEncoder { Quality = _settings.DefaultQuality };
                await image.SaveAsync(ms, jpegEncoder, ct);
                break;

            case OutputFormat.Png:
                var pngEncoder = new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression };
                await image.SaveAsync(ms, pngEncoder, ct);
                break;
        }

        ms.Position = 0;
        return ms;
    }

    /// <inheritdoc />
    public async Task<NoirImageMetadata> GetMetadataAsync(Stream inputStream, CancellationToken ct = default)
    {
        if (inputStream.CanSeek)
            inputStream.Position = 0;

        var info = await Image.IdentifyAsync(inputStream, ct);

        return new NoirImageMetadata
        {
            Width = info.Width,
            Height = info.Height,
            Format = info.Metadata.DecodedImageFormat?.Name ?? "Unknown",
            MimeType = info.Metadata.DecodedImageFormat?.DefaultMimeType ?? "image/unknown",
            ColorDepth = info.PixelType.BitsPerPixel,
            HasTransparency = info.PixelType.AlphaRepresentation != PixelAlphaRepresentation.None
        };
    }

    /// <inheritdoc />
    public async Task<string> GenerateThumbHashAsync(Stream inputStream, CancellationToken ct = default)
    {
        return await ThumbHashGenerator.GenerateAsync(inputStream, ct);
    }

    /// <inheritdoc />
    public async Task<string> ExtractDominantColorAsync(Stream inputStream, CancellationToken ct = default)
    {
        return await ColorAnalyzer.ExtractDominantColorAsync(inputStream, ct);
    }

    /// <inheritdoc />
    public string GenerateSrcset(string baseUrl, IEnumerable<ImageVariantInfo> variants)
    {
        return SrcsetGenerator.GenerateSrcset(variants);
    }

    /// <inheritdoc />
    public async Task<bool> IsValidImageAsync(Stream inputStream, string fileName)
    {
        // Check extension first
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_settings.AllowedExtensions.Contains(extension))
            return false;

        // Then validate the actual content
        try
        {
            if (inputStream.CanSeek)
                inputStream.Position = 0;

            var info = await Image.IdentifyAsync(inputStream);
            return info != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Save a single variant to storage.
    /// </summary>
    private async Task<ImageVariantInfo?> SaveVariantAsync(
        Image<Rgba32> image,
        string slug,
        ImageVariant variant,
        OutputFormat format,
        int width,
        int height,
        string storageFolder,
        CancellationToken ct)
    {
        try
        {
            var extension = GetExtension(format);
            var fileName = $"{slug}-{variant.ToString().ToLowerInvariant()}.{extension}";
            var path = Path.Combine(storageFolder, fileName);

            using var ms = new MemoryStream();

            switch (format)
            {
                case OutputFormat.Avif:
                    // CQLevel: 0-63 where lower is higher quality. Convert from our 0-100 scale.
                    var avifCqLevel2 = Math.Max(0, Math.Min(63, (100 - _settings.AvifQuality) * 63 / 100));
                    var avifEncoder2 = new AVIFEncoder { CQLevel = avifCqLevel2 };
                    await image.SaveAsync(ms, avifEncoder2, ct);
                    break;

                case OutputFormat.WebP:
                    var webpEncoder2 = new WebpEncoder { Quality = _settings.WebPQuality, FileFormat = WebpFileFormatType.Lossy };
                    await image.SaveAsync(ms, webpEncoder2, ct);
                    break;

                case OutputFormat.Jpeg:
                    var jpegEncoder2 = new JpegEncoder { Quality = _settings.DefaultQuality };
                    await image.SaveAsync(ms, jpegEncoder2, ct);
                    break;

                case OutputFormat.Png:
                    var pngEncoder2 = new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression };
                    await image.SaveAsync(ms, pngEncoder2, ct);
                    break;

                default:
                    _logger.LogWarning("Unknown output format: {Format}", format);
                    return null;
            }

            ms.Position = 0;

            // Save to storage
            var storagePath = await _fileStorage.UploadAsync(fileName, ms, storageFolder, ct);
            var url = _fileStorage.GetPublicUrl(storagePath) ?? storagePath;

            // Apply CDN base URL if configured
            if (!string.IsNullOrEmpty(_settings.CdnBaseUrl))
            {
                url = $"{_settings.CdnBaseUrl.TrimEnd('/')}/{fileName}";
            }

            return new ImageVariantInfo
            {
                Variant = variant,
                Format = format,
                Path = path,
                Url = url,
                Width = width,
                Height = height,
                SizeBytes = ms.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save variant {Variant} in format {Format} for {Slug}",
                variant, format, slug);
            return null;
        }
    }

    /// <summary>
    /// Get default variants based on original image size.
    /// SEO-optimized: 3 variants (Thumb 150px, Medium 640px, Large 1280px)
    /// - Thumb: avatars, list thumbnails
    /// - Medium: cards, mobile, content images
    /// - Large: hero images, desktop full display
    /// </summary>
    private List<ImageVariant> GetDefaultVariants(int width, int height)
    {
        var maxDimension = Math.Max(width, height);

        // Always generate Thumb and Medium
        var variants = new List<ImageVariant> { ImageVariant.Thumb, ImageVariant.Medium };

        // Only add Large if image is big enough (no upscaling)
        if (maxDimension >= _settings.LargeSize)
            variants.Add(ImageVariant.Large);

        return variants;
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

    /// <summary>
    /// Get file extension for format.
    /// </summary>
    private static string GetExtension(OutputFormat format) => format switch
    {
        OutputFormat.Avif => "avif",
        OutputFormat.WebP => "webp",
        OutputFormat.Jpeg => "jpg",
        OutputFormat.Png => "png",
        _ => "jpg"
    };

    /// <summary>
    /// Get MIME type for format.
    /// </summary>
    private static string GetMimeType(OutputFormat format) => format switch
    {
        OutputFormat.Avif => "image/avif",
        OutputFormat.WebP => "image/webp",
        OutputFormat.Jpeg => "image/jpeg",
        OutputFormat.Png => "image/png",
        _ => "image/jpeg"
    };

    /// <summary>
    /// Detect format from filename.
    /// </summary>
    private static string DetectFormat(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "JPEG",
            ".png" => "PNG",
            ".gif" => "GIF",
            ".webp" => "WebP",
            ".avif" => "AVIF",
            ".heic" => "HEIC",
            ".heif" => "HEIF",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get MIME type from filename.
    /// </summary>
    private static string GetMimeTypeFromFileName(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".avif" => "image/avif",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            _ => "image/unknown"
        };
    }
}
