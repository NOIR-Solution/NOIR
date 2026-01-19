namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for SlugGenerator.
/// Tests SEO-friendly slug generation, unicode handling, and short ID extraction.
/// </summary>
public class SlugGeneratorTests
{
    #region Generate Tests

    [Theory]
    [InlineData("Hello World.jpg", "hello-world")]
    [InlineData("My Image File.png", "my-image-file")]
    [InlineData("test.webp", "test")]
    public void Generate_WithSimpleFileName_ShouldReturnSlug(string fileName, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("héllo wörld.jpg", "hello-world")]
    [InlineData("café résumé.png", "cafe-resume")]
    [InlineData("naïve façade.webp", "naive-facade")]
    public void Generate_WithUnicodeCharacters_ShouldNormalize(string fileName, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello_world.jpg", "hello-world")]
    [InlineData("my__image.png", "my-image")]
    [InlineData("test___file.webp", "test-file")]
    public void Generate_WithUnderscores_ShouldConvertToHyphens(string fileName, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello---world.jpg", "hello-world")]
    [InlineData("my - - image.png", "my-image")]
    [InlineData("test   file.webp", "test-file")]
    public void Generate_WithMultipleHyphensOrSpaces_ShouldCollapse(string fileName, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello@World#Test$.jpg", "helloworldtest")]
    [InlineData("image!@#$%^&*().png", "image")]
    [InlineData("file(1)[2]{3}.webp", "file123")]
    public void Generate_WithSpecialCharacters_ShouldRemove(string fileName, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("-hello-world-.jpg", "hello-world")]
    [InlineData("---test---.png", "test")]
    public void Generate_WithLeadingTrailingHyphens_ShouldTrim(string fileName, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Generate_WithEmptyOrWhitespace_ShouldReturnRandomSlug(string? fileName)
    {
        // Act
        var result = SlugGenerator.Generate(fileName!);

        // Assert
        result.Should().StartWith("image_");
        result.Should().HaveLength(14); // "image_" + 8 char shortId
    }

    [Fact]
    public void Generate_WithVeryLongFileName_ShouldTruncate()
    {
        // Arrange
        var longName = new string('a', 200) + ".jpg";

        // Act
        var result = SlugGenerator.Generate(longName, 50);

        // Assert
        result.Length.Should().BeLessThanOrEqualTo(50);
    }

    [Theory]
    [InlineData("ăắằẳẵặ.jpg")] // Vietnamese
    [InlineData("ñoño.png")] // Spanish
    [InlineData("größe.webp")] // German
    public void Generate_WithVietnameseAndOtherUnicode_ShouldNormalize(string fileName)
    {
        // Act
        var result = SlugGenerator.Generate(fileName);

        // Assert
        result.Should().MatchRegex("^[a-z0-9-]+$");
        result.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("UPPERCASE.jpg", "uppercase")]
    [InlineData("MixedCase.PNG", "mixedcase")]
    public void Generate_WithMixedCase_ShouldLowercase(string fileName, string expected)
    {
        // Act
        var result = SlugGenerator.Generate(fileName);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("file.tar.gz")]
    [InlineData("archive.backup.zip")]
    public void Generate_WithMultipleExtensions_ShouldRemoveLastExtension(string fileName)
    {
        // Act
        var result = SlugGenerator.Generate(fileName);

        // Assert
        result.Should().MatchRegex("^[a-z0-9-]+$");
        result.Should().NotContain(".");
    }

    [Fact]
    public void Generate_WithOnlySpecialCharacters_ShouldReturnRandomSlug()
    {
        // Arrange - File name with only special chars that get stripped
        var fileName = "@#$%^&*().jpg";

        // Act
        var result = SlugGenerator.Generate(fileName);

        // Assert
        result.Should().StartWith("image_");
        result.Should().MatchRegex("^image_[a-f0-9]{8}$");
    }

    #endregion

    #region GenerateUnique Tests

    [Fact]
    public void GenerateUnique_WithFileName_ShouldAppendShortId()
    {
        // Act
        var result = SlugGenerator.GenerateUnique("hello-world.jpg");

        // Assert
        result.Should().StartWith("hello-world_");
        result.Should().MatchRegex("^hello-world_[a-f0-9]{8}$");
    }

    [Fact]
    public void GenerateUnique_WithCustomSuffix_ShouldUseSuffix()
    {
        // Act
        var result = SlugGenerator.GenerateUnique("test.jpg", "custom123");

        // Assert
        result.Should().Be("test_custom123");
    }

    [Fact]
    public void GenerateUnique_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Act
        var results = Enumerable.Range(0, 100)
            .Select(_ => SlugGenerator.GenerateUnique("test.jpg"))
            .ToList();

        // Assert
        results.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateUnique_LargeScale_ShouldHaveNoCollisions()
    {
        // Act - Generate 1,000 slugs to test for collisions
        var results = Enumerable.Range(0, 1000)
            .Select(_ => SlugGenerator.GenerateUnique("test.jpg"))
            .ToList();

        // Assert
        results.Should().OnlyHaveUniqueItems();
        results.All(r => r.StartsWith("test_")).Should().BeTrue();
    }

    [Fact]
    public void GenerateUnique_WithVeryLongFileName_ShouldTruncateButKeepSuffix()
    {
        // Arrange
        var longName = new string('a', 200) + ".jpg";

        // Act
        var result = SlugGenerator.GenerateUnique(longName);

        // Assert
        result.Should().Contain("_"); // Suffix separator preserved
        result.Split('_').Last().Length.Should().Be(8); // Short ID preserved
    }

    #endregion

    #region ExtractShortId Tests

    [Theory]
    [InlineData("hero-banner_a1b2c3d4", "a1b2c3d4")]
    [InlineData("my-image_12345678", "12345678")]
    [InlineData("test_abcd", "abcd")]
    public void ExtractShortId_WithValidSlug_ShouldReturnShortId(string slug, string expected)
    {
        // Act
        var result = SlugGenerator.ExtractShortId(slug);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hero-banner")]
    [InlineData("no-underscore-here")]
    [InlineData("")]
    [InlineData(null)]
    public void ExtractShortId_WithNoUnderscore_ShouldReturnNull(string? slug)
    {
        // Act
        var result = SlugGenerator.ExtractShortId(slug!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractShortId_WithTrailingUnderscore_ShouldReturnNull()
    {
        // Act
        var result = SlugGenerator.ExtractShortId("test_");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractShortId_WithMultipleUnderscores_ShouldReturnLastPart()
    {
        // Act
        var result = SlugGenerator.ExtractShortId("my_complex_name_a1b2c3d4");

        // Assert
        result.Should().Be("a1b2c3d4");
    }

    #endregion

    #region GenerateShortId Tests

    [Fact]
    public void GenerateShortId_ShouldReturn8Characters()
    {
        // Act
        var result = SlugGenerator.GenerateShortId();

        // Assert
        result.Should().HaveLength(8);
    }

    [Fact]
    public void GenerateShortId_ShouldBeAlphanumeric()
    {
        // Act
        var result = SlugGenerator.GenerateShortId();

        // Assert
        result.Should().MatchRegex("^[a-f0-9]+$");
    }

    [Fact]
    public void GenerateShortId_MultipleCalls_ShouldBeUnique()
    {
        // Act
        var results = Enumerable.Range(0, 1000)
            .Select(_ => SlugGenerator.GenerateShortId())
            .ToList();

        // Assert
        results.Should().OnlyHaveUniqueItems();
    }

    #endregion
}
