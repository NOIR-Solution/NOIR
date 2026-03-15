using NOIR.Domain.Entities.Product;
using NOIR.Domain.Events.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.ProductAttribute;

/// <summary>
/// Unit tests for the ProductAttribute aggregate root entity.
/// Tests factory methods, update methods, domain events, behavior flags,
/// display flags, type configuration, value management, and business rules.
/// </summary>
public class ProductAttributeTests
{
    private const string TestTenantId = "test-tenant";

    #region Helper Methods

    private static Domain.Entities.Product.ProductAttribute CreateTestAttribute(
        string code = "screen_size",
        string name = "Screen Size",
        AttributeType type = AttributeType.Text,
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Product.ProductAttribute.Create(code, name, type, tenantId);
    }

    private static Domain.Entities.Product.ProductAttribute CreateSelectAttribute(
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Product.ProductAttribute.Create("color", "Color", AttributeType.Select, tenantId);
    }

    private static Domain.Entities.Product.ProductAttribute CreateMultiSelectAttribute(
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Product.ProductAttribute.Create("features", "Features", AttributeType.MultiSelect, tenantId);
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidAttribute()
    {
        // Act
        var attr = CreateTestAttribute();

        // Assert
        attr.ShouldNotBeNull();
        attr.Id.ShouldNotBe(Guid.Empty);
        attr.Code.ShouldBe("screen_size");
        attr.Name.ShouldBe("Screen Size");
        attr.Type.ShouldBe(AttributeType.Text);
        attr.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var attr = CreateTestAttribute();

        // Assert
        attr.IsFilterable.ShouldBeFalse();
        attr.IsSearchable.ShouldBeFalse();
        attr.IsRequired.ShouldBeFalse();
        attr.IsVariantAttribute.ShouldBeFalse();
        attr.ShowInProductCard.ShouldBeFalse();
        attr.ShowInSpecifications.ShouldBeTrue();
        attr.IsActive.ShouldBeTrue();
        attr.IsGlobal.ShouldBeFalse();
        attr.SortOrder.ShouldBe(0);
        attr.Unit.ShouldBeNull();
        attr.ValidationRegex.ShouldBeNull();
        attr.MinValue.ShouldBeNull();
        attr.MaxValue.ShouldBeNull();
        attr.MaxLength.ShouldBeNull();
        attr.DefaultValue.ShouldBeNull();
        attr.Placeholder.ShouldBeNull();
        attr.HelpText.ShouldBeNull();
        attr.Values.ShouldBeEmpty();
    }

    [Fact]
    public void Create_ShouldNormalizeCode()
    {
        // Act
        var attr = Domain.Entities.Product.ProductAttribute.Create("Screen Size", "Screen Size", AttributeType.Text);

        // Assert
        attr.Code.ShouldBe("screen_size");
    }

    [Fact]
    public void Create_ShouldLowercaseCode()
    {
        // Act
        var attr = Domain.Entities.Product.ProductAttribute.Create("COLOR", "Color", AttributeType.Select);

        // Assert
        attr.Code.ShouldBe("color");
    }

    [Fact]
    public void Create_ShouldRaiseProductAttributeCreatedEvent()
    {
        // Act
        var attr = CreateTestAttribute();

        // Assert
        attr.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductAttributeCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectData()
    {
        // Act
        var attr = CreateTestAttribute(code: "ram", name: "RAM", type: AttributeType.Number);

        // Assert
        var domainEvent = attr.DomainEvents.Single() as ProductAttributeCreatedEvent;
        domainEvent!.AttributeId.ShouldBe(attr.Id);
        domainEvent.Code.ShouldBe("ram");
        domainEvent.Name.ShouldBe("RAM");
        domainEvent.Type.ShouldBe(AttributeType.Number);
    }

    [Theory]
    [InlineData(AttributeType.Select)]
    [InlineData(AttributeType.MultiSelect)]
    [InlineData(AttributeType.Text)]
    [InlineData(AttributeType.Number)]
    [InlineData(AttributeType.Boolean)]
    [InlineData(AttributeType.Date)]
    [InlineData(AttributeType.Color)]
    [InlineData(AttributeType.Range)]
    [InlineData(AttributeType.Url)]
    [InlineData(AttributeType.File)]
    public void Create_WithVariousTypes_ShouldSetType(AttributeType type)
    {
        // Act
        var attr = Domain.Entities.Product.ProductAttribute.Create("test", "Test", type);

        // Assert
        attr.Type.ShouldBe(type);
    }

    #endregion

    #region UpdateDetails Tests

    [Fact]
    public void UpdateDetails_ShouldUpdateCodeAndName()
    {
        // Arrange
        var attr = CreateTestAttribute();
        attr.ClearDomainEvents();

        // Act
        attr.UpdateDetails("Battery Size", "Battery Size");

        // Assert
        attr.Code.ShouldBe("battery_size");
        attr.Name.ShouldBe("Battery Size");
    }

    [Fact]
    public void UpdateDetails_ShouldNormalizeCode()
    {
        // Arrange
        var attr = CreateTestAttribute();

        // Act
        attr.UpdateDetails("RAM Capacity", "RAM");

        // Assert
        attr.Code.ShouldBe("ram_capacity");
    }

    [Fact]
    public void UpdateDetails_ShouldRaiseProductAttributeUpdatedEvent()
    {
        // Arrange
        var attr = CreateTestAttribute();
        attr.ClearDomainEvents();

        // Act
        attr.UpdateDetails("updated_code", "Updated Name");

        // Assert
        attr.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductAttributeUpdatedEvent>();
    }

    #endregion

    #region SetType Tests

    [Fact]
    public void SetType_ShouldChangeType()
    {
        // Arrange
        var attr = CreateTestAttribute(type: AttributeType.Text);

        // Act
        attr.SetType(AttributeType.Number);

        // Assert
        attr.Type.ShouldBe(AttributeType.Number);
    }

    #endregion

    #region SetBehaviorFlags Tests

    [Fact]
    public void SetBehaviorFlags_ShouldSetAllFlags()
    {
        // Arrange
        var attr = CreateTestAttribute();

        // Act
        attr.SetBehaviorFlags(
            isFilterable: true,
            isSearchable: true,
            isRequired: true,
            isVariantAttribute: true);

        // Assert
        attr.IsFilterable.ShouldBeTrue();
        attr.IsSearchable.ShouldBeTrue();
        attr.IsRequired.ShouldBeTrue();
        attr.IsVariantAttribute.ShouldBeTrue();
    }

    [Fact]
    public void SetBehaviorFlags_AllFalse_ShouldClearAllFlags()
    {
        // Arrange
        var attr = CreateTestAttribute();
        attr.SetBehaviorFlags(true, true, true, true);

        // Act
        attr.SetBehaviorFlags(false, false, false, false);

        // Assert
        attr.IsFilterable.ShouldBeFalse();
        attr.IsSearchable.ShouldBeFalse();
        attr.IsRequired.ShouldBeFalse();
        attr.IsVariantAttribute.ShouldBeFalse();
    }

    #endregion

    #region SetDisplayFlags Tests

    [Fact]
    public void SetDisplayFlags_ShouldSetBothFlags()
    {
        // Arrange
        var attr = CreateTestAttribute();

        // Act
        attr.SetDisplayFlags(showInProductCard: true, showInSpecifications: false);

        // Assert
        attr.ShowInProductCard.ShouldBeTrue();
        attr.ShowInSpecifications.ShouldBeFalse();
    }

    #endregion

    #region SetTypeConfiguration Tests

    [Fact]
    public void SetTypeConfiguration_ShouldSetAllValues()
    {
        // Arrange
        var attr = CreateTestAttribute(type: AttributeType.Number);

        // Act
        attr.SetTypeConfiguration("inch", @"^\d+$", 0m, 100m, null);

        // Assert
        attr.Unit.ShouldBe("inch");
        attr.ValidationRegex.ShouldBe(@"^\d+$");
        attr.MinValue.ShouldBe(0m);
        attr.MaxValue.ShouldBe(100m);
        attr.MaxLength.ShouldBeNull();
    }

    [Fact]
    public void SetTypeConfiguration_ForTextType_ShouldSetMaxLength()
    {
        // Arrange
        var attr = CreateTestAttribute(type: AttributeType.Text);

        // Act
        attr.SetTypeConfiguration(null, null, null, null, 255);

        // Assert
        attr.MaxLength.ShouldBe(255);
    }

    #endregion

    #region SetDefaults Tests

    [Fact]
    public void SetDefaults_ShouldSetAllValues()
    {
        // Arrange
        var attr = CreateTestAttribute();

        // Act
        attr.SetDefaults("Default Val", "Enter value...", "This is a help text");

        // Assert
        attr.DefaultValue.ShouldBe("Default Val");
        attr.Placeholder.ShouldBe("Enter value...");
        attr.HelpText.ShouldBe("This is a help text");
    }

    [Fact]
    public void SetDefaults_WithNulls_ShouldClearValues()
    {
        // Arrange
        var attr = CreateTestAttribute();
        attr.SetDefaults("value", "placeholder", "help");

        // Act
        attr.SetDefaults(null, null, null);

        // Assert
        attr.DefaultValue.ShouldBeNull();
        attr.Placeholder.ShouldBeNull();
        attr.HelpText.ShouldBeNull();
    }

    #endregion

    #region SetActive Tests

    [Fact]
    public void SetActive_False_ShouldDeactivate()
    {
        // Arrange
        var attr = CreateTestAttribute();

        // Act
        attr.SetActive(false);

        // Assert
        attr.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void SetActive_True_ShouldReactivate()
    {
        // Arrange
        var attr = CreateTestAttribute();
        attr.SetActive(false);

        // Act
        attr.SetActive(true);

        // Assert
        attr.IsActive.ShouldBeTrue();
    }

    #endregion

    #region SetSortOrder Tests

    [Fact]
    public void SetSortOrder_ShouldUpdateValue()
    {
        // Arrange
        var attr = CreateTestAttribute();

        // Act
        attr.SetSortOrder(5);

        // Assert
        attr.SortOrder.ShouldBe(5);
    }

    #endregion

    #region SetGlobal Tests

    [Fact]
    public void SetGlobal_True_ShouldMakeGlobal()
    {
        // Arrange
        var attr = CreateTestAttribute();

        // Act
        attr.SetGlobal(true);

        // Assert
        attr.IsGlobal.ShouldBeTrue();
    }

    [Fact]
    public void SetGlobal_False_ShouldRemoveGlobal()
    {
        // Arrange
        var attr = CreateTestAttribute();
        attr.SetGlobal(true);

        // Act
        attr.SetGlobal(false);

        // Assert
        attr.IsGlobal.ShouldBeFalse();
    }

    #endregion

    #region AddValue Tests

    [Fact]
    public void AddValue_ToSelectAttribute_ShouldAddValue()
    {
        // Arrange
        var attr = CreateSelectAttribute();

        // Act
        var value = attr.AddValue("red", "Red");

        // Assert
        attr.Values.ShouldHaveSingleItem();
        value.Value.ShouldBe("red");
        value.DisplayValue.ShouldBe("Red");
        value.AttributeId.ShouldBe(attr.Id);
    }

    [Fact]
    public void AddValue_ToMultiSelectAttribute_ShouldAddValue()
    {
        // Arrange
        var attr = CreateMultiSelectAttribute();

        // Act
        var value = attr.AddValue("wifi", "WiFi");

        // Assert
        attr.Values.ShouldHaveSingleItem();
        value.Value.ShouldBe("wifi");
    }

    [Fact]
    public void AddValue_MultipleValues_ShouldAddAll()
    {
        // Arrange
        var attr = CreateSelectAttribute();

        // Act
        attr.AddValue("red", "Red", 0);
        attr.AddValue("blue", "Blue", 1);
        attr.AddValue("green", "Green", 2);

        // Assert
        attr.Values.Count().ShouldBe(3);
    }

    [Fact]
    public void AddValue_ShouldRaiseDomainEvent()
    {
        // Arrange
        var attr = CreateSelectAttribute();
        attr.ClearDomainEvents();

        // Act
        var value = attr.AddValue("red", "Red");

        // Assert
        attr.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductAttributeValueAddedEvent>();

        var domainEvent = attr.DomainEvents.Single() as ProductAttributeValueAddedEvent;
        domainEvent!.AttributeId.ShouldBe(attr.Id);
        domainEvent.ValueId.ShouldBe(value.Id);
        domainEvent.Value.ShouldBe("red");
        domainEvent.DisplayValue.ShouldBe("Red");
    }

    [Fact]
    public void AddValue_ToTextAttribute_ShouldThrow()
    {
        // Arrange
        var attr = CreateTestAttribute(type: AttributeType.Text);

        // Act
        var act = () => attr.AddValue("value", "Value");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Select or MultiSelect");
    }

    [Theory]
    [InlineData(AttributeType.Number)]
    [InlineData(AttributeType.Boolean)]
    [InlineData(AttributeType.Date)]
    [InlineData(AttributeType.Color)]
    [InlineData(AttributeType.Range)]
    [InlineData(AttributeType.Url)]
    [InlineData(AttributeType.File)]
    public void AddValue_ToNonSelectTypes_ShouldThrow(AttributeType type)
    {
        // Arrange
        var attr = CreateTestAttribute(type: type);

        // Act
        var act = () => attr.AddValue("value", "Value");

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void AddValue_DuplicateValue_ShouldThrow()
    {
        // Arrange
        var attr = CreateSelectAttribute();
        attr.AddValue("red", "Red");

        // Act
        var act = () => attr.AddValue("red", "Red Variant");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("already exists");
    }

    [Fact]
    public void AddValue_DuplicateValueCaseInsensitive_ShouldThrow()
    {
        // Arrange
        var attr = CreateSelectAttribute();
        attr.AddValue("red", "Red");

        // Act
        var act = () => attr.AddValue("RED", "Red Upper");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("already exists");
    }

    [Fact]
    public void AddValue_ShouldSetTenantId()
    {
        // Arrange
        var attr = CreateSelectAttribute(tenantId: TestTenantId);

        // Act
        var value = attr.AddValue("red", "Red");

        // Assert
        value.TenantId.ShouldBe(TestTenantId);
    }

    #endregion

    #region RemoveValue Tests

    [Fact]
    public void RemoveValue_ExistingValue_ShouldRemove()
    {
        // Arrange
        var attr = CreateSelectAttribute();
        var value = attr.AddValue("red", "Red");

        // Act
        attr.RemoveValue(value.Id);

        // Assert
        attr.Values.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveValue_ShouldRaiseDomainEvent()
    {
        // Arrange
        var attr = CreateSelectAttribute();
        var value = attr.AddValue("red", "Red");
        attr.ClearDomainEvents();

        // Act
        attr.RemoveValue(value.Id);

        // Assert
        attr.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductAttributeValueRemovedEvent>();
    }

    [Fact]
    public void RemoveValue_NonExistingId_ShouldThrow()
    {
        // Arrange
        var attr = CreateSelectAttribute();

        // Act
        var act = () => attr.RemoveValue(Guid.NewGuid());

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("not found");
    }

    #endregion

    #region GetValue Tests

    [Fact]
    public void GetValue_ExistingId_ShouldReturnValue()
    {
        // Arrange
        var attr = CreateSelectAttribute();
        var added = attr.AddValue("red", "Red");

        // Act
        var retrieved = attr.GetValue(added.Id);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved!.Id.ShouldBe(added.Id);
    }

    [Fact]
    public void GetValue_NonExistingId_ShouldReturnNull()
    {
        // Arrange
        var attr = CreateSelectAttribute();

        // Act
        var retrieved = attr.GetValue(Guid.NewGuid());

        // Assert
        retrieved.ShouldBeNull();
    }

    #endregion

    #region RequiresValues Tests

    [Fact]
    public void RequiresValues_ForSelectType_ShouldBeTrue()
    {
        // Arrange
        var attr = CreateSelectAttribute();

        // Assert
        attr.RequiresValues.ShouldBeTrue();
    }

    [Fact]
    public void RequiresValues_ForMultiSelectType_ShouldBeTrue()
    {
        // Arrange
        var attr = CreateMultiSelectAttribute();

        // Assert
        attr.RequiresValues.ShouldBeTrue();
    }

    [Theory]
    [InlineData(AttributeType.Text)]
    [InlineData(AttributeType.Number)]
    [InlineData(AttributeType.Boolean)]
    [InlineData(AttributeType.Date)]
    [InlineData(AttributeType.Color)]
    [InlineData(AttributeType.Range)]
    [InlineData(AttributeType.Url)]
    [InlineData(AttributeType.File)]
    public void RequiresValues_ForNonSelectTypes_ShouldBeFalse(AttributeType type)
    {
        // Arrange
        var attr = CreateTestAttribute(type: type);

        // Assert
        attr.RequiresValues.ShouldBeFalse();
    }

    #endregion

    #region MarkAsDeleted Tests

    [Fact]
    public void MarkAsDeleted_ShouldRaiseProductAttributeDeletedEvent()
    {
        // Arrange
        var attr = CreateTestAttribute();
        attr.ClearDomainEvents();

        // Act
        attr.MarkAsDeleted();

        // Assert
        attr.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductAttributeDeletedEvent>()
            .AttributeId.ShouldBe(attr.Id);
    }

    #endregion
}
