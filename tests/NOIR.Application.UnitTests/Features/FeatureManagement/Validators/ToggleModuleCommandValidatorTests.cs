using NOIR.Application.Features.FeatureManagement.Commands.ToggleModule;

namespace NOIR.Application.UnitTests.Features.FeatureManagement.Validators;

/// <summary>
/// Unit tests for ToggleModuleCommandValidator.
/// Tests all validation rules for toggling modules on/off.
/// </summary>
public class ToggleModuleCommandValidatorTests
{
    #region Test Setup

    private readonly Mock<IModuleCatalog> _catalogMock;
    private readonly ToggleModuleCommandValidator _validator;

    private const string ValidFeatureName = "Ecommerce.Products";

    public ToggleModuleCommandValidatorTests()
    {
        _catalogMock = new Mock<IModuleCatalog>();
        _catalogMock.Setup(x => x.Exists(ValidFeatureName)).Returns(true);
        _catalogMock.Setup(x => x.IsCore(ValidFeatureName)).Returns(false);

        _validator = new ToggleModuleCommandValidator(_catalogMock.Object);
    }

    #endregion

    #region Valid Command

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ToggleModuleCommand(ValidFeatureName, true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenDisablingModule_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ToggleModuleCommand(ValidFeatureName, false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region FeatureName Validation

    [Fact]
    public async Task Validate_WhenFeatureNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ToggleModuleCommand("", true);

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
        var command = new ToggleModuleCommand("NonExistent.Feature", true);

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
        var command = new ToggleModuleCommand(coreFeature, true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FeatureName)
            .WithErrorMessage("Core modules cannot be toggled.");
    }

    #endregion
}
