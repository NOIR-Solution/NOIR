using NOIR.Application.Features.Products.Commands.UploadProductImage;

namespace NOIR.Application.UnitTests.Features.Products.Commands.UploadProductImage;

/// <summary>
/// Unit tests for UploadProductImageCommandValidator.
/// </summary>
public class UploadProductImageCommandValidatorTests
{
    private readonly UploadProductImageCommandValidator _validator;

    public UploadProductImageCommandValidatorTests()
    {
        _validator = new UploadProductImageCommandValidator();
    }

    private static UploadProductImageCommand CreateValidCommand() =>
        new(
            ProductId: Guid.NewGuid(),
            FileName: "product-image.jpg",
            FileStream: new MemoryStream(new byte[] { 1, 2, 3 }),
            ContentType: "image/jpeg",
            FileSize: 1024,
            AltText: "Product image",
            IsPrimary: false);

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== ProductId Validation =====

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { ProductId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    // ===== FileName Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyFileName_ShouldFail(string? fileName)
    {
        // Arrange
        var command = CreateValidCommand() with { FileName = fileName! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("File name is required.");
    }

    // ===== FileStream Validation =====

    [Fact]
    public async Task Validate_WithNullFileStream_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { FileStream = null! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileStream)
            .WithErrorMessage("File stream is required.");
    }

    // ===== FileSize Validation =====

    [Fact]
    public async Task Validate_WithZeroFileSize_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { FileSize = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileSize)
            .WithErrorMessage("File cannot be empty.");
    }

    [Fact]
    public async Task Validate_WithNegativeFileSize_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { FileSize = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileSize);
    }

    [Fact]
    public async Task Validate_WithFileSizeExceeding10MB_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { FileSize = 10 * 1024 * 1024 + 1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileSize)
            .WithErrorMessage("File size cannot exceed 10 MB.");
    }

    [Fact]
    public async Task Validate_WithFileSizeExactly10MB_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { FileSize = 10 * 1024 * 1024 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FileSize);
    }

    // ===== ContentType Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyContentType_ShouldFail(string? contentType)
    {
        // Arrange
        var command = CreateValidCommand() with { ContentType = contentType! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("image/webp")]
    [InlineData("image/avif")]
    [InlineData("image/heic")]
    [InlineData("image/heif")]
    public async Task Validate_WithValidContentType_ShouldPass(string contentType)
    {
        // Arrange
        var command = CreateValidCommand() with { ContentType = contentType };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("image/bmp")]
    [InlineData("image/tiff")]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    [InlineData("video/mp4")]
    public async Task Validate_WithInvalidContentType_ShouldFail(string contentType)
    {
        // Arrange
        var command = CreateValidCommand() with { ContentType = contentType };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContentType)
            .WithErrorMessage("Invalid image format. Allowed formats: JPEG, PNG, GIF, WebP, AVIF, HEIC, HEIF.");
    }

    // ===== AltText Validation =====

    [Fact]
    public async Task Validate_WithAltTextExceeding500Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { AltText = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AltText)
            .WithErrorMessage("Alt text cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WithNullAltText_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { AltText = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AltText);
    }
}
