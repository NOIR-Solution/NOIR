namespace NOIR.Application.UnitTests.Features.Tenants.Validators;

using NOIR.Application.Features.Tenants.Commands.UpdateTenant;

/// <summary>
/// Unit tests for UpdateTenantCommandValidator.
/// Tests validation rules for tenant updates.
/// </summary>
public class UpdateTenantCommandValidatorTests
{
    private readonly UpdateTenantCommandValidator _validator;

    public UpdateTenantCommandValidatorTests()
    {
        _validator = new UpdateTenantCommandValidator(CreateLocalizationMock());
    }

    /// <summary>
    /// Creates a mock ILocalizationService that returns the expected English messages.
    /// </summary>
    private static ILocalizationService CreateLocalizationMock()
    {
        var mock = new Mock<ILocalizationService>();

        // Tenant ID validations
        mock.Setup(x => x["validation.tenantId.required"]).Returns("Tenant ID is required");

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
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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

    #region TenantId Validation

    [Fact]
    public async Task Validate_WhenTenantIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateTenantCommand(
            TenantId: Guid.Empty,
            Identifier: "test-tenant",
            Name: "Test Tenant",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId)
            .WithErrorMessage("Tenant ID is required");
    }

    [Fact]
    public async Task Validate_WhenTenantIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
            Identifier: "test-tenant",
            Name: "Test Tenant",
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TenantId);
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
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
    public async Task Validate_WhenIdentifierHasInvalidFormat_ShouldHaveError(string identifier)
    {
        // Arrange
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
    [InlineData("test-tenant")]
    [InlineData("test123")]
    public async Task Validate_WhenIdentifierIsValid_ShouldNotHaveError(string identifier)
    {
        // Arrange
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
            Identifier: "test-tenant",
            Name: new string('a', 201),
            IsActive: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Tenant name cannot exceed 200 characters");
    }

    #endregion

    #region Domain Validation

    [Fact]
    public async Task Validate_WhenDomainIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
    public async Task Validate_WhenDomainIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
    public async Task Validate_WhenDomainHasInvalidFormat_ShouldHaveError(string domain)
    {
        // Arrange
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
    public async Task Validate_WhenDomainIsValid_ShouldNotHaveError(string domain)
    {
        // Arrange
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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
    public async Task Validate_WhenDescriptionIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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

    #endregion

    #region Note Validation

    [Fact]
    public async Task Validate_WhenNoteIsTooLong_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateTenantCommand(
            TenantId: Guid.NewGuid(),
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

    #endregion

    #region FromRequest Tests

    [Fact]
    public async Task FromRequest_WhenCalled_ShouldCreateValidCommand()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var request = new UpdateTenantRequest(
            Identifier: "test-tenant",
            Name: "Test Tenant",
            IsActive: true);

        // Act
        var command = UpdateTenantCommand.FromRequest(tenantId, request);
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
        command.TenantId.Should().Be(tenantId);
        command.Identifier.Should().Be(request.Identifier);
        command.Name.Should().Be(request.Name);
    }

    #endregion
}
