namespace NOIR.Application.UnitTests.Features.LegalPages;

/// <summary>
/// Unit tests for UpdateLegalPageCommandValidator.
/// Tests validation rules for legal page updates.
/// </summary>
public class UpdateLegalPageCommandValidatorTests
{
    private readonly UpdateLegalPageCommandValidator _validator;

    public UpdateLegalPageCommandValidatorTests()
    {
        _validator = new UpdateLegalPageCommandValidator();
    }

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.Empty,
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Page ID is required.");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Title Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenTitleIsEmptyOrWhitespace_ShouldHaveError(string? title)
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: title!,
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public async Task Validate_WhenTitleExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: new string('A', 201),
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must not exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_WhenTitleIs200Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: new string('A', 200),
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    #endregion

    #region HtmlContent Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenHtmlContentIsEmptyOrWhitespace_ShouldHaveError(string? content)
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: content!,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HtmlContent);
    }

    #endregion

    #region MetaTitle Validation

    [Fact]
    public async Task Validate_WhenMetaTitleExceeds60Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: new string('A', 61),
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaTitle)
            .WithErrorMessage("Meta title must not exceed 60 characters.");
    }

    [Fact]
    public async Task Validate_WhenMetaTitleIs60Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: new string('A', 60),
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MetaTitle);
    }

    [Fact]
    public async Task Validate_WhenMetaTitleIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MetaTitle);
    }

    #endregion

    #region MetaDescription Validation

    [Fact]
    public async Task Validate_WhenMetaDescriptionExceeds160Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: new string('A', 161),
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaDescription)
            .WithErrorMessage("Meta description must not exceed 160 characters.");
    }

    [Fact]
    public async Task Validate_WhenMetaDescriptionIs160Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: new string('A', 160),
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MetaDescription);
    }

    #endregion

    #region CanonicalUrl Validation

    [Fact]
    public async Task Validate_WhenCanonicalUrlIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: "not-a-valid-url",
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CanonicalUrl)
            .WithErrorMessage("Canonical URL must be a valid absolute URL.");
    }

    [Fact]
    public async Task Validate_WhenCanonicalUrlIsRelative_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: "/relative/path",
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CanonicalUrl)
            .WithErrorMessage("Canonical URL must be a valid absolute URL.");
    }

    [Theory]
    [InlineData("https://example.com/privacy")]
    [InlineData("http://localhost:3000/terms")]
    [InlineData("https://www.example.com/legal/terms-of-service")]
    public async Task Validate_WhenCanonicalUrlIsValidAbsolute_ShouldNotHaveError(string url)
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: url,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CanonicalUrl);
    }

    [Fact]
    public async Task Validate_WhenCanonicalUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CanonicalUrl);
    }

    [Fact]
    public async Task Validate_WhenCanonicalUrlIsEmpty_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Valid Title",
            HtmlContent: "<p>Valid Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: "",
            AllowIndexing: true);

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
        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "Terms of Service",
            HtmlContent: "<p>Please read these terms carefully.</p>",
            MetaTitle: "Terms of Service | My App",
            MetaDescription: "Read our terms and conditions for using our services.",
            CanonicalUrl: "https://example.com/terms",
            AllowIndexing: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
