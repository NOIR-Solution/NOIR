using NOIR.Domain.Entities.Product;
using NOIR.Domain.Events.Product;

namespace NOIR.Domain.UnitTests.Entities.Product.ProductCategory;

/// <summary>
/// Unit tests for the ProductCategory aggregate root entity.
/// Tests factory methods, update methods, domain events, hierarchy management,
/// product count management, and deletion marking.
/// </summary>
public class ProductCategoryTests
{
    private const string TestTenantId = "test-tenant";

    #region Helper Methods

    private static Domain.Entities.Product.ProductCategory CreateTestCategory(
        string name = "Electronics",
        string slug = "electronics",
        Guid? parentId = null,
        string? tenantId = TestTenantId)
    {
        return Domain.Entities.Product.ProductCategory.Create(name, slug, parentId, tenantId);
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidCategory()
    {
        // Act
        var category = CreateTestCategory();

        // Assert
        category.Should().NotBeNull();
        category.Id.Should().NotBe(Guid.Empty);
        category.Name.Should().Be("Electronics");
        category.Slug.Should().Be("electronics");
        category.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var category = CreateTestCategory();

        // Assert
        category.ParentId.Should().BeNull();
        category.SortOrder.Should().Be(0);
        category.Description.Should().BeNull();
        category.ImageUrl.Should().BeNull();
        category.MetaTitle.Should().BeNull();
        category.MetaDescription.Should().BeNull();
        category.ProductCount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldLowercaseSlug()
    {
        // Act
        var category = Domain.Entities.Product.ProductCategory.Create("Test", "MY-CATEGORY");

        // Assert
        category.Slug.Should().Be("my-category");
    }

    [Fact]
    public void Create_WithParentId_ShouldSetParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var category = CreateTestCategory(parentId: parentId);

        // Assert
        category.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void Create_ShouldRaiseProductCategoryCreatedEvent()
    {
        // Act
        var category = CreateTestCategory();

        // Assert
        category.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCategoryCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectData()
    {
        // Act
        var category = CreateTestCategory(name: "Phones", slug: "phones");

        // Assert
        var domainEvent = category.DomainEvents.Single() as ProductCategoryCreatedEvent;
        domainEvent!.CategoryId.Should().Be(category.Id);
        domainEvent.Name.Should().Be("Phones");
        domainEvent.Slug.Should().Be("phones");
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var category = CreateTestCategory(tenantId: null);

        // Assert
        category.TenantId.Should().BeNull();
    }

    #endregion

    #region UpdateDetails Tests

    [Fact]
    public void UpdateDetails_ShouldUpdateAllFields()
    {
        // Arrange
        var category = CreateTestCategory();
        category.ClearDomainEvents();

        // Act
        category.UpdateDetails("Phones", "phones", "All phones", "https://img.jpg");

        // Assert
        category.Name.Should().Be("Phones");
        category.Slug.Should().Be("phones");
        category.Description.Should().Be("All phones");
        category.ImageUrl.Should().Be("https://img.jpg");
    }

    [Fact]
    public void UpdateDetails_ShouldLowercaseSlug()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        category.UpdateDetails("Phones", "PHONE-Category", null, null);

        // Assert
        category.Slug.Should().Be("phone-category");
    }

    [Fact]
    public void UpdateDetails_ShouldRaiseProductCategoryUpdatedEvent()
    {
        // Arrange
        var category = CreateTestCategory();
        category.ClearDomainEvents();

        // Act
        category.UpdateDetails("Updated", "updated");

        // Assert
        category.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCategoryUpdatedEvent>()
            .Which.Name.Should().Be("Updated");
    }

    [Fact]
    public void UpdateDetails_WithNullOptionalFields_ShouldSetNulls()
    {
        // Arrange
        var category = CreateTestCategory();
        category.UpdateDetails("Cat", "cat", "Desc", "https://img.jpg");

        // Act
        category.UpdateDetails("Cat", "cat", null, null);

        // Assert
        category.Description.Should().BeNull();
        category.ImageUrl.Should().BeNull();
    }

    #endregion

    #region UpdateSeo Tests

    [Fact]
    public void UpdateSeo_ShouldSetMetaFields()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        category.UpdateSeo("Electronics - Best Deals", "Find the best electronics here");

        // Assert
        category.MetaTitle.Should().Be("Electronics - Best Deals");
        category.MetaDescription.Should().Be("Find the best electronics here");
    }

    [Fact]
    public void UpdateSeo_WithNulls_ShouldClearSeo()
    {
        // Arrange
        var category = CreateTestCategory();
        category.UpdateSeo("Title", "Description");

        // Act
        category.UpdateSeo(null, null);

        // Assert
        category.MetaTitle.Should().BeNull();
        category.MetaDescription.Should().BeNull();
    }

    #endregion

    #region SetParent Tests

    [Fact]
    public void SetParent_WithValidParentId_ShouldSetParent()
    {
        // Arrange
        var category = CreateTestCategory();
        var parentId = Guid.NewGuid();

        // Act
        category.SetParent(parentId);

        // Assert
        category.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void SetParent_WithNull_ShouldClearParent()
    {
        // Arrange
        var category = CreateTestCategory(parentId: Guid.NewGuid());

        // Act
        category.SetParent(null);

        // Assert
        category.ParentId.Should().BeNull();
    }

    [Fact]
    public void SetParent_WithOwnId_ShouldThrow()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        var act = () => category.SetParent(category.Id);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be its own parent*");
    }

    #endregion

    #region SetSortOrder Tests

    [Fact]
    public void SetSortOrder_ShouldUpdateValue()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        category.SetSortOrder(10);

        // Assert
        category.SortOrder.Should().Be(10);
    }

    #endregion

    #region ProductCount Tests

    [Fact]
    public void UpdateProductCount_ShouldSetCount()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        category.UpdateProductCount(50);

        // Assert
        category.ProductCount.Should().Be(50);
    }

    [Fact]
    public void IncrementProductCount_ShouldIncrementByOne()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        category.IncrementProductCount();
        category.IncrementProductCount();
        category.IncrementProductCount();

        // Assert
        category.ProductCount.Should().Be(3);
    }

    [Fact]
    public void DecrementProductCount_ShouldDecrementByOne()
    {
        // Arrange
        var category = CreateTestCategory();
        category.UpdateProductCount(5);

        // Act
        category.DecrementProductCount();

        // Assert
        category.ProductCount.Should().Be(4);
    }

    [Fact]
    public void DecrementProductCount_AtZero_ShouldNotGoBelowZero()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        category.DecrementProductCount();

        // Assert
        category.ProductCount.Should().Be(0);
    }

    #endregion

    #region MarkAsDeleted Tests

    [Fact]
    public void MarkAsDeleted_ShouldRaiseProductCategoryDeletedEvent()
    {
        // Arrange
        var category = CreateTestCategory();
        category.ClearDomainEvents();

        // Act
        category.MarkAsDeleted();

        // Assert
        category.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCategoryDeletedEvent>()
            .Which.CategoryId.Should().Be(category.Id);
    }

    #endregion
}
