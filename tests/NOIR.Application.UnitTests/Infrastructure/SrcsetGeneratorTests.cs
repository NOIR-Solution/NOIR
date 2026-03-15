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
        result.ShouldContain("/images/test-thumb.jpg 320w");
        result.ShouldContain("/images/test-medium.jpg 640w");
        result.ShouldContain("/images/test-large.jpg 1280w");
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
        result.ShouldContain("/images/test-thumb.webp 320w");
        result.ShouldContain("/images/test-medium.webp 640w");
        result.ShouldNotContain(".jpg");
    }

    [Fact]
    public void GenerateSrcset_WithEmptyVariants_ShouldReturnEmptyString()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>();

        // Act
        var result = SrcsetGenerator.GenerateSrcset(variants);

        // Assert
        result.ShouldBeEmpty();
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
        result.ShouldContain("/images/test-thumb.jpg 320w");
        result.ShouldContain("/images/test-large.jpg 1280w");
        result.ShouldNotContain("640w");
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
        result.ShouldContain("/images/test-thumb.jpg 320w");
        result.ShouldNotContain("640w");
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
        parts[0].ShouldContain("320w");
        parts[1].ShouldContain("640w");
        parts[2].ShouldContain("1280w");
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
        result.ShouldBe("/images/test.jpg 640w");
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
        result.ShouldContain("/images/test-thumb.avif 320w");
        result.ShouldNotContain(".jpg");
    }

    #endregion

    #region GenerateSizes Tests

    [Fact]
    public void GenerateSizes_WithDefaultSize_ShouldReturn100vw()
    {
        // Act
        var result = SrcsetGenerator.GenerateSizes();

        // Assert
        result.ShouldBe("100vw");
    }

    [Fact]
    public void GenerateSizes_WithCustomDefaultSize_ShouldReturnCustomSize()
    {
        // Act
        var result = SrcsetGenerator.GenerateSizes("50vw");

        // Assert
        result.ShouldBe("50vw");
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
        result.ShouldContain("(max-width: 640px) 100vw");
        result.ShouldContain("(max-width: 1024px) 50vw");
        result.ShouldEndWith("33vw");
    }

    [Fact]
    public void GenerateSizes_WithNullBreakpoints_ShouldReturnDefaultSize()
    {
        // Act
        var result = SrcsetGenerator.GenerateSizes("100vw", null);

        // Assert
        result.ShouldBe("100vw");
    }

    [Fact]
    public void GenerateSizes_WithEmptyBreakpoints_ShouldReturnDefaultSize()
    {
        // Arrange
        var breakpoints = new List<(int maxWidth, string size)>();

        // Act
        var result = SrcsetGenerator.GenerateSizes("100vw", breakpoints);

        // Assert
        result.ShouldBe("100vw");
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
        parts[0].ShouldContain("480px");
        parts[1].ShouldContain("768px");
        parts[2].ShouldContain("1024px");
        parts[3].ShouldBe("33vw");
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
        result.ShouldContain("<picture>");
        result.ShouldContain("</picture>");
        result.ShouldContain("type=\"image/avif\"");
        result.ShouldContain("type=\"image/webp\"");
        result.ShouldContain("<img");
        result.ShouldContain("alt=\"Test image\"");
        result.ShouldContain("loading=\"lazy\"");
        result.ShouldContain("decoding=\"async\"");
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
        result.ShouldContain("class=\"my-image-class\"");
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
        result.ShouldContain("sizes=\"50vw\"");
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
        result.ShouldContain("loading=\"eager\"");
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
        result.ShouldContain("&lt;script&gt;");
        result.ShouldNotContain("<script>");
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
        result.ShouldContain("&quot;quoted&quot;");
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
        result.ShouldContain("<img");
        result.ShouldContain("alt=\"Test image\"");
        result.ShouldContain("loading=\"lazy\"");
        result.ShouldContain("decoding=\"async\"");
        result.ShouldContain("srcset=\"");
        result.ShouldContain("sizes=\"100vw\"");
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
        result.ShouldContain("/images/test.webp");
        result.ShouldNotContain("/images/test.jpg");
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
        result.ShouldContain("class=\"responsive-img\"");
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
        result.ShouldContain("sizes=\"(max-width: 640px) 100vw, 50vw\"");
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
        result.ShouldContain("src=\"/images/test-large.jpg\"");
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
        result.ShouldContain("background-image:");
        result.ShouldContain("image-set(");
        result.ShouldContain("type(\"image/avif\")");
        result.ShouldContain("type(\"image/webp\")");
        result.ShouldContain("type(\"image/jpeg\")");
    }

    [Fact]
    public void GenerateBackgroundImageCss_WithEmptyVariants_ShouldReturnEmpty()
    {
        // Arrange
        var variants = new List<ImageVariantInfo>();

        // Act
        var result = SrcsetGenerator.GenerateBackgroundImageCss(variants, ImageVariant.Medium);

        // Assert
        result.ShouldBeEmpty();
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
        result.ShouldBeEmpty();
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
        result.ShouldContain("url(\"/images/bg.jpg\")");
        result.ShouldContain("type(\"image/jpeg\")");
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
        result.ShouldStartWith("background-image: url(\"/images/bg.jpg\");");
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
        result.ShouldContain("/images/test%20image.jpg");
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
        result.ShouldContain("https://cdn.example.com/images/test.jpg 320w");
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
        result.ShouldNotContain("type=\"image/avif\"");
        result.ShouldContain("type=\"image/webp\"");
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
        result.ShouldNotContain("type=\"image/webp\"");
        result.ShouldNotContain("type=\"image/avif\"");
    }

    #endregion
}
