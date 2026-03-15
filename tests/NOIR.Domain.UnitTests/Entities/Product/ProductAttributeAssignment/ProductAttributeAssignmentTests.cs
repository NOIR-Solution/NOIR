using NOIR.Domain.Entities.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.ProductAttributeAssignment;

/// <summary>
/// Unit tests for the ProductAttributeAssignment entity.
/// Tests factory methods, polymorphic value setters, value clearing behavior,
/// typed value retrieval, multi-select serialization, and HasValue computed property.
/// </summary>
public class ProductAttributeAssignmentTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestProductId = Guid.NewGuid();
    private static readonly Guid TestAttributeId = Guid.NewGuid();

    #region Helper Methods

    private static Domain.Entities.Product.ProductAttributeAssignment CreateTestAssignment(
        Guid? productId = null,
        Guid? attributeId = null,
        Guid? variantId = null,
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Product.ProductAttributeAssignment.Create(
            productId ?? TestProductId,
            attributeId ?? TestAttributeId,
            variantId,
            tenantId);
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidAssignment()
    {
        // Act
        var assignment = CreateTestAssignment();

        // Assert
        assignment.ShouldNotBeNull();
        assignment.Id.ShouldNotBe(Guid.Empty);
        assignment.ProductId.ShouldBe(TestProductId);
        assignment.AttributeId.ShouldBe(TestAttributeId);
        assignment.VariantId.ShouldBeNull();
        assignment.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_WithVariantId_ShouldSetVariantId()
    {
        // Arrange
        var variantId = Guid.NewGuid();

        // Act
        var assignment = CreateTestAssignment(variantId: variantId);

        // Assert
        assignment.VariantId.ShouldBe(variantId);
    }

    [Fact]
    public void Create_ShouldHaveNoValueSet()
    {
        // Act
        var assignment = CreateTestAssignment();

        // Assert
        assignment.HasValue.ShouldBeFalse();
        assignment.DisplayValue.ShouldBeNull();
        assignment.GetTypedValue().ShouldBeNull();
    }

    #endregion

    #region SetSelectValue Tests

    [Fact]
    public void SetSelectValue_ShouldSetAttributeValueIdAndDisplayValue()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        var valueId = Guid.NewGuid();

        // Act
        assignment.SetSelectValue(valueId, "Red");

        // Assert
        assignment.AttributeValueId.ShouldBe(valueId);
        assignment.DisplayValue.ShouldBe("Red");
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void SetSelectValue_ShouldClearOtherValues()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        assignment.SetTextValue("some text");

        // Act
        assignment.SetSelectValue(Guid.NewGuid(), "Red");

        // Assert
        assignment.TextValue.ShouldBeNull();
        assignment.AttributeValueId.ShouldNotBeNull();
    }

    [Fact]
    public void SetSelectValue_GetTypedValue_ShouldReturnGuid()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        var valueId = Guid.NewGuid();
        assignment.SetSelectValue(valueId, "Red");

        // Act
        var typed = assignment.GetTypedValue();

        // Assert
        typed.ShouldBe(valueId);
    }

    #endregion

    #region SetMultiSelectValue Tests

    [Fact]
    public void SetMultiSelectValue_ShouldSerializeValueIds()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        assignment.SetMultiSelectValue(ids, "Red, Blue");

        // Assert
        assignment.AttributeValueIds.ShouldNotBeNullOrEmpty();
        assignment.DisplayValue.ShouldBe("Red, Blue");
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void SetMultiSelectValue_GetMultiSelectValueIds_ShouldDeserializeCorrectly()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var ids = new List<Guid> { id1, id2 };

        // Act
        assignment.SetMultiSelectValue(ids, "Red, Blue");
        var result = assignment.GetMultiSelectValueIds();

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain(id1);
        result.ShouldContain(id2);
    }

    [Fact]
    public void GetMultiSelectValueIds_WhenNoValueSet_ShouldReturnEmptyList()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        var result = assignment.GetMultiSelectValueIds();

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region SetTextValue Tests

    [Fact]
    public void SetTextValue_ShouldSetTextAndDisplayValue()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        assignment.SetTextValue("High resolution display");

        // Assert
        assignment.TextValue.ShouldBe("High resolution display");
        assignment.DisplayValue.ShouldBe("High resolution display");
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void SetTextValue_ShouldClearOtherValues()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        assignment.SetNumberValue(42m);

        // Act
        assignment.SetTextValue("text value");

        // Assert
        assignment.NumberValue.ShouldBeNull();
        assignment.TextValue.ShouldBe("text value");
    }

    [Fact]
    public void SetTextValue_GetTypedValue_ShouldReturnString()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        assignment.SetTextValue("hello");

        // Act
        var typed = assignment.GetTypedValue();

        // Assert
        typed.ShouldBe("hello");
    }

    #endregion

    #region SetNumberValue Tests

    [Fact]
    public void SetNumberValue_ShouldSetNumberAndDisplayValue()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        assignment.SetNumberValue(6.7m);

        // Assert
        assignment.NumberValue.ShouldBe(6.7m);
        assignment.DisplayValue.ShouldBe("6.7");
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void SetNumberValue_WithUnit_ShouldIncludeUnitInDisplay()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        assignment.SetNumberValue(6.7m, "inch");

        // Assert
        assignment.DisplayValue.ShouldBe("6.7 inch");
    }

    [Fact]
    public void SetNumberValue_WithoutUnit_ShouldShowNumberOnly()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        assignment.SetNumberValue(100m);

        // Assert
        assignment.DisplayValue.ShouldBe("100");
    }

    [Fact]
    public void SetNumberValue_GetTypedValue_ShouldReturnDecimal()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        assignment.SetNumberValue(42.5m);

        // Act
        var typed = assignment.GetTypedValue();

        // Assert
        typed.ShouldBe(42.5m);
    }

    #endregion

    #region SetBoolValue Tests

    [Fact]
    public void SetBoolValue_True_ShouldSetYes()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        assignment.SetBoolValue(true);

        // Assert
        assignment.BoolValue.ShouldBe(true);
        assignment.DisplayValue.ShouldBe("Yes");
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void SetBoolValue_False_ShouldSetNo()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        assignment.SetBoolValue(false);

        // Assert
        assignment.BoolValue.ShouldBe(false);
        assignment.DisplayValue.ShouldBe("No");
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void SetBoolValue_GetTypedValue_ShouldReturnBool()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        assignment.SetBoolValue(true);

        // Act
        var typed = assignment.GetTypedValue();

        // Assert
        typed.ShouldBe(true);
    }

    #endregion

    #region SetDateValue Tests

    [Fact]
    public void SetDateValue_ShouldSetDateAndFormatDisplay()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        var date = new DateTime(2025, 6, 15);

        // Act
        assignment.SetDateValue(date);

        // Assert
        assignment.DateValue.ShouldBe(date.Date);
        assignment.DisplayValue.ShouldBe("2025-06-15");
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void SetDateValue_ShouldStripTimeComponent()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        var dateTime = new DateTime(2025, 6, 15, 14, 30, 0);

        // Act
        assignment.SetDateValue(dateTime);

        // Assert
        assignment.DateValue.ShouldBe(new DateTime(2025, 6, 15));
    }

    [Fact]
    public void SetDateValue_GetTypedValue_ShouldReturnDateTime()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        var date = new DateTime(2025, 1, 1);
        assignment.SetDateValue(date);

        // Act
        var typed = assignment.GetTypedValue();

        // Assert
        typed.ShouldBe(date.Date);
    }

    #endregion

    #region SetDateTimeValue Tests

    [Fact]
    public void SetDateTimeValue_ShouldSetDateTimeAndFormatDisplay()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        var dateTime = new DateTime(2025, 6, 15, 14, 30, 45);

        // Act
        assignment.SetDateTimeValue(dateTime);

        // Assert
        assignment.DateTimeValue.ShouldBe(dateTime);
        assignment.DisplayValue.ShouldBe("2025-06-15 14:30:45");
        assignment.HasValue.ShouldBeTrue();
    }

    #endregion

    #region SetColorValue Tests

    [Fact]
    public void SetColorValue_ShouldSetColorAndDisplayValue()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        assignment.SetColorValue("#FF0000");

        // Assert
        assignment.ColorValue.ShouldBe("#FF0000");
        assignment.DisplayValue.ShouldBe("#FF0000");
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void SetColorValue_GetTypedValue_ShouldReturnString()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        assignment.SetColorValue("#00FF00");

        // Act
        var typed = assignment.GetTypedValue();

        // Assert
        typed.ShouldBe("#00FF00");
    }

    #endregion

    #region SetRangeValue Tests

    [Fact]
    public void SetRangeValue_ShouldSetMinMaxAndDisplayValue()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        assignment.SetRangeValue(10m, 100m);

        // Assert
        assignment.MinRangeValue.ShouldBe(10m);
        assignment.MaxRangeValue.ShouldBe(100m);
        assignment.DisplayValue.ShouldBe("10 - 100");
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void SetRangeValue_WithUnit_ShouldIncludeUnitInDisplay()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        assignment.SetRangeValue(10m, 100m, "kg");

        // Assert
        assignment.DisplayValue.ShouldBe("10 - 100 kg");
    }

    #endregion

    #region SetFileValue Tests

    [Fact]
    public void SetFileValue_ShouldSetFileUrlAndDisplayValue()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act
        assignment.SetFileValue("https://example.com/spec.pdf");

        // Assert
        assignment.FileUrl.ShouldBe("https://example.com/spec.pdf");
        assignment.DisplayValue.ShouldBe("https://example.com/spec.pdf");
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void SetFileValue_GetTypedValue_ShouldReturnUrl()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        assignment.SetFileValue("https://example.com/doc.pdf");

        // Act
        var typed = assignment.GetTypedValue();

        // Assert
        typed.ShouldBe("https://example.com/doc.pdf");
    }

    #endregion

    #region Value Clearing Tests

    [Fact]
    public void SettingNewValue_ShouldClearPreviousValue()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Act - Set various values in sequence
        assignment.SetTextValue("text");
        assignment.TextValue.ShouldBe("text");

        assignment.SetNumberValue(42m);
        assignment.TextValue.ShouldBeNull();
        assignment.NumberValue.ShouldBe(42m);

        assignment.SetBoolValue(true);
        assignment.NumberValue.ShouldBeNull();
        assignment.BoolValue.ShouldBe(true);

        assignment.SetColorValue("#000");
        assignment.BoolValue.ShouldBeNull();
        assignment.ColorValue.ShouldBe("#000");

        assignment.SetFileValue("file.pdf");
        assignment.ColorValue.ShouldBeNull();
        assignment.FileUrl.ShouldBe("file.pdf");

        assignment.SetRangeValue(1m, 10m);
        assignment.FileUrl.ShouldBeNull();
        assignment.MinRangeValue.ShouldBe(1m);
    }

    #endregion

    #region HasValue Tests

    [Fact]
    public void HasValue_WhenNoValueSet_ShouldBeFalse()
    {
        // Arrange
        var assignment = CreateTestAssignment();

        // Assert
        assignment.HasValue.ShouldBeFalse();
    }

    [Fact]
    public void HasValue_WhenSelectValueSet_ShouldBeTrue()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        assignment.SetSelectValue(Guid.NewGuid(), "Red");

        // Assert
        assignment.HasValue.ShouldBeTrue();
    }

    [Fact]
    public void HasValue_WhenBoolValueSetToFalse_ShouldBeTrue()
    {
        // Arrange - Even false is a "value set"
        var assignment = CreateTestAssignment();
        assignment.SetBoolValue(false);

        // Assert
        assignment.HasValue.ShouldBeTrue();
    }

    #endregion

    #region GetTypedValue Range Tests

    [Fact]
    public void GetTypedValue_ForRangeValue_ShouldReturnAnonymousObject()
    {
        // Arrange
        var assignment = CreateTestAssignment();
        assignment.SetRangeValue(5m, 50m);

        // Act
        var typed = assignment.GetTypedValue();

        // Assert
        typed.ShouldNotBeNull();
        // Anonymous type - check via dynamic
        var json = System.Text.Json.JsonSerializer.Serialize(typed);
        json.ShouldContain("Min");
        json.ShouldContain("Max");
    }

    #endregion
}
