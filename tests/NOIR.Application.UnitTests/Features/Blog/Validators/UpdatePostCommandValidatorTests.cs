using NOIR.Application.Features.Blog.Commands.UpdatePost;

namespace NOIR.Application.UnitTests.Features.Blog.Validators;

/// <summary>
/// Unit tests for UpdatePostCommandValidator.
/// Tests all validation rules for updating a blog post.
/// </summary>
public class UpdatePostCommandValidatorTests
{
    private readonly UpdatePostCommandValidator _validator = new();

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.Empty,
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
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Post ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Title Validation

    [Fact]
    public async Task Validate_WhenTitleIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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

    #endregion

    #region Slug Validation

    [Fact]
    public async Task Validate_WhenSlugIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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

    [Theory]
    [InlineData("Invalid Slug")]
    [InlineData("INVALID-SLUG")]
    [InlineData("-invalid-slug")]
    public async Task Validate_WhenSlugHasInvalidFormat_ShouldHaveError(string slug)
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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

    #endregion

    #region Excerpt Validation

    [Fact]
    public async Task Validate_WhenExcerptExceeds1000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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

    #endregion

    #region MetaTitle Validation

    [Fact]
    public async Task Validate_WhenMetaTitleExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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

    #endregion

    #region MetaDescription Validation

    [Fact]
    public async Task Validate_WhenMetaDescriptionExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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

    #endregion

    #region FeaturedImageUrl Validation

    [Fact]
    public async Task Validate_WhenFeaturedImageUrlExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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

    #endregion

    #region FeaturedImageAlt Validation

    [Fact]
    public async Task Validate_WhenFeaturedImageAltExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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

    #endregion

    #region CanonicalUrl Validation

    [Fact]
    public async Task Validate_WhenCanonicalUrlExceeds2000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
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

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdatePostCommand(
            Id: Guid.NewGuid(),
            Title: "My Updated Blog Post",
            Slug: "my-updated-blog-post",
            Excerpt: "This is a short excerpt.",
            ContentJson: "{}",
            ContentHtml: "<p>Updated Content</p>",
            FeaturedImageId: Guid.NewGuid(),
            FeaturedImageUrl: "https://example.com/image.jpg",
            FeaturedImageAlt: "Blog post image",
            MetaTitle: "My Updated Blog Post | My Site",
            MetaDescription: "Read about my updated blog post.",
            CanonicalUrl: "https://example.com/blog/my-updated-blog-post",
            AllowIndexing: true,
            CategoryId: Guid.NewGuid(),
            TagIds: [Guid.NewGuid()]);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
