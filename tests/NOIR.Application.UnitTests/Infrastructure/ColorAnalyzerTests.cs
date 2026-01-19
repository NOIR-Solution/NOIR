namespace NOIR.Application.UnitTests.Infrastructure;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Unit tests for ColorAnalyzer.
/// Tests dominant color extraction and palette generation from images.
/// </summary>
public class ColorAnalyzerTests
{
    #region ExtractDominantColorAsync Tests

    [Fact]
    public async Task ExtractDominantColorAsync_WithRedImage_ShouldReturnRedishColor()
    {
        // Arrange
        using var stream = CreateSolidColorImage(100, 100, Color.Red);

        // Act
        var result = await ColorAnalyzer.ExtractDominantColorAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("#");
        result.Should().HaveLength(7); // #RRGGBB format
        // Red color gets quantized, so check it's in the red range
        var r = Convert.ToInt32(result.Substring(1, 2), 16);
        r.Should().BeGreaterThan(200); // Should be mostly red
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithBlueImage_ShouldReturnBlueishColor()
    {
        // Arrange
        using var stream = CreateSolidColorImage(100, 100, Color.Blue);

        // Act
        var result = await ColorAnalyzer.ExtractDominantColorAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("#");
        var b = Convert.ToInt32(result.Substring(5, 2), 16);
        b.Should().BeGreaterThan(200); // Should be mostly blue
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithGreenImage_ShouldReturnGreenishColor()
    {
        // Arrange
        using var stream = CreateSolidColorImage(100, 100, Color.Green);

        // Act
        var result = await ColorAnalyzer.ExtractDominantColorAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("#");
        // Green (0, 128, 0) gets quantized
        var g = Convert.ToInt32(result.Substring(3, 2), 16);
        g.Should().BeGreaterThan(100); // Should have significant green component
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithTransparentImage_ShouldReturnFallbackGray()
    {
        // Arrange
        using var stream = CreateTransparentImage(100, 100);

        // Act
        var result = await ColorAnalyzer.ExtractDominantColorAsync(stream);

        // Assert
        result.Should().Be("#808080"); // Gray fallback for fully transparent images
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithVeryDarkImage_ShouldReturnFallbackGray()
    {
        // Arrange - Create image with colors too dark (brightness < 20)
        using var stream = CreateSolidColorImage(100, 100, new Rgba32(10, 10, 10, 255));

        // Act
        var result = await ColorAnalyzer.ExtractDominantColorAsync(stream);

        // Assert
        result.Should().Be("#808080"); // Gray fallback for very dark images
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithVeryLightImage_ShouldReturnFallbackGray()
    {
        // Arrange - Create image with colors too light (brightness > 235)
        using var stream = CreateSolidColorImage(100, 100, new Rgba32(250, 250, 250, 255));

        // Act
        var result = await ColorAnalyzer.ExtractDominantColorAsync(stream);

        // Assert
        result.Should().Be("#808080"); // Gray fallback for very light images
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithMultipleColors_ShouldReturnMostCommonColor()
    {
        // Arrange - Create image with mostly red but some blue
        using var image = new Image<Rgba32>(100, 100);

        // Fill 80% with red, 20% with blue
        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                if (x < 80)
                    image[x, y] = new Rgba32(255, 0, 0, 255); // Red
                else
                    image[x, y] = new Rgba32(0, 0, 255, 255); // Blue
            }
        }

        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        // Act
        var result = await ColorAnalyzer.ExtractDominantColorAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        var r = Convert.ToInt32(result.Substring(1, 2), 16);
        r.Should().BeGreaterThan(200); // Should be mostly red since red is dominant
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithSeekableStream_ShouldResetPosition()
    {
        // Arrange
        using var stream = CreateSolidColorImage(50, 50, Color.Orange);
        stream.Position = 10; // Set to non-zero position

        // Act
        var result = await ColorAnalyzer.ExtractDominantColorAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("#");
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithSmallImage_ShouldWork()
    {
        // Arrange - Very small image
        using var stream = CreateSolidColorImage(10, 10, Color.Purple);

        // Act
        var result = await ColorAnalyzer.ExtractDominantColorAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("#");
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithLargeImage_ShouldWork()
    {
        // Arrange - Large image (will be resized internally)
        using var stream = CreateSolidColorImage(1000, 1000, Color.Cyan);

        // Act
        var result = await ColorAnalyzer.ExtractDominantColorAsync(stream);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("#");
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithCancellation_ShouldThrow()
    {
        // Arrange
        using var stream = CreateSolidColorImage(100, 100, Color.Red);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await ColorAnalyzer.ExtractDominantColorAsync(stream, cts.Token));
    }

    #endregion

    #region ExtractPaletteAsync Tests

    [Fact]
    public async Task ExtractPaletteAsync_WithMultiColorImage_ShouldReturnPalette()
    {
        // Arrange
        using var image = new Image<Rgba32>(100, 100);

        // Create 4 quadrants with different colors
        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                if (x < 50 && y < 50)
                    image[x, y] = new Rgba32(255, 0, 0, 255); // Red
                else if (x >= 50 && y < 50)
                    image[x, y] = new Rgba32(0, 255, 0, 255); // Green
                else if (x < 50 && y >= 50)
                    image[x, y] = new Rgba32(0, 0, 255, 255); // Blue
                else
                    image[x, y] = new Rgba32(255, 255, 0, 255); // Yellow
            }
        }

        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        // Act
        var result = await ColorAnalyzer.ExtractPaletteAsync(stream, paletteSize: 5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessThanOrEqualTo(5);
        result.Should().OnlyContain(c => c.StartsWith("#") && c.Length == 7);
    }

    [Fact]
    public async Task ExtractPaletteAsync_WithDefaultPaletteSize_ShouldReturn5Colors()
    {
        // Arrange
        using var image = new Image<Rgba32>(100, 100);

        // Create multiple color bands
        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                var colorIndex = x / 20;
                image[x, y] = colorIndex switch
                {
                    0 => new Rgba32(255, 0, 0, 255),
                    1 => new Rgba32(0, 255, 0, 255),
                    2 => new Rgba32(0, 0, 255, 255),
                    3 => new Rgba32(255, 255, 0, 255),
                    _ => new Rgba32(255, 0, 255, 255)
                };
            }
        }

        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        // Act
        var result = await ColorAnalyzer.ExtractPaletteAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task ExtractPaletteAsync_WithCustomPaletteSize_ShouldRespectSize()
    {
        // Arrange
        using var stream = CreateSolidColorImage(100, 100, Color.Red);

        // Act
        var result = await ColorAnalyzer.ExtractPaletteAsync(stream, paletteSize: 3);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task ExtractPaletteAsync_WithSingleColorImage_ShouldReturnOneColor()
    {
        // Arrange
        using var stream = CreateSolidColorImage(100, 100, Color.Orange);

        // Act
        var result = await ColorAnalyzer.ExtractPaletteAsync(stream, paletteSize: 5);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Should().StartWith("#");
    }

    [Fact]
    public async Task ExtractPaletteAsync_WithTransparentPixels_ShouldSkipThem()
    {
        // Arrange
        using var image = new Image<Rgba32>(100, 100);

        // Half transparent, half red
        for (int y = 0; y < 100; y++)
        {
            for (int x = 0; x < 100; x++)
            {
                if (x < 50)
                    image[x, y] = new Rgba32(0, 0, 0, 0); // Transparent
                else
                    image[x, y] = new Rgba32(255, 0, 0, 255); // Red
            }
        }

        using var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;

        // Act
        var result = await ColorAnalyzer.ExtractPaletteAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ExtractPaletteAsync_WithCancellation_ShouldThrow()
    {
        // Arrange
        using var stream = CreateSolidColorImage(100, 100, Color.Red);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await ColorAnalyzer.ExtractPaletteAsync(stream, ct: cts.Token));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExtractDominantColorAsync_WithInvalidImageStream_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        // Act & Assert
        await Assert.ThrowsAsync<SixLabors.ImageSharp.UnknownImageFormatException>(
            async () => await ColorAnalyzer.ExtractDominantColorAsync(stream));
    }

    [Fact]
    public async Task ExtractPaletteAsync_WithInvalidImageStream_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        // Act & Assert
        await Assert.ThrowsAsync<SixLabors.ImageSharp.UnknownImageFormatException>(
            async () => await ColorAnalyzer.ExtractPaletteAsync(stream));
    }

    [Fact]
    public async Task ExtractDominantColorAsync_WithEmptyStream_ShouldThrow()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<SixLabors.ImageSharp.UnknownImageFormatException>(
            async () => await ColorAnalyzer.ExtractDominantColorAsync(stream));
    }

    #endregion

    #region Helper Methods

    private static MemoryStream CreateSolidColorImage(int width, int height, Color color)
    {
        var image = new Image<Rgba32>(width, height, color);
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        image.Dispose();
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateSolidColorImage(int width, int height, Rgba32 color)
    {
        var image = new Image<Rgba32>(width, height, color);
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        image.Dispose();
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateTransparentImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        image.Dispose();
        stream.Position = 0;
        return stream;
    }

    #endregion
}
