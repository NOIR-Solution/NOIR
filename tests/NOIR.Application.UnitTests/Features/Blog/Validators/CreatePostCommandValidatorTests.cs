using NOIR.Application.Features.Blog.Commands.CreatePost;

namespace NOIR.Application.UnitTests.Features.Blog.Validators;

/// <summary>
/// Unit tests for CreatePostCommandValidator.
/// Tests all validation rules for creating a blog post.
/// </summary>
public class CreatePostCommandValidatorTests
{
    private readonly CreatePostCommandValidator _validator = new();

    #region Title Validation

    [Fact]
    public async Task Validate_WhenTitleIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required.");
    }

    [Fact]
    public async Task Validate_WhenTitleExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: new string('A', 501),
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenTitleIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: new string('A', 500),
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    #endregion

    #region Slug Validation

    [Fact]
    public async Task Validate_WhenSlugIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug is required.");
    }

    [Fact]
    public async Task Validate_WhenSlugExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: new string('a', 501),
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug cannot exceed 500 characters.");
    }

    [Theory]
    [InlineData("Invalid Slug")]
    [InlineData("InvalidSlug")]
    [InlineData("INVALID-SLUG")]
    [InlineData("invalid_slug")]
    [InlineData("-invalid-slug")]
    [InlineData("invalid-slug-")]
    [InlineData("invalid--slug")]
    public async Task Validate_WhenSlugHasInvalidFormat_ShouldHaveError(string slug)
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: slug,
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");
    }

    [Theory]
    [InlineData("valid-slug")]
    [InlineData("valid-slug-123")]
    [InlineData("123-valid-slug")]
    [InlineData("a")]
    [InlineData("valid123slug")]
    public async Task Validate_WhenSlugHasValidFormat_ShouldNotHaveError(string slug)
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: slug,
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Slug);
    }

    #endregion

    #region Excerpt Validation

    [Fact]
    public async Task Validate_WhenExcerptExceeds1000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: new string('A', 1001),
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Excerpt)
            .WithErrorMessage("Excerpt cannot exceed 1000 characters.");
    }

    [Fact]
    public async Task Validate_WhenExcerptIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Excerpt);
    }

    #endregion

    #region MetaTitle Validation

    [Fact]
    public async Task Validate_WhenMetaTitleExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: new string('A', 201),
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaTitle)
            .WithErrorMessage("Meta title cannot exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenMetaTitleIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MetaTitle);
    }

    #endregion

    #region MetaDescription Validation

    [Fact]
    public async Task Validate_WhenMetaDescriptionExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: new string('A', 501),
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaDescription)
            .WithErrorMessage("Meta description cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenMetaDescriptionIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MetaDescription);
    }

    #endregion

    #region FeaturedImageUrl Validation

    [Fact]
    public async Task Validate_WhenFeaturedImageUrlExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: new string('a', 2001),
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FeaturedImageUrl)
            .WithErrorMessage("Featured image URL cannot exceed 2000 characters.");
    }

    [Fact]
    public async Task Validate_WhenFeaturedImageUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FeaturedImageUrl);
    }

    #endregion

    #region FeaturedImageAlt Validation

    [Fact]
    public async Task Validate_WhenFeaturedImageAltExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: new string('A', 501),
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FeaturedImageAlt)
            .WithErrorMessage("Featured image alt text cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenFeaturedImageAltIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FeaturedImageAlt);
    }

    #endregion

    #region CanonicalUrl Validation

    [Fact]
    public async Task Validate_WhenCanonicalUrlExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: new string('a', 2001),
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CanonicalUrl)
            .WithErrorMessage("Canonical URL cannot exceed 2000 characters.");
    }

    [Fact]
    public async Task Validate_WhenCanonicalUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "Valid Title",
            Slug: "valid-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CanonicalUrl);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "My Blog Post",
            Slug: "my-blog-post",
            Excerpt: "This is a short excerpt.",
            ContentJson: "{}",
            ContentHtml: "<p>Content</p>",
            FeaturedImageId: Guid.NewGuid(),
            FeaturedImageUrl: "https://example.com/image.jpg",
            FeaturedImageAlt: "Blog post image",
            MetaTitle: "My Blog Post | My Site",
            MetaDescription: "Read about my blog post.",
            CanonicalUrl: "https://example.com/blog/my-blog-post",
            AllowIndexing: true,
            CategoryId: Guid.NewGuid(),
            TagIds: [Guid.NewGuid()]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreatePostCommand(
            Title: "My Blog Post",
            Slug: "my-blog-post",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
