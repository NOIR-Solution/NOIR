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
        category.ShouldNotBeNull();
        category.Id.ShouldNotBe(Guid.Empty);
        category.Name.ShouldBe("Electronics");
        category.Slug.ShouldBe("electronics");
        category.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        // Act
        var category = CreateTestCategory();

        // Assert
        category.ParentId.ShouldBeNull();
        category.SortOrder.ShouldBe(0);
        category.Description.ShouldBeNull();
        category.ImageUrl.ShouldBeNull();
        category.MetaTitle.ShouldBeNull();
        category.MetaDescription.ShouldBeNull();
        category.ProductCount.ShouldBe(0);
    }

    [Fact]
    public void Create_ShouldLowercaseSlug()
    {
        // Act
        var category = Domain.Entities.Product.ProductCategory.Create("Test", "MY-CATEGORY");

        // Assert
        category.Slug.ShouldBe("my-category");
    }

    [Fact]
    public void Create_WithParentId_ShouldSetParent()
    {
        // Arrange
        var parentId = Guid.NewGuid();

        // Act
        var category = CreateTestCategory(parentId: parentId);

        // Assert
        category.ParentId.ShouldBe(parentId);
    }

    [Fact]
    public void Create_ShouldRaiseProductCategoryCreatedEvent()
    {
        // Act
        var category = CreateTestCategory();

        // Assert
        category.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductCategoryCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectData()
    {
        // Act
        var category = CreateTestCategory(name: "Phones", slug: "phones");

        // Assert
        var domainEvent = category.DomainEvents.Single() as ProductCategoryCreatedEvent;
        domainEvent!.CategoryId.ShouldBe(category.Id);
        domainEvent.Name.ShouldBe("Phones");
        domainEvent.Slug.ShouldBe("phones");
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var category = CreateTestCategory(tenantId: null);

        // Assert
        category.TenantId.ShouldBeNull();
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
        category.Name.ShouldBe("Phones");
        category.Slug.ShouldBe("phones");
        category.Description.ShouldBe("All phones");
        category.ImageUrl.ShouldBe("https://img.jpg");
    }

    [Fact]
    public void UpdateDetails_ShouldLowercaseSlug()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        category.UpdateDetails("Phones", "PHONE-Category", null, null);

        // Assert
        category.Slug.ShouldBe("phone-category");
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
        category.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductCategoryUpdatedEvent>()
            .Name.ShouldBe("Updated");
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
        category.Description.ShouldBeNull();
        category.ImageUrl.ShouldBeNull();
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
        category.MetaTitle.ShouldBe("Electronics - Best Deals");
        category.MetaDescription.ShouldBe("Find the best electronics here");
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
        category.MetaTitle.ShouldBeNull();
        category.MetaDescription.ShouldBeNull();
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
        category.ParentId.ShouldBe(parentId);
    }

    [Fact]
    public void SetParent_WithNull_ShouldClearParent()
    {
        // Arrange
        var category = CreateTestCategory(parentId: Guid.NewGuid());

        // Act
        category.SetParent(null);

        // Assert
        category.ParentId.ShouldBeNull();
    }

    [Fact]
    public void SetParent_WithOwnId_ShouldThrow()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        var act = () => category.SetParent(category.Id);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("cannot be its own parent");
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
        category.SortOrder.ShouldBe(10);
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
        category.ProductCount.ShouldBe(50);
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
        category.ProductCount.ShouldBe(3);
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
        category.ProductCount.ShouldBe(4);
    }

    [Fact]
    public void DecrementProductCount_AtZero_ShouldNotGoBelowZero()
    {
        // Arrange
        var category = CreateTestCategory();

        // Act
        category.DecrementProductCount();

        // Assert
        category.ProductCount.ShouldBe(0);
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
        category.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ProductCategoryDeletedEvent>()
            .CategoryId.ShouldBe(category.Id);
    }

    #endregion
}
