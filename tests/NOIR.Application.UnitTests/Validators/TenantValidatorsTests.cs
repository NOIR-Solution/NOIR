namespace NOIR.Application.UnitTests.Validators;

using NOIR.Application.Features.Tenants.Commands.CreateTenant;
using NOIR.Application.Features.Tenants.Commands.UpdateTenant;
using NOIR.Application.Features.Tenants.Commands.DeleteTenant;

/// <summary>
/// Unit tests for tenant command validators.
/// Tests all validation rules using FluentValidation.TestHelper.
/// </summary>
public class TenantValidatorsTests
{
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

        return mock.Object;
    }

    #region CreateTenantCommandValidator Tests

    public class CreateTenantCommandValidatorTests
    {
        private readonly CreateTenantCommandValidator _validator;

        public CreateTenantCommandValidatorTests()
        {
            _validator = new CreateTenantCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new CreateTenantCommand(
                Identifier: "test-tenant",
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithMinimumLength_ShouldPass()
        {
            // Arrange
            var command = new CreateTenantCommand(
                Identifier: "ab",
                Name: "AB",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidSingleCharacterIdentifier_ShouldPass()
        {
            // Arrange - Single alphanumeric character is valid per regex
            var command = new CreateTenantCommand(
                Identifier: "a",
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert - Should fail for min length, not pattern
            result.ShouldHaveValidationErrorFor(x => x.Identifier)
                .WithErrorMessage("Tenant identifier must be at least 2 characters");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullIdentifier_ShouldFail(string? identifier)
        {
            // Arrange
            var command = new CreateTenantCommand(
                Identifier: identifier!,
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Identifier)
                .WithErrorMessage("Tenant identifier is required");
        }

        [Theory]
        [InlineData("a")] // 1 character - below minimum
        public void Validate_IdentifierTooShort_ShouldFail(string identifier)
        {
            // Arrange
            var command = new CreateTenantCommand(
                Identifier: identifier,
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Identifier)
                .WithErrorMessage("Tenant identifier must be at least 2 characters");
        }

        [Fact]
        public void Validate_IdentifierTooLong_ShouldFail()
        {
            // Arrange
            var longIdentifier = new string('a', 101);
            var command = new CreateTenantCommand(
                Identifier: longIdentifier,
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Identifier)
                .WithErrorMessage("Tenant identifier cannot exceed 100 characters");
        }

        [Theory]
        [InlineData("Test-Tenant")] // Uppercase
        [InlineData("test_tenant")] // Underscore
        [InlineData("test tenant")] // Space
        [InlineData("-test-tenant")] // Starts with hyphen
        [InlineData("test-tenant-")] // Ends with hyphen
        [InlineData("TEST")] // All uppercase
        [InlineData("test@tenant")] // Special character
        public void Validate_InvalidIdentifierFormat_ShouldFail(string identifier)
        {
            // Arrange
            var command = new CreateTenantCommand(
                Identifier: identifier,
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Identifier)
                .WithErrorMessage("Tenant identifier must be lowercase alphanumeric with optional hyphens");
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("test")]
        [InlineData("test-tenant")]
        [InlineData("test-tenant-123")]
        [InlineData("a1b2c3")]
        [InlineData("tenant123")]
        [InlineData("123tenant")]
        public void Validate_ValidIdentifierFormats_ShouldPass(string identifier)
        {
            // Arrange
            var command = new CreateTenantCommand(
                Identifier: identifier,
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Identifier);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullName_ShouldFail(string? name)
        {
            // Arrange
            var command = new CreateTenantCommand(
                Identifier: "test-tenant",
                Name: name!,
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Tenant name is required");
        }

        [Theory]
        [InlineData("A")] // 1 character - below minimum
        public void Validate_NameTooShort_ShouldFail(string name)
        {
            // Arrange
            var command = new CreateTenantCommand(
                Identifier: "test-tenant",
                Name: name,
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Tenant name must be at least 2 characters");
        }

        [Fact]
        public void Validate_NameTooLong_ShouldFail()
        {
            // Arrange
            var longName = new string('a', 201);
            var command = new CreateTenantCommand(
                Identifier: "test-tenant",
                Name: longName,
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Tenant name cannot exceed 200 characters");
        }

        [Theory]
        [InlineData("AB")]
        [InlineData("Test Tenant")]
        [InlineData("Tenant Name With Spaces")]
        [InlineData("Tenant-Name-123")]
        [InlineData("Company & Partners")]
        public void Validate_ValidNameFormats_ShouldPass(string name)
        {
            // Arrange
            var command = new CreateTenantCommand(
                Identifier: "test-tenant",
                Name: name,
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Name);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Validate_IsActiveBothValues_ShouldPass(bool isActive)
        {
            // Arrange
            var command = new CreateTenantCommand(
                Identifier: "test-tenant",
                Name: "Test Tenant",
                IsActive: isActive);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    #endregion

    #region UpdateTenantCommandValidator Tests

    public class UpdateTenantCommandValidatorTests
    {
        private readonly UpdateTenantCommandValidator _validator;

        public UpdateTenantCommandValidatorTests()
        {
            _validator = new UpdateTenantCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new UpdateTenantCommand(
                TenantId: Guid.NewGuid(),
                Identifier: "updated-tenant",
                Name: "Updated Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyTenantId_ShouldFail()
        {
            // Arrange
            var command = new UpdateTenantCommand(
                TenantId: Guid.Empty,
                Identifier: "test-tenant",
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TenantId)
                .WithErrorMessage("Tenant ID is required");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullIdentifier_ShouldFail(string? identifier)
        {
            // Arrange
            var command = new UpdateTenantCommand(
                TenantId: Guid.NewGuid(),
                Identifier: identifier!,
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Identifier)
                .WithErrorMessage("Tenant identifier is required");
        }

        [Fact]
        public void Validate_IdentifierTooShort_ShouldFail()
        {
            // Arrange
            var command = new UpdateTenantCommand(
                TenantId: Guid.NewGuid(),
                Identifier: "a",
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Identifier)
                .WithErrorMessage("Tenant identifier must be at least 2 characters");
        }

        [Fact]
        public void Validate_IdentifierTooLong_ShouldFail()
        {
            // Arrange
            var longIdentifier = new string('a', 101);
            var command = new UpdateTenantCommand(
                TenantId: Guid.NewGuid(),
                Identifier: longIdentifier,
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Identifier)
                .WithErrorMessage("Tenant identifier cannot exceed 100 characters");
        }

        [Theory]
        [InlineData("Test-Tenant")]
        [InlineData("test_tenant")]
        [InlineData("-starts-with-hyphen")]
        public void Validate_InvalidIdentifierFormat_ShouldFail(string identifier)
        {
            // Arrange
            var command = new UpdateTenantCommand(
                TenantId: Guid.NewGuid(),
                Identifier: identifier,
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Identifier)
                .WithErrorMessage("Tenant identifier must be lowercase alphanumeric with optional hyphens");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Validate_EmptyOrNullName_ShouldFail(string? name)
        {
            // Arrange
            var command = new UpdateTenantCommand(
                TenantId: Guid.NewGuid(),
                Identifier: "test-tenant",
                Name: name!,
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Tenant name is required");
        }

        [Fact]
        public void Validate_NameTooShort_ShouldFail()
        {
            // Arrange
            var command = new UpdateTenantCommand(
                TenantId: Guid.NewGuid(),
                Identifier: "test-tenant",
                Name: "A",
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Tenant name must be at least 2 characters");
        }

        [Fact]
        public void Validate_NameTooLong_ShouldFail()
        {
            // Arrange
            var longName = new string('a', 201);
            var command = new UpdateTenantCommand(
                TenantId: Guid.NewGuid(),
                Identifier: "test-tenant",
                Name: longName,
                IsActive: true);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Name)
                .WithErrorMessage("Tenant name cannot exceed 200 characters");
        }

        [Fact]
        public void Validate_FromRequestMethod_ShouldCreateValidCommand()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var request = new UpdateTenantRequest(
                Identifier: "test-tenant",
                Name: "Test Tenant",
                IsActive: true);

            // Act
            var command = UpdateTenantCommand.FromRequest(tenantId, request);
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
            command.TenantId.Should().Be(tenantId);
            command.Identifier.Should().Be(request.Identifier);
            command.Name.Should().Be(request.Name);
            command.IsActive.Should().Be(request.IsActive);
        }
    }

    #endregion

    #region DeleteTenantCommandValidator Tests

    public class DeleteTenantCommandValidatorTests
    {
        private readonly DeleteTenantCommandValidator _validator;

        public DeleteTenantCommandValidatorTests()
        {
            _validator = new DeleteTenantCommandValidator(CreateLocalizationMock());
        }

        [Fact]
        public void Validate_ValidCommand_ShouldPass()
        {
            // Arrange
            var command = new DeleteTenantCommand(Guid.NewGuid());

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ValidCommandWithTenantName_ShouldPass()
        {
            // Arrange
            var command = new DeleteTenantCommand(Guid.NewGuid(), "Test Tenant");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_EmptyTenantId_ShouldFail()
        {
            // Arrange
            var command = new DeleteTenantCommand(Guid.Empty);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TenantId)
                .WithErrorMessage("Tenant ID is required");
        }

        [Fact]
        public void Validate_EmptyTenantIdWithTenantName_ShouldFail()
        {
            // Arrange
            var command = new DeleteTenantCommand(Guid.Empty, "Test Tenant");

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TenantId)
                .WithErrorMessage("Tenant ID is required");
        }

        [Fact]
        public void Validate_TenantNameIsOptional_ShouldPass()
        {
            // Arrange
            var command = new DeleteTenantCommand(Guid.NewGuid(), TenantName: null);

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    #endregion
}
