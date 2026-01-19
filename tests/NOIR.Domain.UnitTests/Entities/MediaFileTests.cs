namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the MediaFile entity.
/// Tests factory methods, computed properties, and metadata handling.
/// </summary>
public class MediaFileTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidMediaFile()
    {
        // Arrange
        var shortId = "a1b2c3d4";
        var slug = "hero-banner_a1b2c3d4";
        var originalFileName = "hero-banner.jpg";
        var folder = "blog";
        var defaultUrl = "/uploads/blog/hero-banner_large.webp";
        var width = 1920;
        var height = 1080;
        var format = "jpeg";
        var mimeType = "image/jpeg";
        var sizeBytes = 2048000L;
        var uploadedBy = "user-123";

        // Act
        var mediaFile = MediaFile.Create(
            shortId, slug, originalFileName, folder, defaultUrl,
            thumbHash: null, dominantColor: null,
            width, height, format, mimeType, sizeBytes,
            hasTransparency: false, variantsJson: "[]", srcsetsJson: "{}",
            uploadedBy);

        // Assert
        mediaFile.Should().NotBeNull();
        mediaFile.Id.Should().NotBe(Guid.Empty);
        mediaFile.ShortId.Should().Be(shortId);
        mediaFile.Slug.Should().Be(slug);
        mediaFile.OriginalFileName.Should().Be(originalFileName);
        mediaFile.Folder.Should().Be(folder);
        mediaFile.DefaultUrl.Should().Be(defaultUrl);
        mediaFile.Width.Should().Be(width);
        mediaFile.Height.Should().Be(height);
        mediaFile.Format.Should().Be(format);
        mediaFile.MimeType.Should().Be(mimeType);
        mediaFile.SizeBytes.Should().Be(sizeBytes);
        mediaFile.UploadedBy.Should().Be(uploadedBy);
    }

    [Fact]
    public void Create_WithOptionalParameters_ShouldSetAllProperties()
    {
        // Arrange
        var thumbHash = "base64EncodedThumbHash";
        var dominantColor = "#FF5733";
        var tenantId = "tenant-123";

        // Act
        var mediaFile = MediaFile.Create(
            "short123", "test-image_short123", "test.png", "avatars", "/uploads/avatars/test.webp",
            thumbHash, dominantColor,
            800, 600, "png", "image/png", 512000L,
            hasTransparency: true, variantsJson: "[]", srcsetsJson: "{}",
            "user-456", tenantId);

        // Assert
        mediaFile.ThumbHash.Should().Be(thumbHash);
        mediaFile.DominantColor.Should().Be(dominantColor);
        mediaFile.HasTransparency.Should().BeTrue();
        mediaFile.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithVariantsJson_ShouldPreserveJson()
    {
        // Arrange
        var variantsJson = "[{\"width\":400,\"url\":\"/small.webp\"},{\"width\":800,\"url\":\"/medium.webp\"}]";
        var srcsetsJson = "{\"webp\":\"small.webp 400w, medium.webp 800w\"}";

        // Act
        var mediaFile = MediaFile.Create(
            "short123", "test_short123", "test.jpg", "content", "/default.webp",
            null, null, 1200, 800, "jpeg", "image/jpeg", 1024000L,
            false, variantsJson, srcsetsJson, "user-123");

        // Assert
        mediaFile.VariantsJson.Should().Be(variantsJson);
        mediaFile.SrcsetsJson.Should().Be(srcsetsJson);
    }

    #endregion

    #region AspectRatio Tests

    [Fact]
    public void AspectRatio_LandscapeImage_ShouldBeGreaterThanOne()
    {
        // Arrange
        var mediaFile = MediaFile.Create(
            "short123", "landscape_short123", "landscape.jpg", "blog", "/default.webp",
            null, null, 1920, 1080, "jpeg", "image/jpeg", 1024000L,
            false, "[]", "{}", "user-123");

        // Act
        var aspectRatio = mediaFile.AspectRatio;

        // Assert
        aspectRatio.Should().BeApproximately(1.777, 0.001); // 16:9
    }

    [Fact]
    public void AspectRatio_PortraitImage_ShouldBeLessThanOne()
    {
        // Arrange
        var mediaFile = MediaFile.Create(
            "short123", "portrait_short123", "portrait.jpg", "blog", "/default.webp",
            null, null, 1080, 1920, "jpeg", "image/jpeg", 1024000L,
            false, "[]", "{}", "user-123");

        // Act
        var aspectRatio = mediaFile.AspectRatio;

        // Assert
        aspectRatio.Should().BeApproximately(0.5625, 0.001);
    }

    [Fact]
    public void AspectRatio_SquareImage_ShouldBeExactlyOne()
    {
        // Arrange
        var mediaFile = MediaFile.Create(
            "short123", "square_short123", "square.jpg", "avatars", "/default.webp",
            null, null, 500, 500, "jpeg", "image/jpeg", 512000L,
            false, "[]", "{}", "user-123");

        // Act
        var aspectRatio = mediaFile.AspectRatio;

        // Assert
        aspectRatio.Should().Be(1.0);
    }

    [Fact]
    public void AspectRatio_ZeroHeight_ShouldReturnZero()
    {
        // This tests edge case protection - normally height should never be 0
        // but the property should handle it gracefully
        var mediaFile = MediaFile.Create(
            "short123", "test_short123", "test.jpg", "blog", "/default.webp",
            null, null, 1920, 0, "jpeg", "image/jpeg", 0L,
            false, "[]", "{}", "user-123");

        // Act
        var aspectRatio = mediaFile.AspectRatio;

        // Assert
        aspectRatio.Should().Be(0);
    }

    #endregion

    #region UpdateAltText Tests

    [Fact]
    public void UpdateAltText_WithValue_ShouldSetAltText()
    {
        // Arrange
        var mediaFile = MediaFile.Create(
            "short123", "hero_short123", "hero.jpg", "blog", "/default.webp",
            null, null, 1920, 1080, "jpeg", "image/jpeg", 1024000L,
            false, "[]", "{}", "user-123");
        var altText = "A beautiful sunset over the mountains";

        // Act
        mediaFile.UpdateAltText(altText);

        // Assert
        mediaFile.AltText.Should().Be(altText);
    }

    [Fact]
    public void UpdateAltText_WithNull_ShouldClearAltText()
    {
        // Arrange
        var mediaFile = MediaFile.Create(
            "short123", "hero_short123", "hero.jpg", "blog", "/default.webp",
            null, null, 1920, 1080, "jpeg", "image/jpeg", 1024000L,
            false, "[]", "{}", "user-123");
        mediaFile.UpdateAltText("Initial alt text");

        // Act
        mediaFile.UpdateAltText(null);

        // Assert
        mediaFile.AltText.Should().BeNull();
    }

    [Fact]
    public void UpdateAltText_MultipleTimes_ShouldUseLatestValue()
    {
        // Arrange
        var mediaFile = MediaFile.Create(
            "short123", "hero_short123", "hero.jpg", "blog", "/default.webp",
            null, null, 1920, 1080, "jpeg", "image/jpeg", 1024000L,
            false, "[]", "{}", "user-123");

        // Act
        mediaFile.UpdateAltText("First");
        mediaFile.UpdateAltText("Second");
        mediaFile.UpdateAltText("Third");

        // Assert
        mediaFile.AltText.Should().Be("Third");
    }

    #endregion

    #region Transparency Tests

    [Fact]
    public void Create_PngWithTransparency_ShouldHaveTransparencyTrue()
    {
        // Act
        var mediaFile = MediaFile.Create(
            "short123", "logo_short123", "logo.png", "branding", "/default.webp",
            null, null, 512, 512, "png", "image/png", 256000L,
            hasTransparency: true, variantsJson: "[]", srcsetsJson: "{}",
            "user-123");

        // Assert
        mediaFile.HasTransparency.Should().BeTrue();
        mediaFile.Format.Should().Be("png");
    }

    [Fact]
    public void Create_JpegWithoutTransparency_ShouldHaveTransparencyFalse()
    {
        // Act
        var mediaFile = MediaFile.Create(
            "short123", "photo_short123", "photo.jpg", "gallery", "/default.webp",
            null, null, 3000, 2000, "jpeg", "image/jpeg", 2048000L,
            hasTransparency: false, variantsJson: "[]", srcsetsJson: "{}",
            "user-123");

        // Assert
        mediaFile.HasTransparency.Should().BeFalse();
    }

    #endregion

    #region Folder Tests

    [Theory]
    [InlineData("blog")]
    [InlineData("avatars")]
    [InlineData("content")]
    [InlineData("media/uploads")]
    public void Create_WithVariousFolders_ShouldPreserveFolder(string folder)
    {
        // Act
        var mediaFile = MediaFile.Create(
            "short123", "test_short123", "test.jpg", folder, "/default.webp",
            null, null, 800, 600, "jpeg", "image/jpeg", 512000L,
            false, "[]", "{}", "user-123");

        // Assert
        mediaFile.Folder.Should().Be(folder);
    }

    #endregion

    #region Tenant Tests

    [Fact]
    public void Create_WithTenantId_ShouldBeAssociatedWithTenant()
    {
        // Arrange
        var tenantId = "tenant-abc";

        // Act
        var mediaFile = MediaFile.Create(
            "short123", "test_short123", "test.jpg", "blog", "/default.webp",
            null, null, 800, 600, "jpeg", "image/jpeg", 512000L,
            false, "[]", "{}", "user-123", tenantId);

        // Assert
        mediaFile.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithoutTenantId_ShouldHaveNullTenant()
    {
        // Act
        var mediaFile = MediaFile.Create(
            "short123", "test_short123", "test.jpg", "blog", "/default.webp",
            null, null, 800, 600, "jpeg", "image/jpeg", 512000L,
            false, "[]", "{}", "user-123");

        // Assert
        mediaFile.TenantId.Should().BeNull();
    }

    #endregion
}
