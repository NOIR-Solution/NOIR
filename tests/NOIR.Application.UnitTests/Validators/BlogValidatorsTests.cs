namespace NOIR.Application.UnitTests.Validators;

using NOIR.Application.Features.Blog.Commands.CreatePost;
using NOIR.Application.Features.Blog.Commands.UpdatePost;
using NOIR.Application.Features.Blog.Commands.DeletePost;
using NOIR.Application.Features.Blog.Commands.PublishPost;
using NOIR.Application.Features.Blog.Commands.CreateCategory;
using NOIR.Application.Features.Blog.Commands.UpdateCategory;
using NOIR.Application.Features.Blog.Commands.DeleteCategory;
using NOIR.Application.Features.Blog.Commands.CreateTag;
using NOIR.Application.Features.Blog.Commands.UpdateTag;
using NOIR.Application.Features.Blog.Commands.DeleteTag;

/// <summary>
/// Unit tests for blog command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class BlogValidatorsTests
{
    #region CreatePostCommandValidator Tests

    public class CreatePostCommandValidatorTests
    {
        private readonly CreatePostCommandValidator _validator;

        public CreatePostCommandValidatorTests()
        {
            _validator = new CreatePostCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new CreatePostCommand(
                Title: "Test Post Title",
                Slug: "test-post-title",
                Excerpt: "Test excerpt",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullTitle_ShouldFail(string? title)
        {
            // Arrange
            var command = new CreatePostCommand(
                Title: title!,
                Slug: "test-slug",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required.");
        }

        [Fact]
        public void Validate_TitleTooLong_ShouldFail()
        {
            // Arrange
            var longTitle = new string('a', 501);
            var command = new CreatePostCommand(
                Title: longTitle,
                Slug: "test-slug",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title cannot exceed 500 characters.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullSlug_ShouldFail(string? slug)
        {
            // Arrange
            var command = new CreatePostCommand(
                Title: "Test Title",
                Slug: slug!,
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug is required.");
        }

        [Fact]
        public void Validate_SlugTooLong_ShouldFail()
        {
            // Arrange
            var longSlug = new string('a', 501);
            var command = new CreatePostCommand(
                Title: "Test Title",
                Slug: longSlug,
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug cannot exceed 500 characters.");
        }

        [Theory]
        [InlineData("Test Slug")] // Contains space
        [InlineData("Test_Slug")] // Contains underscore
        [InlineData("UPPERCASE")] // Uppercase letters
        [InlineData("test--slug")] // Double hyphen
        [InlineData("-test-slug")] // Starts with hyphen
        [InlineData("test-slug-")] // Ends with hyphen
        public void Validate_InvalidSlugFormat_ShouldFail(string slug)
        {
            // Arrange
            var command = new CreatePostCommand(
                Title: "Test Title",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");
        }

        [Theory]
        [InlineData("test")]
        [InlineData("test-slug")]
        [InlineData("test-slug-123")]
        [InlineData("a1b2c3")]
        public void Validate_ValidSlugFormats_ShouldPass(string slug)
        {
            // Arrange
            var command = new CreatePostCommand(
                Title: "Test Title",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Slug);
        }

        [Fact]
        public void Validate_ExcerptTooLong_ShouldFail()
        {
            // Arrange
            var longExcerpt = new string('a', 1001);
            var command = new CreatePostCommand(
                Title: "Test Title",
                Slug: "test-slug",
                Excerpt: longExcerpt,
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Excerpt)
                .WithErrorMessage("Excerpt cannot exceed 1000 characters.");
        }

        [Fact]
        public void Validate_MetaTitleTooLong_ShouldFail()
        {
            // Arrange
            var longMetaTitle = new string('a', 201);
            var command = new CreatePostCommand(
                Title: "Test Title",
                Slug: "test-slug",
                Excerpt: null,
                ContentJson: null,
                ContentHtml: null,
                FeaturedImageId: null,
                FeaturedImageUrl: null,
                FeaturedImageAlt: null,
                MetaTitle: longMetaTitle,
                MetaDescription: null,
                CanonicalUrl: null,
                AllowIndexing: true,
                CategoryId: null,
                TagIds: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MetaTitle)
                .WithErrorMessage("Meta title cannot exceed 200 characters.");
        }

        [Fact]
        public void Validate_MetaDescriptionTooLong_ShouldFail()
        {
            // Arrange
            var longMetaDescription = new string('a', 501);
            var command = new CreatePostCommand(
                Title: "Test Title",
                Slug: "test-slug",
                Excerpt: null,
                ContentJson: null,
                ContentHtml: null,
                FeaturedImageId: null,
                FeaturedImageUrl: null,
                FeaturedImageAlt: null,
                MetaTitle: null,
                MetaDescription: longMetaDescription,
                CanonicalUrl: null,
                AllowIndexing: true,
                CategoryId: null,
                TagIds: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MetaDescription)
                .WithErrorMessage("Meta description cannot exceed 500 characters.");
        }

        [Fact]
        public void Validate_FeaturedImageUrlTooLong_ShouldFail()
        {
            // Arrange
            var longUrl = new string('a', 2001);
            var command = new CreatePostCommand(
                Title: "Test Title",
                Slug: "test-slug",
                Excerpt: null,
                ContentJson: null,
                ContentHtml: null,
                FeaturedImageId: null,
                FeaturedImageUrl: longUrl,
                FeaturedImageAlt: null,
                MetaTitle: null,
                MetaDescription: null,
                CanonicalUrl: null,
                AllowIndexing: true,
                CategoryId: null,
                TagIds: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FeaturedImageUrl)
                .WithErrorMessage("Featured image URL cannot exceed 2000 characters.");
        }

        [Fact]
        public void Validate_FeaturedImageAltTooLong_ShouldFail()
        {
            // Arrange
            var longAlt = new string('a', 501);
            var command = new CreatePostCommand(
                Title: "Test Title",
                Slug: "test-slug",
                Excerpt: null,
                ContentJson: null,
                ContentHtml: null,
                FeaturedImageId: null,
                FeaturedImageUrl: "https://example.com/image.jpg",
                FeaturedImageAlt: longAlt,
                MetaTitle: null,
                MetaDescription: null,
                CanonicalUrl: null,
                AllowIndexing: true,
                CategoryId: null,
                TagIds: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.FeaturedImageAlt)
                .WithErrorMessage("Featured image alt text cannot exceed 500 characters.");
        }

        [Fact]
        public void Validate_CanonicalUrlTooLong_ShouldFail()
        {
            // Arrange
            var longUrl = new string('a', 2001);
            var command = new CreatePostCommand(
                Title: "Test Title",
                Slug: "test-slug",
                Excerpt: null,
                ContentJson: null,
                ContentHtml: null,
                FeaturedImageId: null,
                FeaturedImageUrl: null,
                FeaturedImageAlt: null,
                MetaTitle: null,
                MetaDescription: null,
                CanonicalUrl: longUrl,
                AllowIndexing: true,
                CategoryId: null,
                TagIds: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CanonicalUrl)
                .WithErrorMessage("Canonical URL cannot exceed 2000 characters.");
        }
    }

    #endregion

    #region UpdatePostCommandValidator Tests

    public class UpdatePostCommandValidatorTests
    {
        private readonly UpdatePostCommandValidator _validator;

        public UpdatePostCommandValidatorTests()
        {
            _validator = new UpdatePostCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new UpdatePostCommand(
                Id: Guid.NewGuid(),
                Title: "Updated Post Title",
                Slug: "updated-post-title",
                Excerpt: "Updated excerpt",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyId_ShouldFail()
        {
            // Arrange
            var command = new UpdatePostCommand(
                Id: Guid.Empty,
                Title: "Test Title",
                Slug: "test-slug",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Post ID is required.");
        }

        [Fact]
        public void Validate_EmptyTitle_ShouldFail()
        {
            // Arrange
            var command = new UpdatePostCommand(
                Id: Guid.NewGuid(),
                Title: "",
                Slug: "test-slug",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required.");
        }

        [Fact]
        public void Validate_TitleTooLong_ShouldFail()
        {
            // Arrange
            var longTitle = new string('a', 501);
            var command = new UpdatePostCommand(
                Id: Guid.NewGuid(),
                Title: longTitle,
                Slug: "test-slug",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title cannot exceed 500 characters.");
        }

        [Fact]
        public void Validate_EmptySlug_ShouldFail()
        {
            // Arrange
            var command = new UpdatePostCommand(
                Id: Guid.NewGuid(),
                Title: "Test Title",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug is required.");
        }

        [Theory]
        [InlineData("Invalid Slug")]
        [InlineData("UPPERCASE")]
        public void Validate_InvalidSlugFormat_ShouldFail(string slug)
        {
            // Arrange
            var command = new UpdatePostCommand(
                Id: Guid.NewGuid(),
                Title: "Test Title",
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
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");
        }
    }

    #endregion

    #region DeletePostCommandValidator Tests

    public class DeletePostCommandValidatorTests
    {
        private readonly DeletePostCommandValidator _validator;

        public DeletePostCommandValidatorTests()
        {
            _validator = new DeletePostCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new DeletePostCommand(Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyId_ShouldFail()
        {
            // Arrange
            var command = new DeletePostCommand(Guid.Empty);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Post ID is required.");
        }
    }

    #endregion

    #region PublishPostCommandValidator Tests

    public class PublishPostCommandValidatorTests
    {
        private readonly PublishPostCommandValidator _validator;

        public PublishPostCommandValidatorTests()
        {
            _validator = new PublishPostCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new PublishPostCommand(Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithScheduledDate_ShouldPass()
        {
            // Arrange
            var command = new PublishPostCommand(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyId_ShouldFail()
        {
            // Arrange
            var command = new PublishPostCommand(Guid.Empty);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Post ID is required.");
        }
    }

    #endregion

    #region CreateCategoryCommandValidator Tests

    public class CreateCategoryCommandValidatorTests
    {
        private readonly CreateCategoryCommandValidator _validator;

        public CreateCategoryCommandValidatorTests()
        {
            _validator = new CreateCategoryCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new CreateCategoryCommand(
                Name: "Test Category",
                Slug: "test-category",
                Description: "Test description",
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullName_ShouldFail(string? name)
        {
            // Arrange
            var command = new CreateCategoryCommand(
                Name: name!,
                Slug: "test-category",
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name is required.");
        }

        [Fact]
        public void Validate_NameTooLong_ShouldFail()
        {
            // Arrange
            var longName = new string('a', 201);
            var command = new CreateCategoryCommand(
                Name: longName,
                Slug: "test-category",
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name cannot exceed 200 characters.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullSlug_ShouldFail(string? slug)
        {
            // Arrange
            var command = new CreateCategoryCommand(
                Name: "Test Category",
                Slug: slug!,
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug is required.");
        }

        [Fact]
        public void Validate_SlugTooLong_ShouldFail()
        {
            // Arrange
            var longSlug = new string('a', 201);
            var command = new CreateCategoryCommand(
                Name: "Test Category",
                Slug: longSlug,
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug cannot exceed 200 characters.");
        }

        [Theory]
        [InlineData("Invalid Slug")]
        [InlineData("UPPERCASE")]
        [InlineData("-starts-with-hyphen")]
        public void Validate_InvalidSlugFormat_ShouldFail(string slug)
        {
            // Arrange
            var command = new CreateCategoryCommand(
                Name: "Test Category",
                Slug: slug,
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");
        }

        [Fact]
        public void Validate_DescriptionTooLong_ShouldFail()
        {
            // Arrange
            var longDescription = new string('a', 1001);
            var command = new CreateCategoryCommand(
                Name: "Test Category",
                Slug: "test-category",
                Description: longDescription,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description cannot exceed 1000 characters.");
        }

        [Fact]
        public void Validate_MetaTitleTooLong_ShouldFail()
        {
            // Arrange
            var longMetaTitle = new string('a', 201);
            var command = new CreateCategoryCommand(
                Name: "Test Category",
                Slug: "test-category",
                Description: null,
                MetaTitle: longMetaTitle,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MetaTitle)
                .WithErrorMessage("Meta title cannot exceed 200 characters.");
        }

        [Fact]
        public void Validate_MetaDescriptionTooLong_ShouldFail()
        {
            // Arrange
            var longMetaDescription = new string('a', 501);
            var command = new CreateCategoryCommand(
                Name: "Test Category",
                Slug: "test-category",
                Description: null,
                MetaTitle: null,
                MetaDescription: longMetaDescription,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.MetaDescription)
                .WithErrorMessage("Meta description cannot exceed 500 characters.");
        }

        [Fact]
        public void Validate_ImageUrlTooLong_ShouldFail()
        {
            // Arrange
            var longUrl = new string('a', 2001);
            var command = new CreateCategoryCommand(
                Name: "Test Category",
                Slug: "test-category",
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: longUrl,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
                .WithErrorMessage("Image URL cannot exceed 2000 characters.");
        }

        [Fact]
        public void Validate_NegativeSortOrder_ShouldFail()
        {
            // Arrange
            var command = new CreateCategoryCommand(
                Name: "Test Category",
                Slug: "test-category",
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: -1,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SortOrder)
                .WithErrorMessage("Sort order must be a non-negative number.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public void Validate_ValidSortOrder_ShouldPass(int sortOrder)
        {
            // Arrange
            var command = new CreateCategoryCommand(
                Name: "Test Category",
                Slug: "test-category",
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: sortOrder,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
        }
    }

    #endregion

    #region UpdateCategoryCommandValidator Tests

    public class UpdateCategoryCommandValidatorTests
    {
        private readonly UpdateCategoryCommandValidator _validator;

        public UpdateCategoryCommandValidatorTests()
        {
            _validator = new UpdateCategoryCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new UpdateCategoryCommand(
                Id: Guid.NewGuid(),
                Name: "Updated Category",
                Slug: "updated-category",
                Description: "Updated description",
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyId_ShouldFail()
        {
            // Arrange
            var command = new UpdateCategoryCommand(
                Id: Guid.Empty,
                Name: "Test Category",
                Slug: "test-category",
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Category ID is required.");
        }

        [Fact]
        public void Validate_EmptyName_ShouldFail()
        {
            // Arrange
            var command = new UpdateCategoryCommand(
                Id: Guid.NewGuid(),
                Name: "",
                Slug: "test-category",
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name is required.");
        }

        [Fact]
        public void Validate_EmptySlug_ShouldFail()
        {
            // Arrange
            var command = new UpdateCategoryCommand(
                Id: Guid.NewGuid(),
                Name: "Test Category",
                Slug: "",
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: 0,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug is required.");
        }

        [Fact]
        public void Validate_NegativeSortOrder_ShouldFail()
        {
            // Arrange
            var command = new UpdateCategoryCommand(
                Id: Guid.NewGuid(),
                Name: "Test Category",
                Slug: "test-category",
                Description: null,
                MetaTitle: null,
                MetaDescription: null,
                ImageUrl: null,
                SortOrder: -1,
                ParentId: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.SortOrder)
                .WithErrorMessage("Sort order must be a non-negative number.");
        }
    }

    #endregion

    #region DeleteCategoryCommandValidator Tests

    public class DeleteCategoryCommandValidatorTests
    {
        private readonly DeleteCategoryCommandValidator _validator;

        public DeleteCategoryCommandValidatorTests()
        {
            _validator = new DeleteCategoryCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new DeleteCategoryCommand(Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyId_ShouldFail()
        {
            // Arrange
            var command = new DeleteCategoryCommand(Guid.Empty);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Category ID is required.");
        }
    }

    #endregion

    #region CreateTagCommandValidator Tests

    public class CreateTagCommandValidatorTests
    {
        private readonly CreateTagCommandValidator _validator;

        public CreateTagCommandValidatorTests()
        {
            _validator = new CreateTagCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new CreateTagCommand(
                Name: "Test Tag",
                Slug: "test-tag",
                Description: "Test description",
                Color: "#3B82F6");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithoutOptionalFields_ShouldPass()
        {
            // Arrange
            var command = new CreateTagCommand(
                Name: "Test Tag",
                Slug: "test-tag",
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullName_ShouldFail(string? name)
        {
            // Arrange
            var command = new CreateTagCommand(
                Name: name!,
                Slug: "test-tag",
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name is required.");
        }

        [Fact]
        public void Validate_NameTooLong_ShouldFail()
        {
            // Arrange
            var longName = new string('a', 101);
            var command = new CreateTagCommand(
                Name: longName,
                Slug: "test-tag",
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name cannot exceed 100 characters.");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullSlug_ShouldFail(string? slug)
        {
            // Arrange
            var command = new CreateTagCommand(
                Name: "Test Tag",
                Slug: slug!,
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug is required.");
        }

        [Fact]
        public void Validate_SlugTooLong_ShouldFail()
        {
            // Arrange
            var longSlug = new string('a', 101);
            var command = new CreateTagCommand(
                Name: "Test Tag",
                Slug: longSlug,
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug cannot exceed 100 characters.");
        }

        [Theory]
        [InlineData("Invalid Slug")]
        [InlineData("UPPERCASE")]
        public void Validate_InvalidSlugFormat_ShouldFail(string slug)
        {
            // Arrange
            var command = new CreateTagCommand(
                Name: "Test Tag",
                Slug: slug,
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");
        }

        [Fact]
        public void Validate_DescriptionTooLong_ShouldFail()
        {
            // Arrange
            var longDescription = new string('a', 501);
            var command = new CreateTagCommand(
                Name: "Test Tag",
                Slug: "test-tag",
                Description: longDescription,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description cannot exceed 500 characters.");
        }

        [Fact]
        public void Validate_ColorTooLong_ShouldFail()
        {
            // Arrange
            var longColor = new string('a', 21);
            var command = new CreateTagCommand(
                Name: "Test Tag",
                Slug: "test-tag",
                Description: null,
                Color: longColor);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Color)
                .WithErrorMessage("Color cannot exceed 20 characters.");
        }

        [Theory]
        [InlineData("red")] // Not hex format
        [InlineData("#FFF")] // Short hex format
        [InlineData("#GGGGGG")] // Invalid hex characters
        [InlineData("3B82F6")] // Missing hash
        [InlineData("#3B82F6FF")] // Too long (includes alpha)
        public void Validate_InvalidColorFormat_ShouldFail(string color)
        {
            // Arrange
            var command = new CreateTagCommand(
                Name: "Test Tag",
                Slug: "test-tag",
                Description: null,
                Color: color);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Color)
                .WithErrorMessage("Color must be a valid hex color code (e.g., #3B82F6).");
        }

        [Theory]
        [InlineData("#3B82F6")]
        [InlineData("#000000")]
        [InlineData("#FFFFFF")]
        [InlineData("#aabbcc")]
        [InlineData("#AbCdEf")]
        public void Validate_ValidColorFormats_ShouldPass(string color)
        {
            // Arrange
            var command = new CreateTagCommand(
                Name: "Test Tag",
                Slug: "test-tag",
                Description: null,
                Color: color);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Color);
        }
    }

    #endregion

    #region UpdateTagCommandValidator Tests

    public class UpdateTagCommandValidatorTests
    {
        private readonly UpdateTagCommandValidator _validator;

        public UpdateTagCommandValidatorTests()
        {
            _validator = new UpdateTagCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new UpdateTagCommand(
                Id: Guid.NewGuid(),
                Name: "Updated Tag",
                Slug: "updated-tag",
                Description: "Updated description",
                Color: "#FF5733");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyId_ShouldFail()
        {
            // Arrange
            var command = new UpdateTagCommand(
                Id: Guid.Empty,
                Name: "Test Tag",
                Slug: "test-tag",
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Tag ID is required.");
        }

        [Fact]
        public void Validate_EmptyName_ShouldFail()
        {
            // Arrange
            var command = new UpdateTagCommand(
                Id: Guid.NewGuid(),
                Name: "",
                Slug: "test-tag",
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name is required.");
        }

        [Fact]
        public void Validate_NameTooLong_ShouldFail()
        {
            // Arrange
            var longName = new string('a', 101);
            var command = new UpdateTagCommand(
                Id: Guid.NewGuid(),
                Name: longName,
                Slug: "test-tag",
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Name cannot exceed 100 characters.");
        }

        [Fact]
        public void Validate_EmptySlug_ShouldFail()
        {
            // Arrange
            var command = new UpdateTagCommand(
                Id: Guid.NewGuid(),
                Name: "Test Tag",
                Slug: "",
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug is required.");
        }

        [Theory]
        [InlineData("Invalid Slug")]
        [InlineData("UPPERCASE")]
        public void Validate_InvalidSlugFormat_ShouldFail(string slug)
        {
            // Arrange
            var command = new UpdateTagCommand(
                Id: Guid.NewGuid(),
                Name: "Test Tag",
                Slug: slug,
                Description: null,
                Color: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Slug)
                .WithErrorMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("#FFF")]
        public void Validate_InvalidColorFormat_ShouldFail(string color)
        {
            // Arrange
            var command = new UpdateTagCommand(
                Id: Guid.NewGuid(),
                Name: "Test Tag",
                Slug: "test-tag",
                Description: null,
                Color: color);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Color)
                .WithErrorMessage("Color must be a valid hex color code (e.g., #3B82F6).");
        }
    }

    #endregion

    #region DeleteTagCommandValidator Tests

    public class DeleteTagCommandValidatorTests
    {
        private readonly DeleteTagCommandValidator _validator;

        public DeleteTagCommandValidatorTests()
        {
            _validator = new DeleteTagCommandValidator();
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new DeleteTagCommand(Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyId_ShouldFail()
        {
            // Arrange
            var command = new DeleteTagCommand(Guid.Empty);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Id)
                .WithErrorMessage("Tag ID is required.");
        }
    }

    #endregion
}
