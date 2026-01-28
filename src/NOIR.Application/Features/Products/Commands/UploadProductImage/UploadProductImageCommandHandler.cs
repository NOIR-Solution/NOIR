namespace NOIR.Application.Features.Products.Commands.UploadProductImage;

/// <summary>
/// Handler for uploading product images.
/// Processes the image (resizes, optimizes) and stores it.
/// </summary>
public class UploadProductImageCommandHandler : IScopedService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly IImageProcessor _imageProcessor;
    private readonly ILogger<UploadProductImageCommandHandler> _logger;

    private const string ProductImagesFolder = "products";

    public UploadProductImageCommandHandler(
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork,
        IFileStorage fileStorage,
        IImageProcessor imageProcessor,
        ILogger<UploadProductImageCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _imageProcessor = imageProcessor;
        _logger = logger;
    }

    public async Task<Result<ProductImageUploadResultDto>> Handle(
        UploadProductImageCommand command,
        CancellationToken cancellationToken)
    {
        // Get product with tracking and images loaded
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductImageUploadResultDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", "NOIR-PRODUCT-026"));
        }

        // Validate it's a valid image
        if (!await _imageProcessor.IsValidImageAsync(command.FileStream, command.FileName))
        {
            return Result.Failure<ProductImageUploadResultDto>(
                Error.Validation(
                    "image",
                    "Invalid image format or corrupted file.",
                    "NOIR-PRODUCT-030"));
        }

        // Reset stream for processing
        if (command.FileStream.CanSeek)
        {
            command.FileStream.Position = 0;
        }

        // Configure processing options for product images
        var storageFolder = $"{ProductImagesFolder}/{command.ProductId}";
        var options = new ImageProcessingOptions
        {
            // Product images need all variants for responsive display
            Variants = [ImageVariant.Thumb, ImageVariant.Medium, ImageVariant.Large],
            // Generate all modern formats for optimal delivery
            Formats = [OutputFormat.WebP, OutputFormat.Jpeg],
            // Generate placeholder for smooth loading
            GenerateThumbHash = true,
            ExtractDominantColor = true,
            PreserveOriginal = true,
            StorageFolder = storageFolder
        };

        // Process the image
        var result = await _imageProcessor.ProcessAsync(
            command.FileStream,
            command.FileName,
            options,
            cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Product image processing failed for product {ProductId}: {Error}",
                command.ProductId, result.ErrorMessage);

            return Result.Failure<ProductImageUploadResultDto>(
                Error.Failure("NOIR-PRODUCT-031", result.ErrorMessage ?? "Failed to process image"));
        }

        // Get the primary URL (Large WebP variant preferred)
        var primaryVariant = result.Variants
            .Where(v => v.Variant == ImageVariant.Large && v.Format == OutputFormat.WebP)
            .FirstOrDefault()
            ?? result.Variants.FirstOrDefault();

        if (primaryVariant is null)
        {
            _logger.LogError("No image variant generated for product {ProductId}", command.ProductId);
            return Result.Failure<ProductImageUploadResultDto>(
                Error.Failure("NOIR-PRODUCT-032", "Failed to generate image variants"));
        }

        var primaryUrl = primaryVariant.Url ?? primaryVariant.Path;

        // Get variant URLs
        var thumbUrl = result.Variants
            .FirstOrDefault(v => v.Variant == ImageVariant.Thumb && v.Format == OutputFormat.WebP)?.Url;
        var mediumUrl = result.Variants
            .FirstOrDefault(v => v.Variant == ImageVariant.Medium && v.Format == OutputFormat.WebP)?.Url;
        var largeUrl = result.Variants
            .FirstOrDefault(v => v.Variant == ImageVariant.Large && v.Format == OutputFormat.WebP)?.Url;

        // Add image to product
        var image = product.AddImage(primaryUrl, command.AltText, command.IsPrimary);

        // Set sort order (default to end of list)
        var sortOrder = product.Images.Count > 1
            ? product.Images.Where(i => i.Id != image.Id).Max(i => i.SortOrder) + 1
            : 0;
        image.SetSortOrder(sortOrder);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Rollback: delete uploaded files
            _logger.LogError(ex, "Failed to save product image. Rolling back uploaded files.");
            foreach (var variant in result.Variants)
            {
                await _fileStorage.DeleteAsync(variant.Path, cancellationToken);
            }
            throw;
        }

        _logger.LogInformation(
            "Product image uploaded for product {ProductId}: {Slug} ({VariantCount} variants in {Ms}ms)",
            command.ProductId, result.Slug, result.Variants.Count, result.ProcessingTimeMs);

        return Result.Success(new ProductImageUploadResultDto(
            image.Id,
            primaryUrl,
            command.AltText,
            sortOrder,
            command.IsPrimary,
            thumbUrl,
            mediumUrl,
            largeUrl,
            result.Metadata?.Width,
            result.Metadata?.Height,
            result.ThumbHash,
            result.DominantColor,
            "Image uploaded successfully"));
    }
}
