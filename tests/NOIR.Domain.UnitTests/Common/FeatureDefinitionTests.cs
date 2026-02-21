namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Unit tests for the FeatureDefinition record.
/// Tests constructor, default values, and record equality.
/// </summary>
public class FeatureDefinitionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        // Act
        var definition = new FeatureDefinition(
            "Ecommerce.Products",
            "features.ecommerce.products.name",
            "features.ecommerce.products.description");

        // Assert
        definition.Name.Should().Be("Ecommerce.Products");
        definition.DisplayNameKey.Should().Be("features.ecommerce.products.name");
        definition.DescriptionKey.Should().Be("features.ecommerce.products.description");
    }

    [Fact]
    public void DefaultEnabled_ShouldDefaultToTrue()
    {
        // Act
        var definition = new FeatureDefinition(
            "Ecommerce.Products",
            "features.ecommerce.products.name",
            "features.ecommerce.products.description");

        // Assert
        definition.DefaultEnabled.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithExplicitDefaultEnabled_ShouldSetValue()
    {
        // Act
        var definition = new FeatureDefinition(
            "Ecommerce.Products",
            "features.ecommerce.products.name",
            "features.ecommerce.products.description",
            DefaultEnabled: false);

        // Assert
        definition.DefaultEnabled.Should().BeFalse();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var definition1 = new FeatureDefinition(
            "Ecommerce.Products",
            "features.ecommerce.products.name",
            "features.ecommerce.products.description",
            true);

        var definition2 = new FeatureDefinition(
            "Ecommerce.Products",
            "features.ecommerce.products.name",
            "features.ecommerce.products.description",
            true);

        // Assert
        definition1.Should().Be(definition2);
    }

    [Fact]
    public void Inequality_ShouldWorkCorrectly()
    {
        // Arrange
        var definition1 = new FeatureDefinition(
            "Ecommerce.Products",
            "features.ecommerce.products.name",
            "features.ecommerce.products.description",
            true);

        var definition2 = new FeatureDefinition(
            "Ecommerce.Reviews",
            "features.ecommerce.reviews.name",
            "features.ecommerce.reviews.description",
            false);

        // Assert
        definition1.Should().NotBe(definition2);
    }

    #endregion
}
