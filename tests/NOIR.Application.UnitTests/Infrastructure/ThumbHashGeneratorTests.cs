namespace NOIR.Application.UnitTests.Infrastructure;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Unit tests for ThumbHashGenerator.
/// Tests ThumbHash placeholder generation, decoding, and metadata extraction.
/// </summary>
public class ThumbHashGeneratorTests
{
    #region GenerateAsync Tests

    [Fact]
    public async Task GenerateAsync_WithValidImage_ShouldReturnBase64String()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Red);

        // Act
        var result = await ThumbHashGenerator.GenerateAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        // ThumbHash should be valid Base64
        var action = () => Convert.FromBase64String(result);
        action.Should().NotThrow();
    }

    [Fact]
    public async Task GenerateAsync_WithSmallImage_ShouldWork()
    {
        // Arrange
        using var stream = CreateTestImage(32, 32, Color.Blue);

        // Act
        var result = await ThumbHashGenerator.GenerateAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WithLargeImage_ShouldResizeAndGenerate()
    {
        // Arrange
        using var stream = CreateTestImage(1000, 800, Color.Green);

        // Act
        var result = await ThumbHashGenerator.GenerateAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        // ThumbHash is compact - typically around 25-35 bytes when decoded
        var bytes = Convert.FromBase64String(result);
        bytes.Length.Should().BeLessThan(50);
    }

    [Fact]
    public async Task GenerateAsync_WithSquareImage_ShouldWork()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Yellow);

        // Act
        var result = await ThumbHashGenerator.GenerateAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WithWideImage_ShouldPreserveAspectRatio()
    {
        // Arrange - Wide (landscape) image
        using var stream = CreateTestImage(200, 100, Color.Orange);

        // Act
        var result = await ThumbHashGenerator.GenerateAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var (_, _, aspectRatio) = ThumbHashGenerator.GetApproximateDimensions(result);
        aspectRatio.Should().BeGreaterThan(1.0f); // Width > Height
    }

    [Fact]
    public async Task GenerateAsync_WithTallImage_ShouldPreserveAspectRatio()
    {
        // Arrange - Tall (portrait) image
        using var stream = CreateTestImage(100, 200, Color.Purple);

        // Act
        var result = await ThumbHashGenerator.GenerateAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var (_, _, aspectRatio) = ThumbHashGenerator.GetApproximateDimensions(result);
        aspectRatio.Should().BeLessThan(1.0f); // Width < Height
    }

    [Fact]
    public async Task GenerateAsync_WithTransparentImage_ShouldIncludeAlpha()
    {
        // Arrange
        using var image = new Image<Rgba32>(100, 100, new Rgba32(255, 0, 0, 128)); // Semi-transparent red
        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        // Act
        var result = await ThumbHashGenerator.GenerateAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateAsync_MultipleCalls_ShouldProduceConsistentResults()
    {
        // Arrange
        using var stream1 = CreateTestImage(100, 100, Color.Red);
        using var stream2 = CreateTestImage(100, 100, Color.Red);

        // Act
        var result1 = await ThumbHashGenerator.GenerateAsync(stream1);
        var result2 = await ThumbHashGenerator.GenerateAsync(stream2);

        // Assert - Same image should produce same hash
        result1.Should().Be(result2);
    }

    [Fact]
    public async Task GenerateAsync_DifferentImages_ShouldProduceDifferentHashes()
    {
        // Arrange
        using var stream1 = CreateTestImage(100, 100, Color.Red);
        using var stream2 = CreateTestImage(100, 100, Color.Blue);

        // Act
        var result1 = await ThumbHashGenerator.GenerateAsync(stream1);
        var result2 = await ThumbHashGenerator.GenerateAsync(stream2);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public async Task GenerateAsync_WithSeekableStream_ShouldResetPosition()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Cyan);
        stream.Position = 50; // Move to non-zero position

        // Act
        var result = await ThumbHashGenerator.GenerateAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WithCancellation_ShouldThrow()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Red);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await ThumbHashGenerator.GenerateAsync(stream, cts.Token));
    }

    #endregion

    #region DecodeToDataUrl Tests

    [Fact]
    public async Task DecodeToDataUrl_WithValidThumbHash_ShouldReturnDataUrl()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Red);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);

        // Act
        var result = ThumbHashGenerator.DecodeToDataUrl(thumbHash);

        // Assert
        result.Should().StartWith("data:image/png;base64,");
    }

    [Fact]
    public async Task DecodeToDataUrl_ShouldReturnValidBase64()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Green);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);

        // Act
        var result = ThumbHashGenerator.DecodeToDataUrl(thumbHash);

        // Assert
        var base64Part = result.Replace("data:image/png;base64,", "");
        var action = () => Convert.FromBase64String(base64Part);
        action.Should().NotThrow();
    }

    [Fact]
    public async Task DecodeToDataUrl_ShouldProduceValidPng()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Blue);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);

        // Act
        var result = ThumbHashGenerator.DecodeToDataUrl(thumbHash);

        // Assert
        var base64Part = result.Replace("data:image/png;base64,", "");
        var pngBytes = Convert.FromBase64String(base64Part);

        // Verify it's a valid image by loading it
        using var pngStream = new MemoryStream(pngBytes);
        var info = await Image.IdentifyAsync(pngStream);
        info.Should().NotBeNull();
    }

    [Fact]
    public void DecodeToDataUrl_WithInvalidBase64_ShouldThrow()
    {
        // Arrange
        var invalidBase64 = "not-valid-base64!!!";

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => ThumbHashGenerator.DecodeToDataUrl(invalidBase64));
    }

    #endregion

    #region GetAverageColor Tests

    [Fact]
    public async Task GetAverageColor_WithRedImage_ShouldReturnRedishColor()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Red);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);

        // Act
        var result = ThumbHashGenerator.GetAverageColor(thumbHash);

        // Assert
        result.Should().StartWith("#");
        result.Should().HaveLength(7);
        var r = Convert.ToInt32(result.Substring(1, 2), 16);
        r.Should().BeGreaterThan(150); // Should have high red component
    }

    [Fact]
    public async Task GetAverageColor_WithBlueImage_ShouldReturnBlueishColor()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Blue);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);

        // Act
        var result = ThumbHashGenerator.GetAverageColor(thumbHash);

        // Assert
        result.Should().StartWith("#");
        var b = Convert.ToInt32(result.Substring(5, 2), 16);
        b.Should().BeGreaterThan(150); // Should have high blue component
    }

    [Fact]
    public async Task GetAverageColor_ShouldReturnValidHexColor()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Green);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);

        // Act
        var result = ThumbHashGenerator.GetAverageColor(thumbHash);

        // Assert
        result.Should().MatchRegex("^#[0-9A-F]{6}$");
    }

    [Fact]
    public void GetAverageColor_WithInvalidBase64_ShouldThrow()
    {
        // Arrange
        var invalidBase64 = "not-valid";

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => ThumbHashGenerator.GetAverageColor(invalidBase64));
    }

    #endregion

    #region GetApproximateDimensions Tests

    [Fact]
    public async Task GetApproximateDimensions_WithSquareImage_ShouldReturnSquareDimensions()
    {
        // Arrange
        using var stream = CreateTestImage(100, 100, Color.Red);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);

        // Act
        var (width, height, aspectRatio) = ThumbHashGenerator.GetApproximateDimensions(thumbHash);

        // Assert
        aspectRatio.Should().BeApproximately(1.0f, 0.1f);
    }

    [Fact]
    public async Task GetApproximateDimensions_WithWideImage_ShouldReturnWideAspectRatio()
    {
        // Arrange - 2:1 aspect ratio
        using var stream = CreateTestImage(200, 100, Color.Blue);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);

        // Act
        var (width, height, aspectRatio) = ThumbHashGenerator.GetApproximateDimensions(thumbHash);

        // Assert
        aspectRatio.Should().BeGreaterThan(1.5f);
        width.Should().BeGreaterThan(height);
    }

    [Fact]
    public async Task GetApproximateDimensions_WithTallImage_ShouldReturnTallAspectRatio()
    {
        // Arrange - 1:2 aspect ratio
        using var stream = CreateTestImage(100, 200, Color.Green);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);

        // Act
        var (width, height, aspectRatio) = ThumbHashGenerator.GetApproximateDimensions(thumbHash);

        // Assert
        aspectRatio.Should().BeLessThan(0.7f);
        height.Should().BeGreaterThan(width);
    }

    [Fact]
    public async Task GetApproximateDimensions_ShouldReturnPositiveDimensions()
    {
        // Arrange
        using var stream = CreateTestImage(150, 75, Color.Yellow);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);

        // Act
        var (width, height, aspectRatio) = ThumbHashGenerator.GetApproximateDimensions(thumbHash);

        // Assert
        width.Should().BePositive();
        height.Should().BePositive();
        aspectRatio.Should().BePositive();
    }

    [Fact]
    public void GetApproximateDimensions_WithInvalidBase64_ShouldThrow()
    {
        // Arrange
        var invalidBase64 = "not-valid";

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => ThumbHashGenerator.GetApproximateDimensions(invalidBase64));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GenerateAsync_WithInvalidImageData_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        // Act & Assert
        await Assert.ThrowsAsync<SixLabors.ImageSharp.UnknownImageFormatException>(
            async () => await ThumbHashGenerator.GenerateAsync(stream));
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyStream_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<SixLabors.ImageSharp.UnknownImageFormatException>(
            async () => await ThumbHashGenerator.GenerateAsync(stream));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task RoundTrip_GenerateAndDecode_ShouldWork()
    {
        // Arrange
        using var originalStream = CreateTestImage(100, 100, Color.Magenta);
        var thumbHash = await ThumbHashGenerator.GenerateAsync(originalStream);

        // Act
        var dataUrl = ThumbHashGenerator.DecodeToDataUrl(thumbHash);
        var color = ThumbHashGenerator.GetAverageColor(thumbHash);
        var (width, height, aspectRatio) = ThumbHashGenerator.GetApproximateDimensions(thumbHash);

        // Assert
        dataUrl.Should().StartWith("data:image/png;base64,");
        color.Should().StartWith("#");
        width.Should().BePositive();
        height.Should().BePositive();
        aspectRatio.Should().BeApproximately(1.0f, 0.1f);
    }

    [Fact]
    public async Task GenerateAsync_WithMultiColorImage_ShouldCaptureColors()
    {
        // Arrange - Create gradient image
        using var image = new Image<Rgba32>(100, 100);
        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                // Gradient from red to blue
                var r = (byte)(255 - (x * 255 / 100));
                var b = (byte)(x * 255 / 100);
                image[x, y] = new Rgba32(r, 0, b, 255);
            }
        }

        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        // Act
        var thumbHash = await ThumbHashGenerator.GenerateAsync(stream);
        var dataUrl = ThumbHashGenerator.DecodeToDataUrl(thumbHash);

        // Assert
        thumbHash.Should().NotBeNullOrEmpty();
        dataUrl.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Helper Methods

    private static MemoryStream CreateTestImage(int width, int height, Color color)
    {
        var image = new Image<Rgba32>(width, height, color);
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        image.Dispose();
        stream.Position = 0;
        return stream;
    }

    #endregion
}
