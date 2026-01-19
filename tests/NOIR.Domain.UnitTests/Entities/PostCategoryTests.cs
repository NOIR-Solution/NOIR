namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the PostCategory entity.
/// Tests factory methods, hierarchy, and post count management.
/// </summary>
public class PostCategoryTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidCategory()
    {
        // Arrange
        var name = "Technology";
        var slug = "technology";

        // Act
        var category = PostCategory.Create(name, slug);

        // Assert
        category.Should().NotBeNull();
        category.Id.Should().NotBe(Guid.Empty);
        category.Name.Should().Be(name);
        category.Slug.Should().Be(slug);
        category.SortOrder.Should().Be(0);
        category.PostCount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldLowercaseSlug()
    {
        // Act
        var category = PostCategory.Create("Technology", "TECHNOLOGY");

        // Assert
        category.Slug.Should().Be("technology");
    }

    [Fact]
    public void Create_WithOptionalParameters_ShouldSetAllProperties()
    {
        // Arrange
        var name = "Programming";
        var slug = "programming";
        var description = "All about programming";
        var parentId = Guid.NewGuid();
        var tenantId = "tenant-123";

        // Act
        var category = PostCategory.Create(name, slug, description, parentId, tenantId);

        // Assert
        category.Description.Should().Be(description);
        category.ParentId.Should().Be(parentId);
        category.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithoutParentId_ShouldBeTopLevelCategory()
    {
        // Act
        var category = PostCategory.Create("Top Level", "top-level");

        // Assert
        category.ParentId.Should().BeNull();
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_ShouldModifyBasicProperties()
    {
        // Arrange
        var category = PostCategory.Create("Original", "original");
        var newName = "Updated Name";
        var newSlug = "updated-name";
        var newDescription = "New description";

        // Act
        category.Update(newName, newSlug, newDescription);

        // Assert
        category.Name.Should().Be(newName);
        category.Slug.Should().Be("updated-name");
        category.Description.Should().Be(newDescription);
    }

    [Fact]
    public void Update_ShouldLowercaseSlug()
    {
        // Arrange
        var category = PostCategory.Create("Original", "original");

        // Act
        category.Update("Updated", "UPDATED-SLUG");

        // Assert
        category.Slug.Should().Be("updated-slug");
    }

    [Fact]
    public void Update_WithParentId_ShouldChangeParent()
    {
        // Arrange
        var category = PostCategory.Create("Child", "child");
        var newParentId = Guid.NewGuid();

        // Act
        category.Update("Child", "child", null, newParentId);

        // Assert
        category.ParentId.Should().Be(newParentId);
    }

    [Fact]
    public void Update_WithNullParentId_ShouldMakeTopLevel()
    {
        // Arrange
        var category = PostCategory.Create("Child", "child", null, Guid.NewGuid());

        // Act
        category.Update("Child", "child", null, null);

        // Assert
        category.ParentId.Should().BeNull();
    }

    #endregion

    #region SEO Tests

    [Fact]
    public void UpdateSeo_ShouldSetMetaProperties()
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");
        var metaTitle = "Technology Articles | Blog";
        var metaDescription = "Read the latest technology articles";

        // Act
        category.UpdateSeo(metaTitle, metaDescription);

        // Assert
        category.MetaTitle.Should().Be(metaTitle);
        category.MetaDescription.Should().Be(metaDescription);
    }

    [Fact]
    public void UpdateSeo_WithNull_ShouldClearMetaProperties()
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");
        category.UpdateSeo("Title", "Description");

        // Act
        category.UpdateSeo(null, null);

        // Assert
        category.MetaTitle.Should().BeNull();
        category.MetaDescription.Should().BeNull();
    }

    #endregion

    #region Image Tests

    [Fact]
    public void UpdateImage_ShouldSetImageUrl()
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");
        var imageUrl = "/images/categories/tech.jpg";

        // Act
        category.UpdateImage(imageUrl);

        // Assert
        category.ImageUrl.Should().Be(imageUrl);
    }

    [Fact]
    public void UpdateImage_WithNull_ShouldClearImageUrl()
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");
        category.UpdateImage("/images/tech.jpg");

        // Act
        category.UpdateImage(null);

        // Assert
        category.ImageUrl.Should().BeNull();
    }

    #endregion

    #region SortOrder Tests

    [Fact]
    public void SetSortOrder_ShouldUpdateSortOrder()
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");

        // Act
        category.SetSortOrder(5);

        // Assert
        category.SortOrder.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(-1)]
    public void SetSortOrder_WithVariousValues_ShouldAcceptAll(int order)
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");

        // Act
        category.SetSortOrder(order);

        // Assert
        category.SortOrder.Should().Be(order);
    }

    #endregion

    #region PostCount Tests

    [Fact]
    public void IncrementPostCount_ShouldIncreaseByOne()
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");
        var initialCount = category.PostCount;

        // Act
        category.IncrementPostCount();

        // Assert
        category.PostCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public void IncrementPostCount_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");

        // Act
        for (int i = 0; i < 10; i++)
        {
            category.IncrementPostCount();
        }

        // Assert
        category.PostCount.Should().Be(10);
    }

    [Fact]
    public void DecrementPostCount_ShouldDecreaseByOne()
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");
        category.IncrementPostCount();
        category.IncrementPostCount();

        // Act
        category.DecrementPostCount();

        // Assert
        category.PostCount.Should().Be(1);
    }

    [Fact]
    public void DecrementPostCount_WhenZero_ShouldNotGoBelowZero()
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");

        // Act
        category.DecrementPostCount();

        // Assert
        category.PostCount.Should().Be(0);
    }

    [Fact]
    public void DecrementPostCount_MultipleTimes_ShouldNotGoBelowZero()
    {
        // Arrange
        var category = PostCategory.Create("Tech", "tech");
        category.IncrementPostCount();

        // Act
        category.DecrementPostCount();
        category.DecrementPostCount();
        category.DecrementPostCount();

        // Assert
        category.PostCount.Should().Be(0);
    }

    #endregion

    #region Hierarchy Tests

    [Fact]
    public void Create_Children_ShouldBeEmpty()
    {
        // Act
        var category = PostCategory.Create("Parent", "parent");

        // Assert
        category.Children.Should().NotBeNull();
        category.Children.Should().BeEmpty();
    }

    [Fact]
    public void Create_Posts_ShouldBeEmpty()
    {
        // Act
        var category = PostCategory.Create("Category", "category");

        // Assert
        category.Posts.Should().NotBeNull();
        category.Posts.Should().BeEmpty();
    }

    #endregion

    #region Tenant Tests

    [Fact]
    public void Create_WithTenantId_ShouldBeAssociatedWithTenant()
    {
        // Arrange
        var tenantId = "tenant-abc";

        // Act
        var category = PostCategory.Create("Tech", "tech", tenantId: tenantId);

        // Assert
        category.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithoutTenantId_ShouldHaveNullTenant()
    {
        // Act
        var category = PostCategory.Create("Tech", "tech");

        // Assert
        category.TenantId.Should().BeNull();
    }

    #endregion
}
