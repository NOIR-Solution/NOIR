namespace NOIR.Application.UnitTests.Features.Tenants.Validators;

using NOIR.Application.Features.Tenants.Commands.ProvisionTenant;

/// <summary>
/// Unit tests for ProvisionTenantCommandValidator.
/// Tests validation rules for tenant provisioning.
/// </summary>
public class ProvisionTenantCommandValidatorTests
{
    private readonly ProvisionTenantCommandValidator _validator;

    public ProvisionTenantCommandValidatorTests()
    {
        _validator = new ProvisionTenantCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        mock.Setup(x => x["validation.required"]).Returns("This field is required");
        mock.Setup(x => x["validation.email"]).Returns("Invalid email address format");
        mock.Setup(x => x["validation.minLength"]).Returns("Must be at least {0} characters");
        mock.Setup(x => x["validation.maxLength"]).Returns("Cannot exceed {0} characters");
        mock.Setup(x => x["validation.tenants.identifierFormat"]).Returns("Identifier must contain only lowercase letters, numbers, and hyphens");
        mock.Setup(x => x["validation.tenants.domainFormat"]).Returns("Domain must be a valid hostname format");

        return mock.Object;
    }

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValidWithoutAdminUser_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandIsValidWithAdminUser_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@example.com",
            AdminPassword: "password123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenCommandHasAllOptionalFields_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: "test.example.com",
            Description: "Test description",
            Note: "Test note",
            CreateAdminUser: true,
            AdminEmail: "admin@example.com",
            AdminPassword: "password123",
            AdminFirstName: "John",
            AdminLastName: "Doe");

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
        var command = new ProvisionTenantCommand(
            Identifier: identifier!,
            Name: "Test Tenant",
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Identifier);
    }

    [Fact]
    public async Task Validate_WhenIdentifierIsTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "a",
            Name: "Test Tenant",
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Identifier);
    }

    [Fact]
    public async Task Validate_WhenIdentifierIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: new string('a', 65),
            Name: "Test Tenant",
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Identifier);
    }

    [Theory]
    [InlineData("Test-Tenant")]
    [InlineData("test_tenant")]
    [InlineData("test tenant")]
    [InlineData("TEST")]
    public async Task Validate_WhenIdentifierHasInvalidFormat_ShouldHaveError(string identifier)
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: identifier,
            Name: "Test Tenant",
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Identifier)
            .WithErrorMessage("Identifier must contain only lowercase letters, numbers, and hyphens");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("test-tenant")]
    [InlineData("test123")]
    [InlineData("123test")]
    public async Task Validate_WhenIdentifierIsValid_ShouldNotHaveError(string identifier)
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: identifier,
            Name: "Test Tenant",
            CreateAdminUser: false);

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
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: name!,
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenNameIsTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "A",
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenNameIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: new string('a', 257),
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Domain Validation

    [Fact]
    public async Task Validate_WhenDomainIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: null,
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Domain);
    }

    [Fact]
    public async Task Validate_WhenDomainIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: new string('a', 257),
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Domain);
    }

    [Theory]
    [InlineData("Example.com")]
    [InlineData("-example")]
    [InlineData("example-")]
    public async Task Validate_WhenDomainHasInvalidFormat_ShouldHaveError(string domain)
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: domain,
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Domain)
            .WithErrorMessage("Domain must be a valid hostname format");
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("sub.example.com")]
    [InlineData("test-site.example.com")]
    public async Task Validate_WhenDomainIsValid_ShouldNotHaveError(string domain)
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Domain: domain,
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Domain);
    }

    #endregion

    #region Description Validation

    [Fact]
    public async Task Validate_WhenDescriptionIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Description: new string('a', 1025),
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region Note Validation

    [Fact]
    public async Task Validate_WhenNoteIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            Note: new string('a', 4097),
            CreateAdminUser: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }

    #endregion

    #region Admin User Validation

    [Fact]
    public async Task Validate_WhenCreateAdminUserIsTrueAndAdminEmailIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            CreateAdminUser: true,
            AdminEmail: "",
            AdminPassword: "password123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AdminEmail);
    }

    [Fact]
    public async Task Validate_WhenCreateAdminUserIsTrueAndAdminEmailIsNull_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            CreateAdminUser: true,
            AdminEmail: null,
            AdminPassword: "password123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AdminEmail);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public async Task Validate_WhenCreateAdminUserIsTrueAndAdminEmailIsInvalid_ShouldHaveError(string email)
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            CreateAdminUser: true,
            AdminEmail: email,
            AdminPassword: "password123");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AdminEmail);
    }

    [Fact]
    public async Task Validate_WhenCreateAdminUserIsTrueAndAdminPasswordIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@example.com",
            AdminPassword: "");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AdminPassword);
    }

    [Fact]
    public async Task Validate_WhenCreateAdminUserIsTrueAndAdminPasswordIsTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@example.com",
            AdminPassword: "12345");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AdminPassword);
    }

    [Fact]
    public async Task Validate_WhenCreateAdminUserIsFalseAndAdminFieldsAreEmpty_ShouldNotHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            CreateAdminUser: false,
            AdminEmail: null,
            AdminPassword: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AdminEmail);
        result.ShouldNotHaveValidationErrorFor(x => x.AdminPassword);
    }

    [Fact]
    public async Task Validate_WhenAdminFirstNameIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@example.com",
            AdminPassword: "password123",
            AdminFirstName: new string('a', 65));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AdminFirstName);
    }

    [Fact]
    public async Task Validate_WhenAdminLastNameIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new ProvisionTenantCommand(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@example.com",
            AdminPassword: "password123",
            AdminLastName: new string('a', 65));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AdminLastName);
    }

    #endregion
}
