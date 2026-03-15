namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the TenantModuleState entity.
/// Tests creation, availability, enabled state, and inherited tenant entity properties.
/// </summary>
public class TenantModuleStateTests
{
    #region Create Tests

    [Fact]
    public void Create_ShouldCreateValidEntity()
    {
        // Arrange
        var featureName = "Ecommerce.Products";

        // Act
        var state = TenantModuleState.Create(featureName);

        // Assert
        state.ShouldNotBeNull();
        state.Id.ShouldNotBe(Guid.Empty);
        state.FeatureName.ShouldBe(featureName);
        state.IsAvailable.ShouldBeTrue();
        state.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldSetFeatureName()
    {
        // Arrange
        var featureName = "Ecommerce.Reviews";

        // Act
        var state = TenantModuleState.Create(featureName);

        // Assert
        state.FeatureName.ShouldBe("Ecommerce.Reviews");
    }

    #endregion

    #region SetAvailability Tests

    [Fact]
    public void SetAvailability_True_ShouldSetIsAvailableTrue()
    {
        // Arrange
        var state = TenantModuleState.Create("Ecommerce");
        state.SetAvailability(false); // ensure it's false first

        // Act
        state.SetAvailability(true);

        // Assert
        state.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void SetAvailability_False_ShouldSetIsAvailableFalse()
    {
        // Arrange
        var state = TenantModuleState.Create("Ecommerce");

        // Act
        state.SetAvailability(false);

        // Assert
        state.IsAvailable.ShouldBeFalse();
    }

    #endregion

    #region SetEnabled Tests

    [Fact]
    public void SetEnabled_True_ShouldSetIsEnabledTrue()
    {
        // Arrange
        var state = TenantModuleState.Create("Ecommerce");
        state.SetEnabled(false); // ensure it's false first

        // Act
        state.SetEnabled(true);

        // Assert
        state.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void SetEnabled_False_ShouldSetIsEnabledFalse()
    {
        // Arrange
        var state = TenantModuleState.Create("Ecommerce");

        // Act
        state.SetEnabled(false);

        // Assert
        state.IsEnabled.ShouldBeFalse();
    }

    #endregion

    #region TenantEntity Inheritance Tests

    [Fact]
    public void Create_ShouldInheritTenantEntityProperties()
    {
        // Act
        var state = TenantModuleState.Create("Ecommerce");

        // Assert
        state.TenantId.ShouldBeNull();
        state.IsDeleted.ShouldBeFalse();
    }

    #endregion
}
