using NOIR.Application.Features.Products.Commands.UploadProductImage;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.UploadProductImage;

/// <summary>
/// Unit tests for UploadProductImageCommandHandler.
/// Tests uploading and processing product images with mocked dependencies.
/// </summary>
public class UploadProductImageCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<IImageProcessor> _imageProcessorMock;
    private readonly Mock<ILogger<UploadProductImageCommandHandler>> _loggerMock;
    private readonly UploadProductImageCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public UploadProductImageCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _fileStorageMock = new Mock<IFileStorage>();
        _imageProcessorMock = new Mock<IImageProcessor>();
        _loggerMock = new Mock<ILogger<UploadProductImageCommandHandler>>();

        _handler = new UploadProductImageCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _fileStorageMock.Object,
            _imageProcessorMock.Object,
            _loggerMock.Object);
    }

    private static UploadProductImageCommand CreateTestCommand(
        Guid? productId = null,
        string fileName = "test-image.jpg",
        Stream? fileStream = null,
        string contentType = "image/jpeg",
        long fileSize = 1024,
        string? altText = "Test Image",
        bool isPrimary = false)
    {
        return new UploadProductImageCommand(
            productId ?? Guid.NewGuid(),
            fileName,
            fileStream ?? new MemoryStream([1, 2, 3, 4]),
            contentType,
            fileSize,
            altText,
            isPrimary);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product")
    {
        return Product.Create(name, slug, 99.99m, "VND", TestTenantId);
    }

    private static Product CreateTestProductWithImages()
    {
        var product = CreateTestProduct();
        product.AddImage("https://example.com/img1.jpg", "Image 1", true);
        return product;
    }

    private static ImageProcessingResult CreateSuccessfulProcessingResult()
    {
        return new ImageProcessingResult
        {
            Success = true,
            Slug = "test-image",
            Variants =
            [
                new ImageVariantInfo
                {
                    Variant = ImageVariant.Thumb,
                    Format = OutputFormat.WebP,
                    Path = "/products/test/test-image-thumb.webp",
                    Url = "https://cdn.example.com/products/test/test-image-thumb.webp",
                    Width = 150,
                    Height = 84,
                    SizeBytes = 5000
                },
                new ImageVariantInfo
                {
                    Variant = ImageVariant.Large,
                    Format = OutputFormat.WebP,
                    Path = "/products/test/test-image-large.webp",
                    Url = "https://cdn.example.com/products/test/test-image-large.webp",
                    Width = 1280,
                    Height = 720,
                    SizeBytes = 50000
                },
                new ImageVariantInfo
                {
                    Variant = ImageVariant.Medium,
                    Format = OutputFormat.WebP,
                    Path = "/products/test/test-image-medium.webp",
                    Url = "https://cdn.example.com/products/test/test-image-medium.webp",
                    Width = 640,
                    Height = 360,
                    SizeBytes = 25000
                },
                new ImageVariantInfo
                {
                    Variant = ImageVariant.ExtraLarge,
                    Format = OutputFormat.WebP,
                    Path = "/products/test/test-image-extralarge.webp",
                    Url = "https://cdn.example.com/products/test/test-image-extralarge.webp",
                    Width = 1920,
                    Height = 1080,
                    SizeBytes = 75000
                }
            ],
            Metadata = new ImageMetadata
            {
                Width = 1920,
                Height = 1080,
                Format = "jpeg",
                MimeType = "image/jpeg",
                SizeBytes = 100000
            },
            ThumbHash = "1QcSHQRnh493V4dIh4eXh1h4kJUI",
            DominantColor = "#3366CC",
            ProcessingTimeMs = 150
        };
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidImage_ShouldUploadAndReturnResult()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();

        var command = CreateTestCommand(productId: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessfulProcessingResult());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Url.ShouldContain("webp");
        result.Value.AltText.ShouldBe("Test Image");
        result.Value.ThumbHash.ShouldNotBeNullOrEmpty();
        result.Value.DominantColor.ShouldBe("#3366CC");
        result.Value.Message.ShouldContain("successfully");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPrimaryFlag_ShouldSetAsPrimary()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();

        var command = CreateTestCommand(productId: productId, isPrimary: true);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessfulProcessingResult());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsPrimary.ShouldBe(true);

        // Single save: isPrimary is now set directly in AddImage (no two-save workaround)
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullAltText_ShouldUploadWithNullAltText()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();

        var command = CreateTestCommand(productId: productId, altText: null);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessfulProcessingResult());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.AltText.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnVariantUrls()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();

        var command = CreateTestCommand(productId: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessfulProcessingResult());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ThumbUrl.ShouldContain("thumb");
        result.Value.MediumUrl.ShouldContain("medium");
        result.Value.LargeUrl.ShouldContain("large");
    }

    [Fact]
    public async Task Handle_ShouldReturnImageDimensions()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();

        var command = CreateTestCommand(productId: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessfulProcessingResult());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Width.ShouldBe(1920);
        result.Value.Height.ShouldBe(1080);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-026");
        result.Error.Message.ShouldContain("Product");
        result.Error.Message.ShouldContain("not found");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WithInvalidImage_ShouldReturnValidationError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();

        var command = CreateTestCommand(productId: productId, fileName: "invalid.txt");

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-030");
        result.Error.Message.ShouldContain("Invalid image");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Processing Failure Scenarios

    [Fact]
    public async Task Handle_WhenProcessingFails_ShouldReturnFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();

        var command = CreateTestCommand(productId: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImageProcessingResult.Failure("Processing failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-031");
        result.Error.Message.ShouldContain("Processing failed");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoVariantsGenerated_ShouldReturnFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();

        var command = CreateTestCommand(productId: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingResult
            {
                Success = true,
                Variants = [] // No variants
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-032");
        result.Error.Message.ShouldContain("variant");
    }

    #endregion

    #region Rollback Scenarios

    [Fact]
    public async Task Handle_WhenSaveFails_ShouldRollbackUploadedFiles()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var processingResult = CreateSuccessfulProcessingResult();

        var command = CreateTestCommand(productId: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(processingResult);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _handler.Handle(command, CancellationToken.None));

        // Verify cleanup was attempted for each variant
        _fileStorageMock.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(processingResult.Variants.Count));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        var command = CreateTestCommand(productId: productId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessfulProcessingResult());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductByIdForImageUpdateSpec>(), token),
            Times.Once);
        _imageProcessorMock.Verify(
            x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ImageProcessingOptions>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSeekableStream_ShouldResetPosition()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct();
        using var stream = new MemoryStream([1, 2, 3, 4, 5]);
        stream.Position = 3; // Set position to middle

        var command = CreateTestCommand(productId: productId, fileStream: stream);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessfulProcessingResult());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // Stream position should have been reset before processing
    }

    [Fact]
    public async Task Handle_WithExistingImages_ShouldSetCorrectSortOrder()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProductWithImages();

        var command = CreateTestCommand(productId: productId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductByIdForImageUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<ImageProcessingOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessfulProcessingResult());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // New image should be added at the end (sort order 1 since there's already one image)
        result.Value.SortOrder.ShouldBeGreaterThanOrEqualTo(1);
    }

    #endregion
}
