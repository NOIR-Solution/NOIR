namespace NOIR.Application.UnitTests.Features.Tenants.Validators;

using NOIR.Application.Features.Tenants.Commands.CreateTenant;

/// <summary>
/// Unit tests for CreateTenantCommandValidator.
/// Tests validation rules for tenant creation.
/// </summary>
public class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator;

    public CreateTenantCommandValidatorTests()
    {
        _validator = new CreateTenantCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Tenant identifier validations
        mock.Setup(x => x["validation.tenantIdentifier.required"]).Returns("Tenant identifier is required");
        mock.Setup(x => x["validation.tenantIdentifier.pattern"]).Returns("Tenant identifier must be lowercase alphanumeric with optional hyphens");
        mock.Setup(x => x.Get("validation.tenantIdentifier.minLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Tenant identifier must be at least {args[0]} characters");
        mock.Setup(x => x.Get("validation.tenantIdentifier.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Tenant identifier cannot exceed {args[0]} characters");

        // Tenant name validations
        mock.Setup(x => x["validation.tenantName.required"]).Returns("Tenant name is required");
        mock.Setup(x => x.Get("validation.tenantName.minLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Tenant name must be at least {args[0]} characters");
        mock.Setup(x => x.Get("validation.tenantName.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Tenant name cannot exceed {args[0]} characters");

        // Tenant domain validations
        mock.Setup(x => x["validation.tenantDomain.pattern"]).Returns("Domain must be a valid domain name (e.g., example.com)");
        mock.Setup(x => x.Get("validation.tenantDomain.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Domain cannot exceed {args[0]} characters");

        // Tenant description validations
        mock.Setup(x => x.Get("validation.tenantDescription.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Description cannot exceed {args[0]} characters");

        // Tenant note validations
        mock.Setup(x => x.Get("validation.tenantNote.maxLength", It.IsAny<object[]>()))
            .Returns((string _, object[] args) => $"Note cannot exceed {args[0]} characters");

        return mock.Object;
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandHasAllOptionalFields_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: "test.example.com",
            Description: "Test description",
            Note: "Test note",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Identifier Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenIdentifierIsEmptyOrWhitespace_ShouldHaveError(string? identifier)
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: identifier!,
            Name: "Test Tenant",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Identifier);
    }

    [Fact]
    public async Task Validate_WhenIdentifierIsTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "a",
            Name: "Test Tenant",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Identifier)
            .WithErrorMessage("Tenant identifier must be at least 2 characters");
    }

    [Fact]
    public async Task Validate_WhenIdentifierIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: new string('a', 101),
            Name: "Test Tenant",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Identifier)
            .WithErrorMessage("Tenant identifier cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("Test-Tenant")]
    [InlineData("test_tenant")]
    [InlineData("-test")]
    [InlineData("test-")]
    [InlineData("TEST")]
    [InlineData("test tenant")]
    [InlineData("test@tenant")]
    public async Task Validate_WhenIdentifierHasInvalidFormat_ShouldHaveError(string identifier)
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: identifier,
            Name: "Test Tenant",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Identifier)
            .WithErrorMessage("Tenant identifier must be lowercase alphanumeric with optional hyphens");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("test")]
    [InlineData("test-tenant")]
    [InlineData("test123")]
    [InlineData("123test")]
    [InlineData("a1b2c3")]
    public async Task Validate_WhenIdentifierIsValid_ShouldNotHaveError(string identifier)
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: identifier,
            Name: "Test Tenant",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Identifier);
    }

    #endregion

    #region Name Validation

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenNameIsEmptyOrWhitespace_ShouldHaveError(string? name)
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: name!,
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenNameIsTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "A",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Tenant name must be at least 2 characters");
    }

    [Fact]
    public async Task Validate_WhenNameIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: new string('a', 201),
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Tenant name cannot exceed 200 characters");
    }

    [Fact]
    public async Task Validate_WhenNameIs200Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: new string('a', 200),
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Domain Validation

    [Fact]
    public async Task Validate_WhenDomainIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: null,
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Domain);
    }

    [Fact]
    public async Task Validate_WhenDomainIsEmpty_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: "",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Domain);
    }

    [Fact]
    public async Task Validate_WhenDomainIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: new string('a', 501),
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Domain)
            .WithErrorMessage("Domain cannot exceed 500 characters");
    }

    [Theory]
    [InlineData("Example.com")]
    [InlineData("-example.com")]
    [InlineData("example-.com")]
    [InlineData("example..com")]
    [InlineData("example_test.com")]
    public async Task Validate_WhenDomainHasInvalidFormat_ShouldHaveError(string domain)
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: domain,
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Domain)
            .WithErrorMessage("Domain must be a valid domain name (e.g., example.com)");
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("sub.example.com")]
    [InlineData("test-site.example.com")]
    [InlineData("a1.b2.c3.example.com")]
    public async Task Validate_WhenDomainIsValid_ShouldNotHaveError(string domain)
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: domain,
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Domain);
    }

    #endregion

    #region Description Validation

    [Fact]
    public async Task Validate_WhenDescriptionIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Description: null,
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task Validate_WhenDescriptionIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Description: new string('a', 1001),
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 1000 characters");
    }

    [Fact]
    public async Task Validate_WhenDescriptionIs1000Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Description: new string('a', 1000),
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region Note Validation

    [Fact]
    public async Task Validate_WhenNoteIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Note: null,
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public async Task Validate_WhenNoteIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Note: new string('a', 2001),
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Note)
            .WithErrorMessage("Note cannot exceed 2000 characters");
    }

    [Fact]
    public async Task Validate_WhenNoteIs2000Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new CreateTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Note: new string('a', 2000),
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    #endregion
}
