namespace NOIR.Domain.UnitTests.Entities;

/// <summary>
/// Unit tests for the PostTag entity and PostTagAssignment.
/// Tests factory methods, updates, and post count management.
/// </summary>
public class PostTagTests
{
    #region PostTag Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidTag()
    {
        // Arrange
        var name = "JavaScript";
        var slug = "javascript";

        // Act
        var tag = PostTag.Create(name, slug);

        // Assert
        tag.Should().NotBeNull();
        tag.Id.Should().NotBe(Guid.Empty);
        tag.Name.Should().Be(name);
        tag.Slug.Should().Be(slug);
        tag.PostCount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldLowercaseSlug()
    {
        // Act
        var tag = PostTag.Create("JavaScript", "JAVASCRIPT");

        // Assert
        tag.Slug.Should().Be("javascript");
    }

    [Fact]
    public void Create_WithOptionalParameters_ShouldSetAllProperties()
    {
        // Arrange
        var name = "React";
        var slug = "react";
        var description = "React framework related posts";
        var color = "#61DAFB";
        var tenantId = "tenant-123";

        // Act
        var tag = PostTag.Create(name, slug, description, color, tenantId);

        // Assert
        tag.Description.Should().Be(description);
        tag.Color.Should().Be(color);
        tag.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithoutOptionalParameters_ShouldHaveNullOptionalFields()
    {
        // Act
        var tag = PostTag.Create("Tag", "tag");

        // Assert
        tag.Description.Should().BeNull();
        tag.Color.Should().BeNull();
        tag.TenantId.Should().BeNull();
    }

    #endregion

    #region PostTag Update Tests

    [Fact]
    public void Update_ShouldModifyAllEditableProperties()
    {
        // Arrange
        var tag = PostTag.Create("Original", "original");
        var newName = "Updated";
        var newSlug = "updated";
        var newDescription = "Updated description";
        var newColor = "#FF5733";

        // Act
        tag.Update(newName, newSlug, newDescription, newColor);

        // Assert
        tag.Name.Should().Be(newName);
        tag.Slug.Should().Be("updated");
        tag.Description.Should().Be(newDescription);
        tag.Color.Should().Be(newColor);
    }

    [Fact]
    public void Update_ShouldLowercaseSlug()
    {
        // Arrange
        var tag = PostTag.Create("Original", "original");

        // Act
        tag.Update("Updated", "UPDATED-SLUG");

        // Assert
        tag.Slug.Should().Be("updated-slug");
    }

    [Fact]
    public void Update_WithNullOptionalValues_ShouldClearThem()
    {
        // Arrange
        var tag = PostTag.Create("Tag", "tag", "Description", "#000");

        // Act
        tag.Update("Tag", "tag", null, null);

        // Assert
        tag.Description.Should().BeNull();
        tag.Color.Should().BeNull();
    }

    #endregion

    #region PostTag PostCount Tests

    [Fact]
    public void IncrementPostCount_ShouldIncreaseByOne()
    {
        // Arrange
        var tag = PostTag.Create("Tag", "tag");
        var initialCount = tag.PostCount;

        // Act
        tag.IncrementPostCount();

        // Assert
        tag.PostCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public void IncrementPostCount_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var tag = PostTag.Create("Tag", "tag");

        // Act
        for (int i = 0; i < 25; i++)
        {
            tag.IncrementPostCount();
        }

        // Assert
        tag.PostCount.Should().Be(25);
    }

    [Fact]
    public void DecrementPostCount_ShouldDecreaseByOne()
    {
        // Arrange
        var tag = PostTag.Create("Tag", "tag");
        tag.IncrementPostCount();
        tag.IncrementPostCount();

        // Act
        tag.DecrementPostCount();

        // Assert
        tag.PostCount.Should().Be(1);
    }

    [Fact]
    public void DecrementPostCount_WhenZero_ShouldNotGoBelowZero()
    {
        // Arrange
        var tag = PostTag.Create("Tag", "tag");

        // Act
        tag.DecrementPostCount();

        // Assert
        tag.PostCount.Should().Be(0);
    }

    [Fact]
    public void DecrementPostCount_MultipleTimes_ShouldNotGoBelowZero()
    {
        // Arrange
        var tag = PostTag.Create("Tag", "tag");
        tag.IncrementPostCount();

        // Act
        tag.DecrementPostCount();
        tag.DecrementPostCount();
        tag.DecrementPostCount();

        // Assert
        tag.PostCount.Should().Be(0);
    }

    #endregion

    #region PostTag Color Tests

    [Theory]
    [InlineData("#3B82F6")]
    [InlineData("#EF4444")]
    [InlineData("#10B981")]
    [InlineData("#F59E0B")]
    public void Create_WithVariousColors_ShouldPreserveColor(string color)
    {
        // Act
        var tag = PostTag.Create("Tag", "tag", color: color);

        // Assert
        tag.Color.Should().Be(color);
    }

    #endregion

    #region PostTag PostAssignments Tests

    [Fact]
    public void Create_PostAssignments_ShouldBeEmpty()
    {
        // Act
        var tag = PostTag.Create("Tag", "tag");

        // Assert
        tag.PostAssignments.Should().NotBeNull();
        tag.PostAssignments.Should().BeEmpty();
    }

    #endregion

    #region PostTagAssignment Create Tests

    [Fact]
    public void PostTagAssignment_Create_ShouldSetAllProperties()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var tenantId = "tenant-123";

        // Act
        var assignment = PostTagAssignment.Create(postId, tagId, tenantId);

        // Assert
        assignment.Should().NotBeNull();
        assignment.Id.Should().NotBe(Guid.Empty);
        assignment.PostId.Should().Be(postId);
        assignment.TagId.Should().Be(tagId);
        assignment.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void PostTagAssignment_Create_WithoutTenantId_ShouldHaveNullTenant()
    {
        // Act
        var assignment = PostTagAssignment.Create(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        assignment.TenantId.Should().BeNull();
    }

    [Fact]
    public void PostTagAssignment_Create_MultipleTimes_ShouldHaveUniqueIds()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        // Act
        var assignment1 = PostTagAssignment.Create(postId, tagId);
        var assignment2 = PostTagAssignment.Create(postId, tagId);

        // Assert
        assignment1.Id.Should().NotBe(assignment2.Id);
    }

    #endregion

    #region PostTag Tenant Tests

    [Fact]
    public void Create_WithTenantId_ShouldBeAssociatedWithTenant()
    {
        // Arrange
        var tenantId = "tenant-abc";

        // Act
        var tag = PostTag.Create("Tag", "tag", tenantId: tenantId);

        // Assert
        tag.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithoutTenantId_ShouldHaveNullTenant()
    {
        // Act
        var tag = PostTag.Create("Tag", "tag");

        // Assert
        tag.TenantId.Should().BeNull();
    }

    #endregion
}
