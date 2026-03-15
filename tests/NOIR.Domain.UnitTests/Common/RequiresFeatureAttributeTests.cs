namespace NOIR.Domain.UnitTests.Common;

/// <summary>
/// Unit tests for the RequiresFeatureAttribute.
/// Tests constructor, feature storage, and attribute usage metadata.
/// </summary>
public class RequiresFeatureAttributeTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithSingleFeature_ShouldSetFeatures()
    {
        // Act
        var attribute = new RequiresFeatureAttribute("Ecommerce.Products");

        // Assert
        attribute.Features.Count().ShouldBe(1);
        attribute.Features.ShouldContain("Ecommerce.Products");
    }

    [Fact]
    public void Constructor_WithMultipleFeatures_ShouldSetAllFeatures()
    {
        // Act
        var attribute = new RequiresFeatureAttribute("Ecommerce.Products", "Ecommerce.Reviews", "Ecommerce.Cart");

        // Assert
        attribute.Features.Count().ShouldBe(3);
        attribute.Features.ShouldBe(new[] { "Ecommerce.Products", "Ecommerce.Reviews", "Ecommerce.Cart" });
    }

    [Fact]
    public void Constructor_WithEmptyArray_ShouldSetEmptyFeatures()
    {
        // Act
        var attribute = new RequiresFeatureAttribute();

        // Assert
        attribute.Features.ShouldBeEmpty();
    }

    #endregion

    #region AttributeUsage Tests

    [Fact]
    public void Attribute_ShouldTargetClass()
    {
        // Arrange
        var attributeUsage = typeof(RequiresFeatureAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        attributeUsage.ValidOn.ShouldBe(AttributeTargets.Class);
    }

    [Fact]
    public void Attribute_ShouldNotAllowMultiple()
    {
        // Arrange
        var attributeUsage = typeof(RequiresFeatureAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        attributeUsage.AllowMultiple.ShouldBeFalse();
    }

    [Fact]
    public void Attribute_ShouldBeInheritable()
    {
        // Arrange
        var attributeUsage = typeof(RequiresFeatureAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Assert
        attributeUsage.Inherited.ShouldBeTrue();
    }

    #endregion
}
