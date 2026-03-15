namespace NOIR.Application.UnitTests.Infrastructure;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Unit tests for ImageProcessorService.
/// Tests image processing, variant generation, metadata extraction, and validation.
/// </summary>
public class ImageProcessorServiceTests
{
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<ILogger<ImageProcessorService>> _loggerMock;
    private readonly ImageProcessingSettings _settings;
    private readonly ImageProcessorService _sut;

    public ImageProcessorServiceTests()
    {
        _fileStorageMock = new Mock<IFileStorage>();
        _loggerMock = new Mock<ILogger<ImageProcessorService>>();

        _settings = new ImageProcessingSettings
        {
            DefaultQuality = 90,
            WebPQuality = 90,
            AvifQuality = 75,
            ThumbSize = 150,
            MediumSize = 640,
            LargeSize = 1280,
            ExtraLargeSize = 1920,
            GenerateWebP = true,
            GenerateJpeg = true,
            GenerateAvif = false,
            AutoRotate = true,
            StorageFolder = "images",
            AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"]
        };

        var optionsMock = new Mock<IOptionsMonitor<ImageProcessingSettings>>();
        optionsMock.Setup(x => x.CurrentValue).Returns(_settings);

        // Setup file storage mock to return paths
        _fileStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string fileName, Stream _, string folder, CancellationToken _) =>
                $"{folder}/{fileName}");

        _fileStorageMock
            .Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns((string path) => $"/media/{path}");

        _sut = new ImageProcessorService(
            optionsMock.Object,
            _fileStorageMock.Object,
            _loggerMock.Object);
    }

    #region ProcessAsync Tests

    [Fact]
    public async Task ProcessAsync_WithValidImage_ShouldReturnSuccessResult()
    {
        // Arrange
        using var stream = CreateTestImage(500, 400);
        var options = new ImageProcessingOptions
        {
            GenerateThumbHash = true,
            ExtractDominantColor = true
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "test-image.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        result.ErrorMessage.ShouldBeNull();
        result.Slug.ShouldNotBeNullOrEmpty();
        result.Variants.ShouldNotBeEmpty();
        result.Metadata.ShouldNotBeNull();
        result.ProcessingTimeMs.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ProcessAsync_WithThumbHashEnabled_ShouldGenerateThumbHash()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);
        var options = new ImageProcessingOptions
        {
            GenerateThumbHash = true,
            ExtractDominantColor = false
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "test.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        result.ThumbHash.ShouldNotBeNullOrEmpty();
        // ThumbHash should be valid Base64
        var action = () => Convert.FromBase64String(result.ThumbHash!);
        action.ShouldNotThrow();
    }

    [Fact]
    public async Task ProcessAsync_WithDominantColorEnabled_ShouldExtractColor()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200, Color.Red);
        var options = new ImageProcessingOptions
        {
            GenerateThumbHash = false,
            ExtractDominantColor = true
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "test.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        result.DominantColor.ShouldNotBeNullOrEmpty();
        result.DominantColor.ShouldStartWith("#");
    }

    [Fact]
    public async Task ProcessAsync_WithThumbHashDisabled_ShouldNotGenerateThumbHash()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);
        var options = new ImageProcessingOptions
        {
            GenerateThumbHash = false,
            ExtractDominantColor = false
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "test.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        result.ThumbHash.ShouldBeNull();
    }

    [Fact]
    public async Task ProcessAsync_ShouldGenerateSlugFromFileName()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);
        var options = new ImageProcessingOptions();

        // Act
        var result = await _sut.ProcessAsync(stream, "My Test Image.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        result.Slug.ShouldStartWith("my-test-image");
        result.Slug.ShouldMatch(@"^my-test-image_[a-f0-9]{8}$");
    }

    [Fact]
    public async Task ProcessAsync_ShouldExtractMetadata()
    {
        // Arrange
        using var stream = CreateTestImage(800, 600);
        var options = new ImageProcessingOptions();

        // Act
        var result = await _sut.ProcessAsync(stream, "test.png", options);

        // Assert
        result.Success.ShouldBe(true);
        result.Metadata.ShouldNotBeNull();
        result.Metadata!.Width.ShouldBe(800);
        result.Metadata.Height.ShouldBe(600);
        result.Metadata.Format.ShouldBe("PNG");
    }

    [Fact]
    public async Task ProcessAsync_WithSpecificVariants_ShouldGenerateOnlyThoseVariants()
    {
        // Arrange
        using var stream = CreateTestImage(1000, 800);
        var options = new ImageProcessingOptions
        {
            Variants = [ImageVariant.Thumb, ImageVariant.Medium],
            Formats = [OutputFormat.Jpeg],
            GenerateThumbHash = false,
            ExtractDominantColor = false
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "test.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        result.Variants.Count().ShouldBe(2);
        result.Variants.ShouldAllBe(v => v.Variant == ImageVariant.Thumb || v.Variant == ImageVariant.Medium);
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidImage_ShouldReturnFailure()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));
        var options = new ImageProcessingOptions();

        // Act
        var result = await _sut.ProcessAsync(stream, "test.jpg", options);

        // Assert
        result.Success.ShouldBe(false);
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessAsync_ShouldCallFileStorageUpload()
    {
        // Arrange - Use image larger than Thumb size (150px) to ensure variant is generated
        using var stream = CreateTestImage(400, 400);
        var options = new ImageProcessingOptions
        {
            Variants = [ImageVariant.Thumb], // Thumb is 150px, fits within 400px image
            Formats = [OutputFormat.Jpeg],
            GenerateThumbHash = false,
            ExtractDominantColor = false
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "test.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        _fileStorageMock.Verify(
            x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_WithCustomStorageFolder_ShouldUseFolder()
    {
        // Arrange - Use image larger than Thumb size (150px) to ensure variant is generated
        using var stream = CreateTestImage(400, 400);
        var options = new ImageProcessingOptions
        {
            StorageFolder = "custom-folder",
            Variants = [ImageVariant.Thumb], // Thumb is 150px, fits within 400px image
            Formats = [OutputFormat.Jpeg],
            GenerateThumbHash = false,
            ExtractDominantColor = false
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "test.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        _fileStorageMock.Verify(
            x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), "custom-folder", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessAsync_SmallImageShouldSkipLargerVariants()
    {
        // Arrange - Image smaller than Medium size
        using var stream = CreateTestImage(300, 200);
        var options = new ImageProcessingOptions
        {
            Formats = [OutputFormat.Jpeg],
            GenerateThumbHash = false,
            ExtractDominantColor = false
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "small.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        // Should not generate Large variant for small images (no upscaling)
        result.Variants.ShouldNotContain(v => v.Variant == ImageVariant.Large);
    }

    [Fact]
    public async Task ProcessAsync_LargeImageShouldGenerateAllVariants()
    {
        // Arrange - Large image
        using var stream = CreateTestImage(2000, 1500);
        var options = new ImageProcessingOptions
        {
            Formats = [OutputFormat.Jpeg],
            GenerateThumbHash = false,
            ExtractDominantColor = false
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "large.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        // SEO-optimized: 3 variants (Thumb, Medium, Large)
        result.Variants.ShouldContain(v => v.Variant == ImageVariant.Thumb);
        result.Variants.ShouldContain(v => v.Variant == ImageVariant.Medium);
        result.Variants.ShouldContain(v => v.Variant == ImageVariant.Large);
    }

    #endregion

    #region GenerateVariantAsync Tests

    [Fact]
    public async Task GenerateVariantAsync_WithValidImage_ShouldReturnStream()
    {
        // Arrange
        using var stream = CreateTestImage(500, 400);

        // Act
        using var result = await _sut.GenerateVariantAsync(stream, ImageVariant.Medium, OutputFormat.Jpeg);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateVariantAsync_WithWebPFormat_ShouldReturnWebP()
    {
        // Arrange
        using var stream = CreateTestImage(500, 400);

        // Act
        using var result = await _sut.GenerateVariantAsync(stream, ImageVariant.Medium, OutputFormat.WebP);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);

        // Verify it's a valid image
        result.Position = 0;
        var info = await Image.IdentifyAsync(result);
        info.ShouldNotBeNull();
    }

    [Fact]
    public async Task GenerateVariantAsync_WithJpegFormat_ShouldReturnJpeg()
    {
        // Arrange
        using var stream = CreateTestImage(500, 400);

        // Act
        using var result = await _sut.GenerateVariantAsync(stream, ImageVariant.Large, OutputFormat.Jpeg);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);

        // Verify it's JPEG
        result.Position = 0;
        var info = await Image.IdentifyAsync(result);
        info.ShouldNotBeNull();
        info.Metadata.DecodedImageFormat?.Name.ShouldBe("JPEG");
    }

    [Fact]
    public async Task GenerateVariantAsync_WithPngFormat_ShouldReturnPng()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);

        // Act
        using var result = await _sut.GenerateVariantAsync(stream, ImageVariant.Thumb, OutputFormat.Png);

        // Assert
        result.ShouldNotBeNull();
        result.Position = 0;
        var info = await Image.IdentifyAsync(result);
        info.ShouldNotBeNull();
        info.Metadata.DecodedImageFormat?.Name.ShouldBe("PNG");
    }

    [Fact]
    public async Task GenerateVariantAsync_ShouldResizeImage()
    {
        // Arrange - 1000x800 image
        using var stream = CreateTestImage(1000, 800);

        // Act - Generate medium variant (640px)
        using var result = await _sut.GenerateVariantAsync(stream, ImageVariant.Medium, OutputFormat.Jpeg);

        // Assert
        result.Position = 0;
        var info = await Image.IdentifyAsync(result);
        info.Width.ShouldBeLessThanOrEqualTo(_settings.MediumSize);
        info.Height.ShouldBeLessThanOrEqualTo(_settings.MediumSize);
    }

    [Fact]
    public async Task GenerateVariantAsync_ShouldMaintainAspectRatio()
    {
        // Arrange - 1000x500 (2:1 aspect ratio)
        using var stream = CreateTestImage(1000, 500);

        // Act
        using var result = await _sut.GenerateVariantAsync(stream, ImageVariant.Medium, OutputFormat.Jpeg);

        // Assert
        result.Position = 0;
        var info = await Image.IdentifyAsync(result);
        var aspectRatio = (double)info.Width / info.Height;
        aspectRatio.ShouldBe(2.0, 0.05);
    }

    [Fact]
    public async Task GenerateVariantAsync_WithInvalidImage_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        // Act & Assert
        await Assert.ThrowsAsync<SixLabors.ImageSharp.UnknownImageFormatException>(
            async () => await _sut.GenerateVariantAsync(stream, ImageVariant.Medium, OutputFormat.Jpeg));
    }

    #endregion

    #region GetMetadataAsync Tests

    [Fact]
    public async Task GetMetadataAsync_WithValidImage_ShouldReturnMetadata()
    {
        // Arrange
        using var stream = CreateTestImage(800, 600);

        // Act
        var result = await _sut.GetMetadataAsync(stream);

        // Assert
        result.ShouldNotBeNull();
        result.Width.ShouldBe(800);
        result.Height.ShouldBe(600);
    }

    [Fact]
    public async Task GetMetadataAsync_ShouldReturnFormat()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);

        // Act
        var result = await _sut.GetMetadataAsync(stream);

        // Assert
        result.Format.ShouldNotBeNullOrEmpty();
        result.MimeType.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetMetadataAsync_WithInvalidImage_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        // Act & Assert
        await Assert.ThrowsAsync<SixLabors.ImageSharp.UnknownImageFormatException>(
            async () => await _sut.GetMetadataAsync(stream));
    }

    [Fact]
    public async Task GetMetadataAsync_ShouldResetStreamPosition()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);
        stream.Position = 50;

        // Act
        var result = await _sut.GetMetadataAsync(stream);

        // Assert
        result.ShouldNotBeNull();
        result.Width.ShouldBe(200);
    }

    #endregion

    #region GenerateThumbHashAsync Tests

    [Fact]
    public async Task GenerateThumbHashAsync_WithValidImage_ShouldReturnBase64()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);

        // Act
        var result = await _sut.GenerateThumbHashAsync(stream);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        var action = () => Convert.FromBase64String(result);
        action.ShouldNotThrow();
    }

    #endregion

    #region ExtractDominantColorAsync Tests

    [Fact]
    public async Task ExtractDominantColorAsync_WithValidImage_ShouldReturnHexColor()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200, Color.Blue);

        // Act
        var result = await _sut.ExtractDominantColorAsync(stream);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldStartWith("#");
        result.Length.ShouldBe(7);
    }

    #endregion

    #region IsValidImageAsync Tests

    [Fact]
    public async Task IsValidImageAsync_WithValidJpeg_ShouldReturnTrue()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);

        // Act
        var result = await _sut.IsValidImageAsync(stream, "test.jpg");

        // Assert
        result.ShouldBe(true);
    }

    [Fact]
    public async Task IsValidImageAsync_WithValidPng_ShouldReturnTrue()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);

        // Act
        var result = await _sut.IsValidImageAsync(stream, "test.png");

        // Assert
        result.ShouldBe(true);
    }

    [Fact]
    public async Task IsValidImageAsync_WithInvalidExtension_ShouldReturnFalse()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);

        // Act
        var result = await _sut.IsValidImageAsync(stream, "test.txt");

        // Assert
        result.ShouldBe(false);
    }

    [Fact]
    public async Task IsValidImageAsync_WithInvalidContent_ShouldReturnFalse()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        // Act
        var result = await _sut.IsValidImageAsync(stream, "test.jpg");

        // Assert
        result.ShouldBe(false);
    }

    [Fact]
    public async Task IsValidImageAsync_WithNoExtension_ShouldReturnFalse()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);

        // Act
        var result = await _sut.IsValidImageAsync(stream, "testfile");

        // Assert
        result.ShouldBe(false);
    }

    [Fact]
    public async Task IsValidImageAsync_WithEmptyFileName_ShouldReturnFalse()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);

        // Act
        var result = await _sut.IsValidImageAsync(stream, "");

        // Assert
        result.ShouldBe(false);
    }

    [Fact]
    public async Task IsValidImageAsync_ShouldResetStreamPosition()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);
        stream.Position = 50;

        // Act
        var result = await _sut.IsValidImageAsync(stream, "test.jpg");

        // Assert
        result.ShouldBe(true);
    }

    #endregion

    #region GenerateSrcset Tests

    [Fact]
    public void GenerateSrcset_WithVariants_ShouldReturnSrcsetString()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/media/test-thumb.jpg", Width = 150 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "/media/test-medium.jpg", Width = 640 }
        };

        // Act
        var result = _sut.GenerateSrcset("/media", variants);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("/media/test-thumb.jpg 150w");
        result.ShouldContain("/media/test-medium.jpg 640w");
    }

    [Fact]
    public void GenerateSrcset_WithEmptyVariants_ShouldReturnEmptyString()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>();

        // Act
        var result = _sut.GenerateSrcset("/media", variants);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task ProcessAsync_WithSeekableStream_ShouldWork()
    {
        // Arrange
        using var stream = CreateTestImage(200, 200);
        stream.Position = 0;
        var options = new ImageProcessingOptions
        {
            GenerateThumbHash = false,
            ExtractDominantColor = false,
            Variants = [ImageVariant.Thumb],
            Formats = [OutputFormat.Jpeg]
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "test.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleFormats_ShouldGenerateAllFormats()
    {
        // Arrange
        using var stream = CreateTestImage(500, 400);
        var options = new ImageProcessingOptions
        {
            Variants = [ImageVariant.Thumb],
            Formats = [OutputFormat.Jpeg, OutputFormat.WebP],
            GenerateThumbHash = false,
            ExtractDominantColor = false
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "test.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
        result.Variants.ShouldContain(v => v.Format == OutputFormat.Jpeg);
        result.Variants.ShouldContain(v => v.Format == OutputFormat.WebP);
    }

    [Fact]
    public async Task ProcessAsync_WithVerySmallImage_ShouldWork()
    {
        // Arrange - 1x1 pixel image
        using var stream = CreateTestImage(1, 1);
        var options = new ImageProcessingOptions
        {
            GenerateThumbHash = false,
            ExtractDominantColor = false,
            Variants = [ImageVariant.Thumb],
            Formats = [OutputFormat.Jpeg]
        };

        // Act
        var result = await _sut.ProcessAsync(stream, "tiny.jpg", options);

        // Assert
        result.Success.ShouldBe(true);
    }

    [Fact]
    public async Task GenerateVariantAsync_WithThumbVariant_ShouldResizeCorrectly()
    {
        // Arrange
        using var stream = CreateTestImage(500, 400);

        // Act
        using var result = await _sut.GenerateVariantAsync(stream, ImageVariant.Thumb, OutputFormat.Jpeg);

        // Assert
        result.Position = 0;
        var info = await Image.IdentifyAsync(result);
        Math.Max(info.Width, info.Height).ShouldBeLessThanOrEqualTo(_settings.ThumbSize);
    }

    #endregion

    #region Helper Methods

    private static MemoryStream CreateTestImage(int width, int height, Color? color = null)
    {
        var image = new Image<Rgba32>(width, height, color ?? Color.LightGray);
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        image.Dispose();
        stream.Position = 0;
        return stream;
    }

    #endregion
}
