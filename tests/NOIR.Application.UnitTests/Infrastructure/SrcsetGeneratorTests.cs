namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for SrcsetGenerator.
/// Tests srcset generation, sizes attribute generation, and HTML markup generation for responsive images.
/// </summary>
public class SrcsetGeneratorTests
{
    #region GenerateSrcset Tests

    [Fact]
    public void GenerateSrcset_WithMultipleVariants_ShouldReturnSrcsetString()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test-thumb.jpg", Width = 320 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "/images/test-medium.jpg", Width = 640 },
            new() { Variant = ImageVariant.Large, Format = OutputFormat.Jpeg, Url = "/images/test-large.jpg", Width = 1280 }
        };

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants);

        // Assert
        result.Should().Contain("/images/test-thumb.jpg 320w");
        result.Should().Contain("/images/test-medium.jpg 640w");
        result.Should().Contain("/images/test-large.jpg 1280w");
    }

    [Fact]
    public void GenerateSrcset_WithFormatFilter_ShouldFilterByFormat()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test-thumb.jpg", Width = 320 },
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.WebP, Url = "/images/test-thumb.webp", Width = 320 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "/images/test-medium.jpg", Width = 640 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.WebP, Url = "/images/test-medium.webp", Width = 640 }
        };

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants, OutputFormat.WebP);

        // Assert
        result.Should().Contain("/images/test-thumb.webp 320w");
        result.Should().Contain("/images/test-medium.webp 640w");
        result.Should().NotContain(".jpg");
    }

    [Fact]
    public void GenerateSrcset_WithEmptyVariants_ShouldReturnEmptyString()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>();

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSrcset_WithNullUrls_ShouldSkipThem()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test-thumb.jpg", Width = 320 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = null, Width = 640 },
            new() { Variant = ImageVariant.Large, Format = OutputFormat.Jpeg, Url = "/images/test-large.jpg", Width = 1280 }
        };

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants);

        // Assert
        result.Should().Contain("/images/test-thumb.jpg 320w");
        result.Should().Contain("/images/test-large.jpg 1280w");
        result.Should().NotContain("640w");
    }

    [Fact]
    public void GenerateSrcset_WithEmptyUrls_ShouldSkipThem()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test-thumb.jpg", Width = 320 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "", Width = 640 }
        };

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants);

        // Assert
        result.Should().Contain("/images/test-thumb.jpg 320w");
        result.Should().NotContain("640w");
    }

    [Fact]
    public void GenerateSrcset_ShouldOrderByWidth()
    {
        // Arrange - Out of order
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Large, Format = OutputFormat.Jpeg, Url = "/images/test-large.jpg", Width = 1280 },
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test-thumb.jpg", Width = 320 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "/images/test-medium.jpg", Width = 640 }
        };

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants);

        // Assert
        var parts = result.Split(", ");
        parts[0].Should().Contain("320w");
        parts[1].Should().Contain("640w");
        parts[2].Should().Contain("1280w");
    }

    [Fact]
    public void GenerateSrcset_WithSingleVariant_ShouldReturnSingleEntry()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 640 }
        };

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants);

        // Assert
        result.Should().Be("/images/test.jpg 640w");
    }

    [Fact]
    public void GenerateSrcset_WithAvifFormat_ShouldFilterCorrectly()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Avif, Url = "/images/test-thumb.avif", Width = 320 },
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test-thumb.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants, OutputFormat.Avif);

        // Assert
        result.Should().Contain("/images/test-thumb.avif 320w");
        result.Should().NotContain(".jpg");
    }

    #endregion

    #region GenerateSizes Tests

    [Fact]
    public void GenerateSizes_WithDefaultSize_ShouldReturn100vw()
    {
        // Act
        var result = SrcsetGenerator.GenerateSizes();

        // Assert
        result.Should().Be("100vw");
    }

    [Fact]
    public void GenerateSizes_WithCustomDefaultSize_ShouldReturnCustomSize()
    {
        // Act
        var result = SrcsetGenerator.GenerateSizes("50vw");

        // Assert
        result.Should().Be("50vw");
    }

    [Fact]
    public void GenerateSizes_WithBreakpoints_ShouldReturnMediaQueries()
    {
        // Arrange
        var breakpoints = new List<(int maxWidth, string size)>
        {
            (640, "100vw"),
            (1024, "50vw")
        };

        // Act
        var result = SrcsetGenerator.GenerateSizes("33vw", breakpoints);

        // Assert
        result.Should().Contain("(max-width: 640px) 100vw");
        result.Should().Contain("(max-width: 1024px) 50vw");
        result.Should().EndWith("33vw");
    }

    [Fact]
    public void GenerateSizes_WithNullBreakpoints_ShouldReturnDefaultSize()
    {
        // Act
        var result = SrcsetGenerator.GenerateSizes("100vw", null);

        // Assert
        result.Should().Be("100vw");
    }

    [Fact]
    public void GenerateSizes_WithEmptyBreakpoints_ShouldReturnDefaultSize()
    {
        // Arrange
        var breakpoints = new List<(int maxWidth, string size)>();

        // Act
        var result = SrcsetGenerator.GenerateSizes("100vw", breakpoints);

        // Assert
        result.Should().Be("100vw");
    }

    [Fact]
    public void GenerateSizes_WithBreakpoints_ShouldOrderByWidth()
    {
        // Arrange - Out of order
        var breakpoints = new List<(int maxWidth, string size)>
        {
            (1024, "50vw"),
            (480, "100vw"),
            (768, "75vw")
        };

        // Act
        var result = SrcsetGenerator.GenerateSizes("33vw", breakpoints);

        // Assert
        var parts = result.Split(", ");
        parts[0].Should().Contain("480px");
        parts[1].Should().Contain("768px");
        parts[2].Should().Contain("1024px");
        parts[3].Should().Be("33vw");
    }

    #endregion

    #region GeneratePictureElement Tests

    [Fact]
    public void GeneratePictureElement_WithVariants_ShouldGenerateHtml()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Avif, Url = "/images/test-thumb.avif", Width = 320 },
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.WebP, Url = "/images/test-thumb.webp", Width = 320 },
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test-thumb.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GeneratePictureElement(variants, "Test image");

        // Assert
        result.Should().Contain("<picture>");
        result.Should().Contain("</picture>");
        result.Should().Contain("type=\"image/avif\"");
        result.Should().Contain("type=\"image/webp\"");
        result.Should().Contain("<img");
        result.Should().Contain("alt=\"Test image\"");
        result.Should().Contain("loading=\"lazy\"");
        result.Should().Contain("decoding=\"async\"");
    }

    [Fact]
    public void GeneratePictureElement_WithClassName_ShouldIncludeClass()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GeneratePictureElement(variants, "Test", className: "my-image-class");

        // Assert
        result.Should().Contain("class=\"my-image-class\"");
    }

    [Fact]
    public void GeneratePictureElement_WithCustomSizes_ShouldIncludeSizes()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GeneratePictureElement(variants, "Test", sizes: "50vw");

        // Assert
        result.Should().Contain("sizes=\"50vw\"");
    }

    [Fact]
    public void GeneratePictureElement_WithEagerLoading_ShouldSetLoading()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GeneratePictureElement(variants, "Test", loading: "eager");

        // Assert
        result.Should().Contain("loading=\"eager\"");
    }

    [Fact]
    public void GeneratePictureElement_ShouldEscapeHtmlInAlt()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GeneratePictureElement(variants, "Test <script>alert('xss')</script>");

        // Assert
        result.Should().Contain("&lt;script&gt;");
        result.Should().NotContain("<script>");
    }

    [Fact]
    public void GeneratePictureElement_WithQuotesInAlt_ShouldEscape()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GeneratePictureElement(variants, "Test \"quoted\" text");

        // Assert
        result.Should().Contain("&quot;quoted&quot;");
    }

    #endregion

    #region GenerateImgTag Tests

    [Fact]
    public void GenerateImgTag_WithVariants_ShouldGenerateHtml()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test-thumb.jpg", Width = 320 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "/images/test-medium.jpg", Width = 640 }
        };

        // Act
        var result = SrcsetGenerator.GenerateImgTag(variants, "Test image");

        // Assert
        result.Should().Contain("<img");
        result.Should().Contain("alt=\"Test image\"");
        result.Should().Contain("loading=\"lazy\"");
        result.Should().Contain("decoding=\"async\"");
        result.Should().Contain("srcset=\"");
        result.Should().Contain("sizes=\"100vw\"");
    }

    [Fact]
    public void GenerateImgTag_WithSpecificFormat_ShouldUseFormat()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.WebP, Url = "/images/test.webp", Width = 320 },
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GenerateImgTag(variants, "Test", format: OutputFormat.WebP);

        // Assert
        result.Should().Contain("/images/test.webp");
        result.Should().NotContain("/images/test.jpg");
    }

    [Fact]
    public void GenerateImgTag_WithClassName_ShouldIncludeClass()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GenerateImgTag(variants, "Test", className: "responsive-img");

        // Assert
        result.Should().Contain("class=\"responsive-img\"");
    }

    [Fact]
    public void GenerateImgTag_WithCustomSizes_ShouldIncludeSizes()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GenerateImgTag(variants, "Test", sizes: "(max-width: 640px) 100vw, 50vw");

        // Assert
        result.Should().Contain("sizes=\"(max-width: 640px) 100vw, 50vw\"");
    }

    [Fact]
    public void GenerateImgTag_UsesLargestVariantForSrc()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test-thumb.jpg", Width = 320 },
            new() { Variant = ImageVariant.Large, Format = OutputFormat.Jpeg, Url = "/images/test-large.jpg", Width = 1280 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "/images/test-medium.jpg", Width = 640 }
        };

        // Act
        var result = SrcsetGenerator.GenerateImgTag(variants, "Test");

        // Assert
        result.Should().Contain("src=\"/images/test-large.jpg\"");
    }

    #endregion

    #region GenerateBackgroundImageCss Tests

    [Fact]
    public void GenerateBackgroundImageCss_WithVariants_ShouldGenerateCss()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Avif, Url = "/images/bg.avif", Width = 640 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.WebP, Url = "/images/bg.webp", Width = 640 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "/images/bg.jpg", Width = 640 }
        };

        // Act
        var result = SrcsetGenerator.GenerateBackgroundImageCss(variants, ImageVariant.Medium);

        // Assert
        result.Should().Contain("background-image:");
        result.Should().Contain("image-set(");
        result.Should().Contain("type(\"image/avif\")");
        result.Should().Contain("type(\"image/webp\")");
        result.Should().Contain("type(\"image/jpeg\")");
    }

    [Fact]
    public void GenerateBackgroundImageCss_WithEmptyVariants_ShouldReturnEmpty()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>();

        // Act
        var result = SrcsetGenerator.GenerateBackgroundImageCss(variants, ImageVariant.Medium);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateBackgroundImageCss_WithNoMatchingVariant_ShouldReturnEmpty()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/small.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GenerateBackgroundImageCss(variants, ImageVariant.Large);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateBackgroundImageCss_WithOnlyJpeg_ShouldIncludeFallback()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "/images/bg.jpg", Width = 640 }
        };

        // Act
        var result = SrcsetGenerator.GenerateBackgroundImageCss(variants, ImageVariant.Medium);

        // Assert
        result.Should().Contain("url(\"/images/bg.jpg\")");
        result.Should().Contain("type(\"image/jpeg\")");
    }

    [Fact]
    public void GenerateBackgroundImageCss_FallbackUsesJpegFirst()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.WebP, Url = "/images/bg.webp", Width = 640 },
            new() { Variant = ImageVariant.Medium, Format = OutputFormat.Jpeg, Url = "/images/bg.jpg", Width = 640 }
        };

        // Act
        var result = SrcsetGenerator.GenerateBackgroundImageCss(variants, ImageVariant.Medium);

        // Assert
        // Fallback should use JPEG (most compatible)
        result.Should().StartWith("background-image: url(\"/images/bg.jpg\");");
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void GenerateSrcset_WithSpecialCharactersInUrl_ShouldPreserve()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test%20image.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants);

        // Assert
        result.Should().Contain("/images/test%20image.jpg");
    }

    [Fact]
    public void GenerateSrcset_WithAbsoluteUrls_ShouldPreserve()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "https://cdn.example.com/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants);

        // Assert
        result.Should().Contain("https://cdn.example.com/images/test.jpg 320w");
    }

    [Fact]
    public void GeneratePictureElement_WithNoAvifVariants_ShouldOmitAvifSource()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.WebP, Url = "/images/test.webp", Width = 320 },
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GeneratePictureElement(variants, "Test");

        // Assert
        result.Should().NotContain("type=\"image/avif\"");
        result.Should().Contain("type=\"image/webp\"");
    }

    [Fact]
    public void GeneratePictureElement_WithNoWebPVariants_ShouldOmitWebPSource()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>
        {
            new() { Variant = ImageVariant.Thumb, Format = OutputFormat.Jpeg, Url = "/images/test.jpg", Width = 320 }
        };

        // Act
        var result = SrcsetGenerator.GeneratePictureElement(variants, "Test");

        // Assert
        result.Should().NotContain("type=\"image/webp\"");
        result.Should().NotContain("type=\"image/avif\"");
    }

    #endregion
}
