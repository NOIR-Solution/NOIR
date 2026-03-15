namespace NOIR.Application.UnitTests.Specifications;

using NOIR.Application.Features.Blog.Specifications;

/// <summary>
/// Unit tests for Blog specifications (Category, Post, Tag).
/// Verifies that specifications are correctly configured with expected filters,
/// sorting, includes, pagination, tracking, and query tags.
/// </summary>
public class BlogSpecificationsTests
{
    private static readonly Guid TestId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestId3 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid AuthorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    #region Helper Methods

    private static PostCategory CreateCategory(
        Guid? id = null,
        string? name = null,
        string? slug = null,
        string? description = null,
        Guid? parentId = null,
        string? tenantId = null,
        int sortOrder = 0)
    {
        var category = PostCategory.Create(
            name: name ?? "Test Category",
            slug: slug ?? "test-category",
            description: description,
            parentId: parentId,
            tenantId: tenantId);

        category.SetSortOrder(sortOrder);

        // Use reflection to set Id since it's a private setter
        if (id.HasValue)
        {
            typeof(PostCategory).GetProperty("Id")!.SetValue(category, id.Value);
        }

        return category;
    }

    private static PostTag CreateTag(
        Guid? id = null,
        string? name = null,
        string? slug = null,
        string? description = null,
        string? color = null,
        string? tenantId = null)
    {
        var tag = PostTag.Create(
            name: name ?? "Test Tag",
            slug: slug ?? "test-tag",
            description: description,
            color: color,
            tenantId: tenantId);

        if (id.HasValue)
        {
            typeof(PostTag).GetProperty("Id")!.SetValue(tag, id.Value);
        }

        return tag;
    }

    private static Post CreatePost(
        Guid? id = null,
        string? title = null,
        string? slug = null,
        string? excerpt = null,
        PostStatus status = PostStatus.Draft,
        Guid? categoryId = null,
        Guid? authorId = null,
        string? tenantId = null,
        DateTimeOffset? publishedAt = null,
        DateTimeOffset? scheduledPublishAt = null)
    {
        var post = Post.Create(
            title: title ?? "Test Post",
            slug: slug ?? "test-post",
            authorId: authorId ?? AuthorId,
            tenantId: tenantId);

        if (id.HasValue)
        {
            typeof(Post).GetProperty("Id")!.SetValue(post, id.Value);
        }

        if (excerpt != null)
        {
            post.UpdateContent(post.Title, post.Slug, excerpt, null, null);
        }

        if (categoryId.HasValue)
        {
            post.SetCategory(categoryId.Value);
        }

        // Set status and dates using reflection for testing purposes
        if (status != PostStatus.Draft)
        {
            typeof(Post).GetProperty("Status")!.SetValue(post, status);
        }

        if (publishedAt.HasValue)
        {
            typeof(Post).GetProperty("PublishedAt")!.SetValue(post, publishedAt.Value);
        }

        if (scheduledPublishAt.HasValue)
        {
            typeof(Post).GetProperty("ScheduledPublishAt")!.SetValue(post, scheduledPublishAt.Value);
        }

        return post;
    }

    private static PostTagAssignment CreateTagAssignment(
        Guid postId,
        Guid tagId,
        string? tenantId = null)
    {
        return PostTagAssignment.Create(postId, tagId, tenantId);
    }

    #endregion

    #region CategoriesSpec Tests

    [Fact]
    public void CategoriesSpec_WithNoSearch_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new CategoriesSpec();

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void CategoriesSpec_ShouldHaveOrderBy()
    {
        // Arrange & Act
        var spec = new CategoriesSpec();

        // Assert - Ordered by SortOrder then Name
        spec.OrderBy.ShouldNotBeNull();
        spec.ThenByExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void CategoriesSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new CategoriesSpec();

        // Assert
        spec.QueryTags.ShouldContain("GetCategories");
    }

    [Fact]
    public void CategoriesSpec_WithoutIncludeChildren_ShouldNotIncludeChildren()
    {
        // Arrange & Act
        var spec = new CategoriesSpec(includeChildren: false);

        // Assert
        spec.Includes.ShouldBeEmpty();
    }

    [Fact]
    public void CategoriesSpec_WithIncludeChildren_ShouldIncludeChildren()
    {
        // Arrange & Act
        var spec = new CategoriesSpec(includeChildren: true);

        // Assert
        spec.Includes.Count().ShouldBe(1);
    }

    [Fact]
    public void CategoriesSpec_DefaultAsNoTracking_ShouldBeTrue()
    {
        // Arrange & Act
        var spec = new CategoriesSpec();

        // Assert
        spec.AsNoTracking.ShouldBe(true);
    }

    [Fact]
    public void CategoriesSpec_WithMatchingNameSearch_ShouldSatisfy()
    {
        // Arrange
        var category = CreateCategory(name: "Technology");
        var spec = new CategoriesSpec(search: "Tech");

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(true);
    }

    [Fact]
    public void CategoriesSpec_WithMatchingDescriptionSearch_ShouldSatisfy()
    {
        // Arrange
        var category = CreateCategory(name: "General", description: "Tech articles");
        var spec = new CategoriesSpec(search: "Tech");

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(true);
    }

    [Fact]
    public void CategoriesSpec_WithNonMatchingSearch_ShouldNotSatisfy()
    {
        // Arrange
        var category = CreateCategory(name: "Science", description: "Physics and Chemistry");
        var spec = new CategoriesSpec(search: "Tech");

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(false);
    }

    [Fact]
    public void CategoriesSpec_WithNullSearch_ShouldSatisfyAll()
    {
        // Arrange
        var category = CreateCategory(name: "Anything");
        var spec = new CategoriesSpec(search: null);

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(true);
    }

    #endregion

    #region TopLevelCategoriesSpec Tests

    [Fact]
    public void TopLevelCategoriesSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new TopLevelCategoriesSpec();

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void TopLevelCategoriesSpec_ShouldIncludeChildren()
    {
        // Arrange & Act
        var spec = new TopLevelCategoriesSpec();

        // Assert
        spec.Includes.Count().ShouldBe(1);
    }

    [Fact]
    public void TopLevelCategoriesSpec_ShouldHaveOrderBy()
    {
        // Arrange & Act
        var spec = new TopLevelCategoriesSpec();

        // Assert
        spec.OrderBy.ShouldNotBeNull();
        spec.ThenByExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void TopLevelCategoriesSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new TopLevelCategoriesSpec();

        // Assert
        spec.QueryTags.ShouldContain("GetTopLevelCategories");
    }

    [Fact]
    public void TopLevelCategoriesSpec_WithNoParent_ShouldSatisfy()
    {
        // Arrange
        var category = CreateCategory(parentId: null);
        var spec = new TopLevelCategoriesSpec();

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(true);
    }

    [Fact]
    public void TopLevelCategoriesSpec_WithParent_ShouldNotSatisfy()
    {
        // Arrange
        var category = CreateCategory(parentId: TestId1);
        var spec = new TopLevelCategoriesSpec();

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(false);
    }

    #endregion

    #region CategoryByIdSpec Tests

    [Fact]
    public void CategoryByIdSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new CategoryByIdSpec(TestId1);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void CategoryByIdSpec_ShouldIncludeParentAndChildren()
    {
        // Arrange & Act
        var spec = new CategoryByIdSpec(TestId1);

        // Assert
        spec.Includes.Count().ShouldBe(2);
    }

    [Fact]
    public void CategoryByIdSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new CategoryByIdSpec(TestId1);

        // Assert
        spec.QueryTags.ShouldContain("GetCategoryById");
    }

    [Fact]
    public void CategoryByIdSpec_DefaultAsNoTracking_ShouldBeTrue()
    {
        // Arrange & Act
        var spec = new CategoryByIdSpec(TestId1);

        // Assert
        spec.AsNoTracking.ShouldBe(true);
    }

    [Fact]
    public void CategoryByIdSpec_WithMatchingId_ShouldSatisfy()
    {
        // Arrange
        var category = CreateCategory(id: TestId1);
        var spec = new CategoryByIdSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(true);
    }

    [Fact]
    public void CategoryByIdSpec_WithNonMatchingId_ShouldNotSatisfy()
    {
        // Arrange
        var category = CreateCategory(id: TestId1);
        var spec = new CategoryByIdSpec(TestId2);

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(false);
    }

    #endregion

    #region CategoryByIdForUpdateSpec Tests

    [Fact]
    public void CategoryByIdForUpdateSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new CategoryByIdForUpdateSpec(TestId1);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void CategoryByIdForUpdateSpec_ShouldEnableTracking()
    {
        // Arrange & Act
        var spec = new CategoryByIdForUpdateSpec(TestId1);

        // Assert
        spec.AsNoTracking.ShouldBe(false);
    }

    [Fact]
    public void CategoryByIdForUpdateSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new CategoryByIdForUpdateSpec(TestId1);

        // Assert
        spec.QueryTags.ShouldContain("GetCategoryByIdForUpdate");
    }

    [Fact]
    public void CategoryByIdForUpdateSpec_WithMatchingId_ShouldSatisfy()
    {
        // Arrange
        var category = CreateCategory(id: TestId1);
        var spec = new CategoryByIdForUpdateSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(true);
    }

    #endregion

    #region CategoryBySlugSpec Tests

    [Fact]
    public void CategoryBySlugSpec_ShouldHaveWhereExpressions()
    {
        // Arrange & Act
        var spec = new CategoryBySlugSpec("test-slug");

        // Assert
        spec.WhereExpressions.Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void CategoryBySlugSpec_ShouldIncludeParentAndChildren()
    {
        // Arrange & Act
        var spec = new CategoryBySlugSpec("test-slug");

        // Assert
        spec.Includes.Count().ShouldBe(2);
    }

    [Fact]
    public void CategoryBySlugSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new CategoryBySlugSpec("test-slug");

        // Assert
        spec.QueryTags.ShouldContain("GetCategoryBySlug");
    }

    [Fact]
    public void CategoryBySlugSpec_ShouldConvertToLowercase()
    {
        // Arrange
        var category = CreateCategory(slug: "test-slug");
        var spec = new CategoryBySlugSpec("TEST-SLUG");

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(true);
    }

    [Fact]
    public void CategoryBySlugSpec_WithMatchingSlug_ShouldSatisfy()
    {
        // Arrange
        var category = CreateCategory(slug: "my-category");
        var spec = new CategoryBySlugSpec("my-category");

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(true);
    }

    [Fact]
    public void CategoryBySlugSpec_WithNonMatchingSlug_ShouldNotSatisfy()
    {
        // Arrange
        var category = CreateCategory(slug: "my-category");
        var spec = new CategoryBySlugSpec("other-category");

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(false);
    }

    #endregion

    #region CategorySlugExistsSpec Tests

    [Fact]
    public void CategorySlugExistsSpec_ShouldHaveWhereExpressions()
    {
        // Arrange & Act
        var spec = new CategorySlugExistsSpec("test-slug");

        // Assert
        spec.WhereExpressions.Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void CategorySlugExistsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new CategorySlugExistsSpec("test-slug");

        // Assert
        spec.QueryTags.ShouldContain("CheckCategorySlugExists");
    }

    [Fact]
    public void CategorySlugExistsSpec_WithMatchingSlug_ShouldSatisfy()
    {
        // Arrange
        var category = CreateCategory(slug: "unique-slug");
        var spec = new CategorySlugExistsSpec("unique-slug");

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(true);
    }

    [Fact]
    public void CategorySlugExistsSpec_WithExcludedId_ShouldNotSatisfy()
    {
        // Arrange
        var category = CreateCategory(id: TestId1, slug: "unique-slug");
        var spec = new CategorySlugExistsSpec("unique-slug", excludeId: TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(category).ShouldBe(false);
    }

    #endregion

    #region CategoryHasPostsSpec Tests

    [Fact]
    public void CategoryHasPostsSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new CategoryHasPostsSpec(TestId1);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void CategoryHasPostsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new CategoryHasPostsSpec(TestId1);

        // Assert
        spec.QueryTags.ShouldContain("CheckCategoryHasPosts");
    }

    [Fact]
    public void CategoryHasPostsSpec_WithMatchingCategoryId_ShouldSatisfy()
    {
        // Arrange
        var post = CreatePost(categoryId: TestId1);
        var spec = new CategoryHasPostsSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(true);
    }

    [Fact]
    public void CategoryHasPostsSpec_WithDifferentCategoryId_ShouldNotSatisfy()
    {
        // Arrange
        var post = CreatePost(categoryId: TestId2);
        var spec = new CategoryHasPostsSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(false);
    }

    #endregion

    #region TagsSpec Tests

    [Fact]
    public void TagsSpec_WithNoSearch_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new TagsSpec();

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void TagsSpec_ShouldHaveOrderByName()
    {
        // Arrange & Act
        var spec = new TagsSpec();

        // Assert
        spec.OrderBy.ShouldNotBeNull();
    }

    [Fact]
    public void TagsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new TagsSpec();

        // Assert
        spec.QueryTags.ShouldContain("GetTags");
    }

    [Fact]
    public void TagsSpec_DefaultAsNoTracking_ShouldBeTrue()
    {
        // Arrange & Act
        var spec = new TagsSpec();

        // Assert
        spec.AsNoTracking.ShouldBe(true);
    }

    [Fact]
    public void TagsSpec_WithMatchingNameSearch_ShouldSatisfy()
    {
        // Arrange
        var tag = CreateTag(name: "JavaScript");
        var spec = new TagsSpec(search: "Java");

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(true);
    }

    [Fact]
    public void TagsSpec_WithMatchingDescriptionSearch_ShouldSatisfy()
    {
        // Arrange
        var tag = CreateTag(name: "Programming", description: "JavaScript related");
        var spec = new TagsSpec(search: "Java");

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(true);
    }

    [Fact]
    public void TagsSpec_WithNonMatchingSearch_ShouldNotSatisfy()
    {
        // Arrange
        var tag = CreateTag(name: "Python", description: "Snake language");
        var spec = new TagsSpec(search: "Java");

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(false);
    }

    [Fact]
    public void TagsSpec_WithNullSearch_ShouldSatisfyAll()
    {
        // Arrange
        var tag = CreateTag(name: "Anything");
        var spec = new TagsSpec(search: null);

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(true);
    }

    #endregion

    #region TagByIdSpec Tests

    [Fact]
    public void TagByIdSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new TagByIdSpec(TestId1);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void TagByIdSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new TagByIdSpec(TestId1);

        // Assert
        spec.QueryTags.ShouldContain("GetTagById");
    }

    [Fact]
    public void TagByIdSpec_DefaultAsNoTracking_ShouldBeTrue()
    {
        // Arrange & Act
        var spec = new TagByIdSpec(TestId1);

        // Assert
        spec.AsNoTracking.ShouldBe(true);
    }

    [Fact]
    public void TagByIdSpec_WithMatchingId_ShouldSatisfy()
    {
        // Arrange
        var tag = CreateTag(id: TestId1);
        var spec = new TagByIdSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(true);
    }

    [Fact]
    public void TagByIdSpec_WithNonMatchingId_ShouldNotSatisfy()
    {
        // Arrange
        var tag = CreateTag(id: TestId1);
        var spec = new TagByIdSpec(TestId2);

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(false);
    }

    #endregion

    #region TagByIdForUpdateSpec Tests

    [Fact]
    public void TagByIdForUpdateSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new TagByIdForUpdateSpec(TestId1);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void TagByIdForUpdateSpec_ShouldEnableTracking()
    {
        // Arrange & Act
        var spec = new TagByIdForUpdateSpec(TestId1);

        // Assert
        spec.AsNoTracking.ShouldBe(false);
    }

    [Fact]
    public void TagByIdForUpdateSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new TagByIdForUpdateSpec(TestId1);

        // Assert
        spec.QueryTags.ShouldContain("GetTagByIdForUpdate");
    }

    [Fact]
    public void TagByIdForUpdateSpec_WithMatchingId_ShouldSatisfy()
    {
        // Arrange
        var tag = CreateTag(id: TestId1);
        var spec = new TagByIdForUpdateSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(true);
    }

    #endregion

    #region TagBySlugSpec Tests

    [Fact]
    public void TagBySlugSpec_ShouldHaveWhereExpressions()
    {
        // Arrange & Act
        var spec = new TagBySlugSpec("test-slug");

        // Assert
        spec.WhereExpressions.Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void TagBySlugSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new TagBySlugSpec("test-slug");

        // Assert
        spec.QueryTags.ShouldContain("GetTagBySlug");
    }

    [Fact]
    public void TagBySlugSpec_ShouldConvertToLowercase()
    {
        // Arrange
        var tag = CreateTag(slug: "test-slug");
        var spec = new TagBySlugSpec("TEST-SLUG");

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(true);
    }

    [Fact]
    public void TagBySlugSpec_WithMatchingSlug_ShouldSatisfy()
    {
        // Arrange
        var tag = CreateTag(slug: "my-tag");
        var spec = new TagBySlugSpec("my-tag");

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(true);
    }

    #endregion

    #region TagSlugExistsSpec Tests

    [Fact]
    public void TagSlugExistsSpec_ShouldHaveWhereExpressions()
    {
        // Arrange & Act
        var spec = new TagSlugExistsSpec("test-slug");

        // Assert
        spec.WhereExpressions.Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void TagSlugExistsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new TagSlugExistsSpec("test-slug");

        // Assert
        spec.QueryTags.ShouldContain("CheckTagSlugExists");
    }

    [Fact]
    public void TagSlugExistsSpec_WithMatchingSlug_ShouldSatisfy()
    {
        // Arrange
        var tag = CreateTag(slug: "unique-slug");
        var spec = new TagSlugExistsSpec("unique-slug");

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(true);
    }

    [Fact]
    public void TagSlugExistsSpec_WithExcludedId_ShouldNotSatisfy()
    {
        // Arrange
        var tag = CreateTag(id: TestId1, slug: "unique-slug");
        var spec = new TagSlugExistsSpec("unique-slug", excludeId: TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(false);
    }

    #endregion

    #region TagsByIdsSpec Tests

    [Fact]
    public void TagsByIdsSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var ids = new List<Guid> { TestId1, TestId2 };
        var spec = new TagsByIdsSpec(ids);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void TagsByIdsSpec_ShouldEnableTracking()
    {
        // Arrange & Act
        var ids = new List<Guid> { TestId1 };
        var spec = new TagsByIdsSpec(ids);

        // Assert
        spec.AsNoTracking.ShouldBe(false);
    }

    [Fact]
    public void TagsByIdsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var ids = new List<Guid> { TestId1 };
        var spec = new TagsByIdsSpec(ids);

        // Assert
        spec.QueryTags.ShouldContain("GetTagsByIds");
    }

    [Fact]
    public void TagsByIdsSpec_WithMatchingId_ShouldSatisfy()
    {
        // Arrange
        var tag = CreateTag(id: TestId1);
        var spec = new TagsByIdsSpec(new List<Guid> { TestId1, TestId2 });

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(true);
    }

    [Fact]
    public void TagsByIdsSpec_WithNonMatchingId_ShouldNotSatisfy()
    {
        // Arrange
        var tag = CreateTag(id: TestId3);
        var spec = new TagsByIdsSpec(new List<Guid> { TestId1, TestId2 });

        // Act & Assert
        spec.IsSatisfiedBy(tag).ShouldBe(false);
    }

    #endregion

    #region TagAssignmentsByPostIdSpec Tests

    [Fact]
    public void TagAssignmentsByPostIdSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new TagAssignmentsByPostIdSpec(TestId1);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void TagAssignmentsByPostIdSpec_ShouldEnableTracking()
    {
        // Arrange & Act
        var spec = new TagAssignmentsByPostIdSpec(TestId1);

        // Assert
        spec.AsNoTracking.ShouldBe(false);
    }

    [Fact]
    public void TagAssignmentsByPostIdSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new TagAssignmentsByPostIdSpec(TestId1);

        // Assert
        spec.QueryTags.ShouldContain("GetTagAssignmentsByPostId");
    }

    [Fact]
    public void TagAssignmentsByPostIdSpec_WithMatchingPostId_ShouldSatisfy()
    {
        // Arrange
        var assignment = CreateTagAssignment(TestId1, TestId2);
        var spec = new TagAssignmentsByPostIdSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(assignment).ShouldBe(true);
    }

    [Fact]
    public void TagAssignmentsByPostIdSpec_WithDifferentPostId_ShouldNotSatisfy()
    {
        // Arrange
        var assignment = CreateTagAssignment(TestId2, TestId3);
        var spec = new TagAssignmentsByPostIdSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(assignment).ShouldBe(false);
    }

    #endregion

    #region TagAssignmentsByTagIdSpec Tests

    [Fact]
    public void TagAssignmentsByTagIdSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new TagAssignmentsByTagIdSpec(TestId1);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void TagAssignmentsByTagIdSpec_ShouldEnableTracking()
    {
        // Arrange & Act
        var spec = new TagAssignmentsByTagIdSpec(TestId1);

        // Assert
        spec.AsNoTracking.ShouldBe(false);
    }

    [Fact]
    public void TagAssignmentsByTagIdSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new TagAssignmentsByTagIdSpec(TestId1);

        // Assert
        spec.QueryTags.ShouldContain("GetTagAssignmentsByTagId");
    }

    [Fact]
    public void TagAssignmentsByTagIdSpec_WithMatchingTagId_ShouldSatisfy()
    {
        // Arrange
        var assignment = CreateTagAssignment(TestId2, TestId1);
        var spec = new TagAssignmentsByTagIdSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(assignment).ShouldBe(true);
    }

    #endregion

    #region PostsSpec Tests

    [Fact]
    public void PostsSpec_WithNoFilters_ShouldHaveWhereExpressions()
    {
        // Arrange & Act
        var spec = new PostsSpec();

        // Assert
        spec.WhereExpressions.Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void PostsSpec_ShouldIncludeCategoryAndTags()
    {
        // Arrange & Act
        var spec = new PostsSpec();

        // Assert - Includes Category and FeaturedImage
        spec.Includes.Count().ShouldBe(2);
        spec.IncludeStrings.ShouldContain("TagAssignments.Tag");
    }

    [Fact]
    public void PostsSpec_ShouldOrderByCreatedAtDescending()
    {
        // Arrange & Act
        var spec = new PostsSpec();

        // Assert
        spec.OrderByDescending.ShouldNotBeNull();
    }

    [Fact]
    public void PostsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new PostsSpec();

        // Assert
        spec.QueryTags.ShouldContain("GetPosts");
    }

    [Fact]
    public void PostsSpec_WithPagination_ShouldSetSkipAndTake()
    {
        // Arrange & Act
        var spec = new PostsSpec(skip: 10, take: 20);

        // Assert
        spec.Skip.ShouldBe(10);
        spec.Take.ShouldBe(20);
    }

    [Fact]
    public void PostsSpec_WithoutPagination_ShouldNotSetSkipAndTake()
    {
        // Arrange & Act
        var spec = new PostsSpec();

        // Assert
        spec.Skip.ShouldBeNull();
        spec.Take.ShouldBeNull();
    }

    [Fact]
    public void PostsSpec_DefaultAsNoTracking_ShouldBeTrue()
    {
        // Arrange & Act
        var spec = new PostsSpec();

        // Assert
        spec.AsNoTracking.ShouldBe(true);
    }

    [Fact]
    public void PostsSpec_WithMatchingTitleSearch_ShouldSatisfy()
    {
        // Arrange
        var post = CreatePost(title: "Introduction to TypeScript");
        var spec = new PostsSpec(search: "TypeScript");

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(true);
    }

    [Fact]
    public void PostsSpec_WithMatchingExcerptSearch_ShouldSatisfy()
    {
        // Arrange
        var post = CreatePost(title: "Programming", excerpt: "Learn TypeScript basics");
        var spec = new PostsSpec(search: "TypeScript");

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(true);
    }

    [Fact]
    public void PostsSpec_WithStatusFilter_ShouldFilterByStatus()
    {
        // Arrange
        var draftPost = CreatePost(status: PostStatus.Draft);
        var publishedPost = CreatePost(status: PostStatus.Published);
        var spec = new PostsSpec(status: PostStatus.Published);

        // Act & Assert
        spec.IsSatisfiedBy(draftPost).ShouldBe(false);
        spec.IsSatisfiedBy(publishedPost).ShouldBe(true);
    }

    [Fact]
    public void PostsSpec_WithCategoryFilter_ShouldFilterByCategory()
    {
        // Arrange
        var post1 = CreatePost(categoryId: TestId1);
        var post2 = CreatePost(categoryId: TestId2);
        var spec = new PostsSpec(categoryId: TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(post1).ShouldBe(true);
        spec.IsSatisfiedBy(post2).ShouldBe(false);
    }

    [Fact]
    public void PostsSpec_WithAuthorFilter_ShouldFilterByAuthor()
    {
        // Arrange
        var post1 = CreatePost(authorId: AuthorId);
        var post2 = CreatePost(authorId: TestId2);
        var spec = new PostsSpec(authorId: AuthorId);

        // Act & Assert
        spec.IsSatisfiedBy(post1).ShouldBe(true);
        spec.IsSatisfiedBy(post2).ShouldBe(false);
    }

    #endregion

    #region PublishedPostsSpec Tests

    [Fact]
    public void PublishedPostsSpec_ShouldHaveWhereExpressions()
    {
        // Arrange & Act
        var spec = new PublishedPostsSpec();

        // Assert
        spec.WhereExpressions.Count().ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void PublishedPostsSpec_ShouldIncludeCategoryAndTags()
    {
        // Arrange & Act
        var spec = new PublishedPostsSpec();

        // Assert - Includes Category and FeaturedImage
        spec.Includes.Count().ShouldBe(2);
        spec.IncludeStrings.ShouldContain("TagAssignments.Tag");
    }

    [Fact]
    public void PublishedPostsSpec_ShouldOrderByPublishedAtDescending()
    {
        // Arrange & Act
        var spec = new PublishedPostsSpec();

        // Assert
        spec.OrderByDescending.ShouldNotBeNull();
    }

    [Fact]
    public void PublishedPostsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new PublishedPostsSpec();

        // Assert
        spec.QueryTags.ShouldContain("GetPublishedPosts");
    }

    [Fact]
    public void PublishedPostsSpec_WithPagination_ShouldSetSkipAndTake()
    {
        // Arrange & Act
        var spec = new PublishedPostsSpec(skip: 5, take: 10);

        // Assert
        spec.Skip.ShouldBe(5);
        spec.Take.ShouldBe(10);
    }

    [Fact]
    public void PublishedPostsSpec_OnlyMatchesPublishedPosts()
    {
        // Arrange
        var publishedPost = CreatePost(
            status: PostStatus.Published,
            publishedAt: DateTimeOffset.UtcNow.AddDays(-1));
        var draftPost = CreatePost(status: PostStatus.Draft);
        var spec = new PublishedPostsSpec();

        // Act & Assert
        spec.IsSatisfiedBy(publishedPost).ShouldBe(true);
        spec.IsSatisfiedBy(draftPost).ShouldBe(false);
    }

    [Fact]
    public void PublishedPostsSpec_DoesNotMatchFuturePublishedPosts()
    {
        // Arrange
        var futurePost = CreatePost(
            status: PostStatus.Published,
            publishedAt: DateTimeOffset.UtcNow.AddDays(1));
        var spec = new PublishedPostsSpec();

        // Act & Assert
        spec.IsSatisfiedBy(futurePost).ShouldBe(false);
    }

    #endregion

    #region PostByIdSpec Tests

    [Fact]
    public void PostByIdSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new PostByIdSpec(TestId1);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void PostByIdSpec_ShouldIncludeCategoryAndTags()
    {
        // Arrange & Act
        var spec = new PostByIdSpec(TestId1);

        // Assert - Includes Category and FeaturedImage
        spec.Includes.Count().ShouldBe(2);
        spec.IncludeStrings.ShouldContain("TagAssignments.Tag");
    }

    [Fact]
    public void PostByIdSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new PostByIdSpec(TestId1);

        // Assert
        spec.QueryTags.ShouldContain("GetPostById");
    }

    [Fact]
    public void PostByIdSpec_DefaultAsNoTracking_ShouldBeTrue()
    {
        // Arrange & Act
        var spec = new PostByIdSpec(TestId1);

        // Assert
        spec.AsNoTracking.ShouldBe(true);
    }

    [Fact]
    public void PostByIdSpec_WithMatchingId_ShouldSatisfy()
    {
        // Arrange
        var post = CreatePost(id: TestId1);
        var spec = new PostByIdSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(true);
    }

    [Fact]
    public void PostByIdSpec_WithNonMatchingId_ShouldNotSatisfy()
    {
        // Arrange
        var post = CreatePost(id: TestId1);
        var spec = new PostByIdSpec(TestId2);

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(false);
    }

    #endregion

    #region PostByIdForUpdateSpec Tests

    [Fact]
    public void PostByIdForUpdateSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new PostByIdForUpdateSpec(TestId1);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void PostByIdForUpdateSpec_ShouldIncludeTagAssignments()
    {
        // Arrange & Act
        var spec = new PostByIdForUpdateSpec(TestId1);

        // Assert - Includes TagAssignments and FeaturedImage
        spec.Includes.Count().ShouldBe(2);
    }

    [Fact]
    public void PostByIdForUpdateSpec_ShouldEnableTracking()
    {
        // Arrange & Act
        var spec = new PostByIdForUpdateSpec(TestId1);

        // Assert
        spec.AsNoTracking.ShouldBe(false);
    }

    [Fact]
    public void PostByIdForUpdateSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new PostByIdForUpdateSpec(TestId1);

        // Assert
        spec.QueryTags.ShouldContain("GetPostByIdForUpdate");
    }

    [Fact]
    public void PostByIdForUpdateSpec_WithMatchingId_ShouldSatisfy()
    {
        // Arrange
        var post = CreatePost(id: TestId1);
        var spec = new PostByIdForUpdateSpec(TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(true);
    }

    #endregion

    #region PostBySlugSpec Tests

    [Fact]
    public void PostBySlugSpec_ShouldHaveWhereExpressions()
    {
        // Arrange & Act
        var spec = new PostBySlugSpec("test-slug");

        // Assert
        spec.WhereExpressions.Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void PostBySlugSpec_ShouldIncludeCategoryAndTags()
    {
        // Arrange & Act
        var spec = new PostBySlugSpec("test-slug");

        // Assert - Includes Category and FeaturedImage
        spec.Includes.Count().ShouldBe(2);
        spec.IncludeStrings.ShouldContain("TagAssignments.Tag");
    }

    [Fact]
    public void PostBySlugSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new PostBySlugSpec("test-slug");

        // Assert
        spec.QueryTags.ShouldContain("GetPostBySlug");
    }

    [Fact]
    public void PostBySlugSpec_ShouldConvertToLowercase()
    {
        // Arrange
        var post = CreatePost(slug: "test-slug");
        var spec = new PostBySlugSpec("TEST-SLUG");

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(true);
    }

    [Fact]
    public void PostBySlugSpec_WithMatchingSlug_ShouldSatisfy()
    {
        // Arrange
        var post = CreatePost(slug: "my-post");
        var spec = new PostBySlugSpec("my-post");

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(true);
    }

    #endregion

    #region ScheduledPostsReadyToPublishSpec Tests

    [Fact]
    public void ScheduledPostsReadyToPublishSpec_ShouldHaveWhereExpressions()
    {
        // Arrange & Act
        var spec = new ScheduledPostsReadyToPublishSpec();

        // Assert
        spec.WhereExpressions.Count().ShouldBe(2);
    }

    [Fact]
    public void ScheduledPostsReadyToPublishSpec_ShouldEnableTracking()
    {
        // Arrange & Act
        var spec = new ScheduledPostsReadyToPublishSpec();

        // Assert
        spec.AsNoTracking.ShouldBe(false);
    }

    [Fact]
    public void ScheduledPostsReadyToPublishSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new ScheduledPostsReadyToPublishSpec();

        // Assert
        spec.QueryTags.ShouldContain("GetScheduledPostsReadyToPublish");
    }

    [Fact]
    public void ScheduledPostsReadyToPublishSpec_WithScheduledPostInPast_ShouldSatisfy()
    {
        // Arrange
        var post = CreatePost(
            status: PostStatus.Scheduled,
            scheduledPublishAt: DateTimeOffset.UtcNow.AddHours(-1));
        var spec = new ScheduledPostsReadyToPublishSpec();

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(true);
    }

    [Fact]
    public void ScheduledPostsReadyToPublishSpec_WithScheduledPostInFuture_ShouldNotSatisfy()
    {
        // Arrange
        var post = CreatePost(
            status: PostStatus.Scheduled,
            scheduledPublishAt: DateTimeOffset.UtcNow.AddHours(1));
        var spec = new ScheduledPostsReadyToPublishSpec();

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(false);
    }

    [Fact]
    public void ScheduledPostsReadyToPublishSpec_WithNonScheduledStatus_ShouldNotSatisfy()
    {
        // Arrange
        var post = CreatePost(
            status: PostStatus.Draft,
            scheduledPublishAt: DateTimeOffset.UtcNow.AddHours(-1));
        var spec = new ScheduledPostsReadyToPublishSpec();

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(false);
    }

    #endregion

    #region PostSlugExistsSpec Tests

    [Fact]
    public void PostSlugExistsSpec_ShouldHaveWhereExpressions()
    {
        // Arrange & Act
        var spec = new PostSlugExistsSpec("test-slug");

        // Assert
        spec.WhereExpressions.Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void PostSlugExistsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new PostSlugExistsSpec("test-slug");

        // Assert
        spec.QueryTags.ShouldContain("CheckPostSlugExists");
    }

    [Fact]
    public void PostSlugExistsSpec_WithMatchingSlug_ShouldSatisfy()
    {
        // Arrange
        var post = CreatePost(slug: "unique-slug");
        var spec = new PostSlugExistsSpec("unique-slug");

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(true);
    }

    [Fact]
    public void PostSlugExistsSpec_WithExcludedId_ShouldNotSatisfy()
    {
        // Arrange
        var post = CreatePost(id: TestId1, slug: "unique-slug");
        var spec = new PostSlugExistsSpec("unique-slug", excludeId: TestId1);

        // Act & Assert
        spec.IsSatisfiedBy(post).ShouldBe(false);
    }

    #endregion

    #region PostsWithTagForUpdateSpec Tests

    [Fact]
    public void PostsWithTagForUpdateSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new PostsWithTagForUpdateSpec(TestId1);

        // Assert
        spec.WhereExpressions.Count().ShouldBe(1);
    }

    [Fact]
    public void PostsWithTagForUpdateSpec_ShouldIncludeTagAssignments()
    {
        // Arrange & Act
        var spec = new PostsWithTagForUpdateSpec(TestId1);

        // Assert
        spec.Includes.Count().ShouldBe(1);
    }

    [Fact]
    public void PostsWithTagForUpdateSpec_ShouldEnableTracking()
    {
        // Arrange & Act
        var spec = new PostsWithTagForUpdateSpec(TestId1);

        // Assert
        spec.AsNoTracking.ShouldBe(false);
    }

    [Fact]
    public void PostsWithTagForUpdateSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new PostsWithTagForUpdateSpec(TestId1);

        // Assert
        spec.QueryTags.ShouldContain("GetPostsWithTagForUpdate");
    }

    #endregion
}
