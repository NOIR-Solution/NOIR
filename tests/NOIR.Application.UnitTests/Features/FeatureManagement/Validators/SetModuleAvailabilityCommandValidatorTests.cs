using NOIR.Application.Features.FeatureManagement.Commands.SetModuleAvailability;

namespace NOIR.Application.UnitTests.Features.FeatureManagement.Validators;

/// <summary>
/// Unit tests for SetModuleAvailabilityCommandValidator.
/// Tests all validation rules for setting module availability.
/// </summary>
public class SetModuleAvailabilityCommandValidatorTests
{
    #region Test Setup

    private readonly Mock<IModuleCatalog> _catalogMock;
    private readonly SetModuleAvailabilityCommandValidator _validator;

    private const string ValidTenantId = "test-tenant";
    private const string ValidFeatureName = "Ecommerce.Products";

    public SetModuleAvailabilityCommandValidatorTests()
    {
        _catalogMock = new Mock<IModuleCatalog>();
        _catalogMock.Setup(x => x.Exists(ValidFeatureName)).Returns(true);
        _catalogMock.Setup(x => x.IsCore(ValidFeatureName)).Returns(false);

        _validator = new SetModuleAvailabilityCommandValidator(_catalogMock.Object);
    }

    #endregion

    #region Valid Command

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new SetModuleAvailabilityCommand(ValidTenantId, ValidFeatureName, true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenSettingAvailabilityToFalse_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new SetModuleAvailabilityCommand(ValidTenantId, ValidFeatureName, false);

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
        var command = new SetModuleAvailabilityCommand("", ValidFeatureName, true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TenantId)
            .WithErrorMessage("Tenant ID is required.");
    }

    #endregion

    #region FeatureName Validation

    [Fact]
    public async Task Validate_WhenFeatureNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new SetModuleAvailabilityCommand(ValidTenantId, "", true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FeatureName)
            .WithErrorMessage("Feature name is required.");
    }

    [Fact]
    public async Task Validate_WhenFeatureDoesNotExistInCatalog_ShouldHaveError()
    {
        // Arrange
        _catalogMock.Setup(x => x.Exists("NonExistent.Feature")).Returns(false);
        var command = new SetModuleAvailabilityCommand(ValidTenantId, "NonExistent.Feature", true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FeatureName)
            .WithErrorMessage("Feature not found in catalog.");
    }

    [Fact]
    public async Task Validate_WhenFeatureIsCoreModule_ShouldHaveError()
    {
        // Arrange
        const string coreFeature = "Auth";
        _catalogMock.Setup(x => x.Exists(coreFeature)).Returns(true);
        _catalogMock.Setup(x => x.IsCore(coreFeature)).Returns(true);
        var command = new SetModuleAvailabilityCommand(ValidTenantId, coreFeature, false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FeatureName)
            .WithErrorMessage("Core modules cannot be modified.");
    }

    #endregion
}
