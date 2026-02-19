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
        attr.Should().NotBeNull();
        attr.Id.Should().NotBe(Guid.Empty);
        attr.Code.Should().Be("screen_size");
        attr.Name.Should().Be("Screen Size");
        attr.Type.Should().Be(AttributeType.Text);
        attr.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var attr = CreateTestAttribute();

        // Assert
        attr.IsFilterable.Should().BeFalse();
        attr.IsSearchable.Should().BeFalse();
        attr.IsRequired.Should().BeFalse();
        attr.IsVariantAttribute.Should().BeFalse();
        attr.ShowInProductCard.Should().BeFalse();
        attr.ShowInSpecifications.Should().BeTrue();
        attr.IsActive.Should().BeTrue();
        attr.IsGlobal.Should().BeFalse();
        attr.SortOrder.Should().Be(0);
        attr.Unit.Should().BeNull();
        attr.ValidationRegex.Should().BeNull();
        attr.MinValue.Should().BeNull();
        attr.MaxValue.Should().BeNull();
        attr.MaxLength.Should().BeNull();
        attr.DefaultValue.Should().BeNull();
        attr.Placeholder.Should().BeNull();
        attr.HelpText.Should().BeNull();
        attr.Values.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldNormalizeCode()
    {
        // Act
        var attr = Domain.Entities.Product.ProductAttribute.Create("Screen Size", "Screen Size", AttributeType.Text);

        // Assert
        attr.Code.Should().Be("screen_size");
    }

    [Fact]
    public void Create_ShouldLowercaseCode()
    {
        // Act
        var attr = Domain.Entities.Product.ProductAttribute.Create("COLOR", "Color", AttributeType.Select);

        // Assert
        attr.Code.Should().Be("color");
    }

    [Fact]
    public void Create_ShouldRaiseProductAttributeCreatedEvent()
    {
        // Act
        var attr = CreateTestAttribute();

        // Assert
        attr.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductAttributeCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectData()
    {
        // Act
        var attr = CreateTestAttribute(code: "ram", name: "RAM", type: AttributeType.Number);

        // Assert
        var domainEvent = attr.DomainEvents.Single() as ProductAttributeCreatedEvent;
        domainEvent!.AttributeId.Should().Be(attr.Id);
        domainEvent.Code.Should().Be("ram");
        domainEvent.Name.Should().Be("RAM");
        domainEvent.Type.Should().Be(AttributeType.Number);
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
        attr.Type.Should().Be(type);
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
        attr.Code.Should().Be("battery_size");
        attr.Name.Should().Be("Battery Size");
    }

    [Fact]
    public void UpdateDetails_ShouldNormalizeCode()
    {
        // Arrange
        var attr = CreateTestAttribute();

        // Act
        attr.UpdateDetails("RAM Capacity", "RAM");

        // Assert
        attr.Code.Should().Be("ram_capacity");
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
        attr.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductAttributeUpdatedEvent>();
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
        attr.Type.Should().Be(AttributeType.Number);
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
        attr.IsFilterable.Should().BeTrue();
        attr.IsSearchable.Should().BeTrue();
        attr.IsRequired.Should().BeTrue();
        attr.IsVariantAttribute.Should().BeTrue();
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
        attr.IsFilterable.Should().BeFalse();
        attr.IsSearchable.Should().BeFalse();
        attr.IsRequired.Should().BeFalse();
        attr.IsVariantAttribute.Should().BeFalse();
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
        attr.ShowInProductCard.Should().BeTrue();
        attr.ShowInSpecifications.Should().BeFalse();
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
        attr.Unit.Should().Be("inch");
        attr.ValidationRegex.Should().Be(@"^\d+$");
        attr.MinValue.Should().Be(0m);
        attr.MaxValue.Should().Be(100m);
        attr.MaxLength.Should().BeNull();
    }

    [Fact]
    public void SetTypeConfiguration_ForTextType_ShouldSetMaxLength()
    {
        // Arrange
        var attr = CreateTestAttribute(type: AttributeType.Text);

        // Act
        attr.SetTypeConfiguration(null, null, null, null, 255);

        // Assert
        attr.MaxLength.Should().Be(255);
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
        attr.DefaultValue.Should().Be("Default Val");
        attr.Placeholder.Should().Be("Enter value...");
        attr.HelpText.Should().Be("This is a help text");
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
        attr.DefaultValue.Should().BeNull();
        attr.Placeholder.Should().BeNull();
        attr.HelpText.Should().BeNull();
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
        attr.IsActive.Should().BeFalse();
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
        attr.IsActive.Should().BeTrue();
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
        attr.SortOrder.Should().Be(5);
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
        attr.IsGlobal.Should().BeTrue();
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
        attr.IsGlobal.Should().BeFalse();
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
        attr.Values.Should().ContainSingle();
        value.Value.Should().Be("red");
        value.DisplayValue.Should().Be("Red");
        value.AttributeId.Should().Be(attr.Id);
    }

    [Fact]
    public void AddValue_ToMultiSelectAttribute_ShouldAddValue()
    {
        // Arrange
        var attr = CreateMultiSelectAttribute();

        // Act
        var value = attr.AddValue("wifi", "WiFi");

        // Assert
        attr.Values.Should().ContainSingle();
        value.Value.Should().Be("wifi");
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
        attr.Values.Should().HaveCount(3);
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
        attr.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductAttributeValueAddedEvent>();

        var domainEvent = attr.DomainEvents.Single() as ProductAttributeValueAddedEvent;
        domainEvent!.AttributeId.Should().Be(attr.Id);
        domainEvent.ValueId.Should().Be(value.Id);
        domainEvent.Value.Should().Be("red");
        domainEvent.DisplayValue.Should().Be("Red");
    }

    [Fact]
    public void AddValue_ToTextAttribute_ShouldThrow()
    {
        // Arrange
        var attr = CreateTestAttribute(type: AttributeType.Text);

        // Act
        var act = () => attr.AddValue("value", "Value");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Select or MultiSelect*");
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
        act.Should().Throw<InvalidOperationException>();
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already exists*");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void AddValue_ShouldSetTenantId()
    {
        // Arrange
        var attr = CreateSelectAttribute(tenantId: TestTenantId);

        // Act
        var value = attr.AddValue("red", "Red");

        // Assert
        value.TenantId.Should().Be(TestTenantId);
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
        attr.Values.Should().BeEmpty();
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
        attr.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductAttributeValueRemovedEvent>();
    }

    [Fact]
    public void RemoveValue_NonExistingId_ShouldThrow()
    {
        // Arrange
        var attr = CreateSelectAttribute();

        // Act
        var act = () => attr.RemoveValue(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found*");
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
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(added.Id);
    }

    [Fact]
    public void GetValue_NonExistingId_ShouldReturnNull()
    {
        // Arrange
        var attr = CreateSelectAttribute();

        // Act
        var retrieved = attr.GetValue(Guid.NewGuid());

        // Assert
        retrieved.Should().BeNull();
    }

    #endregion

    #region RequiresValues Tests

    [Fact]
    public void RequiresValues_ForSelectType_ShouldBeTrue()
    {
        // Arrange
        var attr = CreateSelectAttribute();

        // Assert
        attr.RequiresValues.Should().BeTrue();
    }

    [Fact]
    public void RequiresValues_ForMultiSelectType_ShouldBeTrue()
    {
        // Arrange
        var attr = CreateMultiSelectAttribute();

        // Assert
        attr.RequiresValues.Should().BeTrue();
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
        attr.RequiresValues.Should().BeFalse();
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
        attr.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductAttributeDeletedEvent>()
            .Which.AttributeId.Should().Be(attr.Id);
    }

    #endregion
}
