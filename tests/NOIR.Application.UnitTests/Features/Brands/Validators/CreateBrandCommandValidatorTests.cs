using NOIR.Application.Features.Brands.Commands.CreateBrand;

namespace NOIR.Application.UnitTests.Features.Brands.Validators;

/// <summary>
/// Unit tests for CreateBrandCommandValidator.
/// Tests all validation rules for creating a brand.
/// </summary>
public class CreateBrandCommandValidatorTests
{
    private readonly CreateBrandCommandValidator _validator = new();

    #region Name Validation

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

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
        var command = new CreateBrandCommand(
            Name: new string('A', 201),
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Brand name must not exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenNameIs200Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: new string('A', 200),
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

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
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

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
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: new string('a', 201),
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Brand slug must not exceed 200 characters.");
    }

    [Theory]
    [InlineData("Invalid Slug")]
    [InlineData("INVALID-SLUG")]
    [InlineData("invalid_slug")]
    [InlineData("invalid.slug")]
    public async Task Validate_WhenSlugHasInvalidFormat_ShouldHaveError(string slug)
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: slug,
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug can only contain lowercase letters, numbers, and hyphens.");
    }

    [Theory]
    [InlineData("valid-slug")]
    [InlineData("nike")]
    [InlineData("adidas-original")]
    [InlineData("brand123")]
    public async Task Validate_WhenSlugHasValidFormat_ShouldNotHaveError(string slug)
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: slug,
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Slug);
    }

    #endregion

    #region LogoUrl Validation

    [Fact]
    public async Task Validate_WhenLogoUrlExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: new string('a', 501),
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LogoUrl)
            .WithErrorMessage("Logo URL must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenLogoUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

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
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: new string('a', 501),
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BannerUrl)
            .WithErrorMessage("Banner URL must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenBannerUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BannerUrl);
    }

    #endregion

    #region Description Validation

    [Fact]
    public async Task Validate_WhenDescriptionExceeds5000Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: new string('A', 5001),
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 5000 characters.");
    }

    [Fact]
    public async Task Validate_WhenDescriptionIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

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
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: new string('a', 501),
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Website)
            .WithErrorMessage("Website URL must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenWebsiteIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Website);
    }

    #endregion

    #region MetaTitle Validation

    [Fact]
    public async Task Validate_WhenMetaTitleExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: new string('A', 201),
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaTitle)
            .WithErrorMessage("Meta title must not exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenMetaTitleIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

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
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: new string('A', 501),
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaDescription)
            .WithErrorMessage("Meta description must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenMetaDescriptionIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Valid Name",
            Slug: "valid-slug",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MetaDescription);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Nike",
            Slug: "nike",
            LogoUrl: "https://cdn.example.com/nike-logo.png",
            BannerUrl: "https://cdn.example.com/nike-banner.png",
            Description: "Nike is a global sportswear brand.",
            Website: "https://www.nike.com",
            MetaTitle: "Nike | Shop Sports Apparel",
            MetaDescription: "Shop the latest Nike sports apparel and footwear.",
            IsFeatured: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateBrandCommand(
            Name: "Nike",
            Slug: "nike",
            LogoUrl: null,
            BannerUrl: null,
            Description: null,
            Website: null,
            MetaTitle: null,
            MetaDescription: null,
            IsFeatured: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
