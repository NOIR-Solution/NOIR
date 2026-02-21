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
        state.Should().NotBeNull();
        state.Id.Should().NotBe(Guid.Empty);
        state.FeatureName.Should().Be(featureName);
        state.IsAvailable.Should().BeTrue();
        state.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldSetFeatureName()
    {
        // Arrange
        var featureName = "Ecommerce.Reviews";

        // Act
        var state = TenantModuleState.Create(featureName);

        // Assert
        state.FeatureName.Should().Be("Ecommerce.Reviews");
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
        state.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void SetAvailability_False_ShouldSetIsAvailableFalse()
    {
        // Arrange
        var state = TenantModuleState.Create("Ecommerce");

        // Act
        state.SetAvailability(false);

        // Assert
        state.IsAvailable.Should().BeFalse();
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
        state.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void SetEnabled_False_ShouldSetIsEnabledFalse()
    {
        // Arrange
        var state = TenantModuleState.Create("Ecommerce");

        // Act
        state.SetEnabled(false);

        // Assert
        state.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region TenantEntity Inheritance Tests

    [Fact]
    public void Create_ShouldInheritTenantEntityProperties()
    {
        // Act
        var state = TenantModuleState.Create("Ecommerce");

        // Assert
        state.TenantId.Should().BeNull();
        state.IsDeleted.Should().BeFalse();
    }

    #endregion
}
