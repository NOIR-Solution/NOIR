namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for CacheKeys static class.
/// Tests cache key generation and tag helper methods.
/// </summary>
public class CacheKeysTests
{
    #region Permission Keys Tests

    [Fact]
    public void UserPermissions_ShouldReturnCorrectKey()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var result = CacheKeys.UserPermissions(userId);

        // Assert
        result.ShouldBe("perm:user:user-123");
    }

    [Fact]
    public void RolePermissions_ShouldReturnCorrectKey()
    {
        // Arrange
        var roleId = "role-456";

        // Act
        var result = CacheKeys.RolePermissions(roleId);

        // Assert
        result.ShouldBe("perm:role:role-456");
    }

    #endregion

    #region User Keys Tests

    [Fact]
    public void UserProfile_ShouldReturnCorrectKey()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var result = CacheKeys.UserProfile(userId);

        // Assert
        result.ShouldBe("user:profile:user-123");
    }

    [Fact]
    public void UserById_ShouldReturnCorrectKey()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var result = CacheKeys.UserById(userId);

        // Assert
        result.ShouldBe("user:id:user-123");
    }

    [Fact]
    public void UserByEmail_ShouldReturnLowercaseKey()
    {
        // Arrange
        var email = "Test@Example.COM";

        // Act
        var result = CacheKeys.UserByEmail(email);

        // Assert
        result.ShouldBe("user:email:test@example.com");
    }

    #endregion

    #region Role Keys Tests

    [Fact]
    public void RoleById_ShouldReturnCorrectKey()
    {
        // Arrange
        var roleId = "role-456";

        // Act
        var result = CacheKeys.RoleById(roleId);

        // Assert
        result.ShouldBe("role:id:role-456");
    }

    [Fact]
    public void AllRoles_ShouldReturnCorrectKey()
    {
        // Act
        var result = CacheKeys.AllRoles();

        // Assert
        result.ShouldBe("role:all");
    }

    #endregion

    #region Tenant Keys Tests

    [Fact]
    public void TenantById_ShouldReturnCorrectKey()
    {
        // Arrange
        var tenantId = "tenant-789";

        // Act
        var result = CacheKeys.TenantById(tenantId);

        // Assert
        result.ShouldBe("tenant:id:tenant-789");
    }

    [Fact]
    public void TenantSettings_ShouldReturnCorrectKey()
    {
        // Arrange
        var tenantId = "tenant-789";

        // Act
        var result = CacheKeys.TenantSettings(tenantId);

        // Assert
        result.ShouldBe("settings:tenant:tenant-789");
    }

    #endregion

    #region Blog Keys Tests

    [Fact]
    public void PostBySlug_ShouldReturnCorrectKey()
    {
        // Arrange
        var slug = "my-awesome-post";

        // Act
        var result = CacheKeys.PostBySlug(slug);

        // Assert
        result.ShouldBe("blog:post:slug:my-awesome-post");
    }

    [Fact]
    public void PostById_ShouldReturnCorrectKey()
    {
        // Arrange
        var postId = Guid.Parse("12345678-1234-1234-1234-123456789abc");

        // Act
        var result = CacheKeys.PostById(postId);

        // Assert
        result.ShouldBe("blog:post:id:12345678-1234-1234-1234-123456789abc");
    }

    [Fact]
    public void PostsList_WithoutCategory_ShouldReturnCorrectKey()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;

        // Act
        var result = CacheKeys.PostsList(page, pageSize);

        // Assert
        result.ShouldBe("blog:posts:p1:s10");
    }

    [Fact]
    public void PostsList_WithCategory_ShouldReturnCorrectKey()
    {
        // Arrange
        var page = 2;
        var pageSize = 20;
        var categorySlug = "tech";

        // Act
        var result = CacheKeys.PostsList(page, pageSize, categorySlug);

        // Assert
        result.ShouldBe("blog:posts:p2:s20:ctech");
    }

    [Fact]
    public void BlogCategories_ShouldReturnCorrectKey()
    {
        // Act
        var result = CacheKeys.BlogCategories();

        // Assert
        result.ShouldBe("blog:categories");
    }

    [Fact]
    public void BlogTags_ShouldReturnCorrectKey()
    {
        // Act
        var result = CacheKeys.BlogTags();

        // Assert
        result.ShouldBe("blog:tags");
    }

    [Fact]
    public void RssFeed_ShouldReturnCorrectKey()
    {
        // Act
        var result = CacheKeys.RssFeed();

        // Assert
        result.ShouldBe("blog:feed:rss");
    }

    [Fact]
    public void Sitemap_ShouldReturnCorrectKey()
    {
        // Act
        var result = CacheKeys.Sitemap();

        // Assert
        result.ShouldBe("blog:sitemap");
    }

    #endregion

    #region Tag Helper Tests

    [Fact]
    public void UserTags_ShouldReturnCorrectTags()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var result = CacheKeys.UserTags(userId);

        // Assert
        result.Count().ShouldBe(1);
        result.ShouldContain("user:user-123");
    }

    [Fact]
    public void PermissionTags_ShouldReturnCorrectTags()
    {
        // Arrange
        var userId = "user-123";

        // Act
        var result = CacheKeys.PermissionTags(userId);

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain("permissions");
        result.ShouldContain("user:user-123");
    }

    [Fact]
    public void RoleTags_ShouldReturnCorrectTags()
    {
        // Arrange
        var roleId = "role-456";

        // Act
        var result = CacheKeys.RoleTags(roleId);

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain("role:role-456");
        result.ShouldContain("roles");
    }

    [Fact]
    public void PostTags_ShouldReturnCorrectTags()
    {
        // Arrange
        var postId = Guid.Parse("12345678-1234-1234-1234-123456789abc");

        // Act
        var result = CacheKeys.PostTags(postId);

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain("blog");
        result.ShouldContain("post:12345678-1234-1234-1234-123456789abc");
    }

    [Fact]
    public void BlogListTags_ShouldReturnCorrectTags()
    {
        // Act
        var result = CacheKeys.BlogListTags();

        // Assert
        result.Count().ShouldBe(2);
        result.ShouldContain("blog");
        result.ShouldContain("posts-list");
    }

    #endregion
}
