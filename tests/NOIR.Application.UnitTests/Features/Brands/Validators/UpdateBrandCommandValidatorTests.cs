using NOIR.Application.Features.Brands.Commands.UpdateBrand;

namespace NOIR.Application.UnitTests.Features.Brands.Validators;

/// <summary>
/// Unit tests for UpdateBrandCommandValidator.
/// Tests all validation rules for updating a brand.
/// </summary>
public class UpdateBrandCommandValidatorTests
{
    private readonly UpdateBrandCommandValidator _validator = new();

    private static UpdateBrandCommand CreateValidCommand(
        Guid? id = null,
        string name = "Nike",
        string slug = "nike",
        string? description = null,
        string? website = null,
        string? logoUrl = null,
        string? bannerUrl = null,
        string? metaTitle = null,
        string? metaDescription = null,
        bool isActive = true,
        bool isFeatured = false,
        int sortOrder = 0)
    {
        return new UpdateBrandCommand(
            id ?? Guid.NewGuid(),
            name,
            slug,
            description,
            website,
            logoUrl,
            bannerUrl,
            metaTitle,
            metaDescription,
            isActive,
            isFeatured,
            sortOrder);
    }

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(id: Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Brand ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(id: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Name Validation

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(name: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Brand name is required.");
    }

    [Fact]
    public async Task Validate_WhenNameExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(name: new string('A', 201));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Brand name cannot exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenNameIs200Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(name: new string('A', 200));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Slug Validation

    [Fact]
    public async Task Validate_WhenSlugIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(slug: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Brand slug is required.");
    }

    [Fact]
    public async Task Validate_WhenSlugExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(slug: new string('a', 201));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Brand slug cannot exceed 200 characters.");
    }

    [Theory]
    [InlineData("Invalid Slug")]
    [InlineData("INVALID-SLUG")]
    [InlineData("-invalid-slug")]
    [InlineData("invalid-slug-")]
    public async Task Validate_WhenSlugHasInvalidFormat_ShouldHaveError(string slug)
    {
        // Arrange
        var command = CreateValidCommand(slug: slug);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug must contain only lowercase letters, numbers, and hyphens.");
    }

    [Theory]
    [InlineData("valid-slug")]
    [InlineData("nike")]
    [InlineData("adidas-original")]
    [InlineData("brand123")]
    public async Task Validate_WhenSlugHasValidFormat_ShouldNotHaveError(string slug)
    {
        // Arrange
        var command = CreateValidCommand(slug: slug);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Slug);
    }

    #endregion

    #region Description Validation

    [Fact]
    public async Task Validate_WhenDescriptionExceeds5000Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(description: new string('A', 5001));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 5000 characters.");
    }

    [Fact]
    public async Task Validate_WhenDescriptionIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(description: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region Website Validation

    [Fact]
    public async Task Validate_WhenWebsiteExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(website: new string('a', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Website)
            .WithErrorMessage("Website URL cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenWebsiteIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(website: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Website);
    }

    #endregion

    #region LogoUrl Validation

    [Fact]
    public async Task Validate_WhenLogoUrlExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(logoUrl: new string('a', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LogoUrl)
            .WithErrorMessage("Logo URL cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenLogoUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(logoUrl: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LogoUrl);
    }

    #endregion

    #region BannerUrl Validation

    [Fact]
    public async Task Validate_WhenBannerUrlExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(bannerUrl: new string('a', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BannerUrl)
            .WithErrorMessage("Banner URL cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenBannerUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand(bannerUrl: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BannerUrl);
    }

    #endregion

    #region MetaTitle Validation

    [Fact]
    public async Task Validate_WhenMetaTitleExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(metaTitle: new string('A', 201));

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
        var command = CreateValidCommand(metaTitle: null);

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
        var command = CreateValidCommand(metaDescription: new string('A', 501));

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
        var command = CreateValidCommand(metaDescription: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MetaDescription);
    }

    #endregion

    #region SortOrder Validation

    [Fact]
    public async Task Validate_WhenSortOrderIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand(sortOrder: -1);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortOrder)
            .WithErrorMessage("Sort order must be zero or greater.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Validate_WhenSortOrderIsNonNegative_ShouldNotHaveError(int sortOrder)
    {
        // Arrange
        var command = CreateValidCommand(sortOrder: sortOrder);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateBrandCommand(
            Guid.NewGuid(),
            "Nike",
            "nike",
            "Nike is a global sportswear brand.",
            "https://www.nike.com",
            "https://cdn.example.com/nike-logo.png",
            "https://cdn.example.com/nike-banner.png",
            "Nike | Shop Sports Apparel",
            "Shop the latest Nike sports apparel and footwear.",
            IsActive: true,
            IsFeatured: true,
            SortOrder: 1);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateBrandCommand(
            Guid.NewGuid(),
            "Nike",
            "nike",
            Description: null,
            Website: null,
            LogoUrl: null,
            BannerUrl: null,
            MetaTitle: null,
            MetaDescription: null,
            IsActive: true,
            IsFeatured: false,
            SortOrder: 0);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
