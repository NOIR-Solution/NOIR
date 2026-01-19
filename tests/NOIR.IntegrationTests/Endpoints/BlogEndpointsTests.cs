using NOIR.Application.Features.Blog.DTOs;
using BlogPagedResult = NOIR.Application.Features.Blog.Queries.GetPosts.PagedResult<NOIR.Application.Features.Blog.DTOs.PostListDto>;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for blog management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class BlogEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BlogEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    private async Task<(string Email, string Password, AuthResponse Auth)> CreateTestUserAsync()
    {
        var adminClient = await GetAdminClientAsync();
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "TestPassword123!";

        var createCommand = new CreateUserCommand(
            Email: email,
            Password: password,
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            RoleNames: null); // No roles - regular user without admin permissions

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        // Login as the created user
        var loginCommand = new LoginCommand(email, password);
        var loginResult = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await loginResult.Content.ReadFromJsonAsync<LoginResponse>();

        return (email, password, loginResponse!.Auth!);
    }

    #region Posts Endpoints Tests

    [Fact]
    public async Task GetPosts_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/blog/posts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<BlogPagedResult>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPosts_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/blog/posts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPosts_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/blog/posts?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<BlogPagedResult>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task GetPostById_ValidId_ShouldReturnPost()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a post
        var postRequest = CreateTestPostRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/blog/posts", postRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdPost = await createResponse.Content.ReadFromJsonWithEnumsAsync<PostDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/blog/posts/{createdPost!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var post = await response.Content.ReadFromJsonWithEnumsAsync<PostDto>();
        post.Should().NotBeNull();
        post!.Id.Should().Be(createdPost.Id);
        post.Title.Should().Be(postRequest.Title);
    }

    [Fact]
    public async Task GetPostById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/blog/posts/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPostBySlug_ValidSlug_ShouldReturnPost()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a post
        var postRequest = CreateTestPostRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/blog/posts", postRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdPost = await createResponse.Content.ReadFromJsonWithEnumsAsync<PostDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/blog/posts/by-slug/{createdPost!.Slug}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var post = await response.Content.ReadFromJsonWithEnumsAsync<PostDto>();
        post.Should().NotBeNull();
        post!.Slug.Should().Be(createdPost.Slug);
    }

    [Fact]
    public async Task GetPostBySlug_InvalidSlug_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/blog/posts/by-slug/non-existent-slug-12345");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePost_ValidRequest_ShouldReturnCreatedPost()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestPostRequest();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/blog/posts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var post = await response.Content.ReadFromJsonWithEnumsAsync<PostDto>();
        post.Should().NotBeNull();
        post!.Title.Should().Be(request.Title);
        post.Slug.Should().Be(request.Slug);
    }

    [Fact]
    public async Task CreatePost_DuplicateSlug_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestPostRequest();

        // Create the first post
        await adminClient.PostAsJsonAsync("/api/blog/posts", request);

        // Act - Try to create with same slug
        var response = await adminClient.PostAsJsonAsync("/api/blog/posts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePost_EmptyTitle_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new CreatePostRequest(
            Title: "",
            Slug: $"test-post-{Guid.NewGuid():N}",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/blog/posts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestPostRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/blog/posts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePost_ValidRequest_ShouldReturnUpdatedPost()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a post
        var createRequest = CreateTestPostRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/blog/posts", createRequest);
        var createdPost = await createResponse.Content.ReadFromJsonWithEnumsAsync<PostDto>();

        // Update the post
        var updateRequest = new UpdatePostRequest(
            Title: "Updated Title",
            Slug: createdPost!.Slug,
            Excerpt: "Updated excerpt",
            ContentJson: createdPost.ContentJson,
            ContentHtml: "<p>Updated content</p>",
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/blog/posts/{createdPost.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedPost = await response.Content.ReadFromJsonWithEnumsAsync<PostDto>();
        updatedPost.Should().NotBeNull();
        updatedPost!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdatePost_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdatePostRequest(
            Title: "Updated Title",
            Slug: "some-slug",
            Excerpt: null,
            ContentJson: null,
            ContentHtml: null,
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/blog/posts/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a post
        var request = CreateTestPostRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/blog/posts", request);
        var createdPost = await createResponse.Content.ReadFromJsonWithEnumsAsync<PostDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/blog/posts/{createdPost!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's deleted
        var getResponse = await adminClient.GetAsync($"/api/blog/posts/{createdPost.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/blog/posts/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PublishPost_ValidId_ShouldReturnPublishedPost()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a post
        var request = CreateTestPostRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/blog/posts", request);
        var createdPost = await createResponse.Content.ReadFromJsonWithEnumsAsync<PostDto>();

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/blog/posts/{createdPost!.Id}/publish",
            new PublishPostRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var publishedPost = await response.Content.ReadFromJsonWithEnumsAsync<PostDto>();
        publishedPost.Should().NotBeNull();
        publishedPost!.Status.Should().Be(NOIR.Domain.Enums.PostStatus.Published);
    }

    [Fact]
    public async Task PublishPost_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/blog/posts/{Guid.NewGuid()}/publish",
            new PublishPostRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Categories Endpoints Tests

    [Fact]
    public async Task GetCategories_AsAdmin_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/blog/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PostCategoryListDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCategories_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/blog/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategories_WithSearch_ShouldFilterResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a category
        var categoryRequest = CreateTestCategoryRequest();
        await adminClient.PostAsJsonAsync("/api/blog/categories", categoryRequest);

        // Act
        var response = await adminClient.GetAsync($"/api/blog/categories?search={categoryRequest.Name}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PostCategoryListDto>>();
        result.Should().NotBeNull();
        result.Should().Contain(c => c.Name == categoryRequest.Name);
    }

    [Fact]
    public async Task CreateCategory_ValidRequest_ShouldReturnCreatedCategory()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestCategoryRequest();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/blog/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var category = await response.Content.ReadFromJsonAsync<PostCategoryDto>();
        category.Should().NotBeNull();
        category!.Name.Should().Be(request.Name);
        category.Slug.Should().Be(request.Slug);
    }

    [Fact]
    public async Task CreateCategory_DuplicateSlug_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestCategoryRequest();

        // Create the first category
        await adminClient.PostAsJsonAsync("/api/blog/categories", request);

        // Act - Try to create with same slug
        var response = await adminClient.PostAsJsonAsync("/api/blog/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateCategory_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new CreateCategoryRequest(
            Name: "",
            Slug: $"test-category-{Guid.NewGuid():N}",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/blog/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestCategoryRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/blog/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCategory_ValidRequest_ShouldReturnUpdatedCategory()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a category
        var createRequest = CreateTestCategoryRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/blog/categories", createRequest);
        var createdCategory = await createResponse.Content.ReadFromJsonAsync<PostCategoryDto>();

        // Update the category
        var updateRequest = new UpdateCategoryRequest(
            Name: "Updated Category Name",
            Slug: createdCategory!.Slug,
            Description: "Updated description",
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 1,
            ParentId: null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/blog/categories/{createdCategory.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCategory = await response.Content.ReadFromJsonAsync<PostCategoryDto>();
        updatedCategory.Should().NotBeNull();
        updatedCategory!.Name.Should().Be("Updated Category Name");
    }

    [Fact]
    public async Task UpdateCategory_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateCategoryRequest(
            Name: "Updated Category",
            Slug: "some-slug",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/blog/categories/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a category
        var request = CreateTestCategoryRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/blog/categories", request);
        var createdCategory = await createResponse.Content.ReadFromJsonAsync<PostCategoryDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/blog/categories/{createdCategory!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteCategory_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/blog/categories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Tags Endpoints Tests

    [Fact]
    public async Task GetTags_AsAdmin_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/blog/tags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PostTagListDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTags_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/blog/tags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTags_WithSearch_ShouldFilterResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a tag
        var tagRequest = CreateTestTagRequest();
        await adminClient.PostAsJsonAsync("/api/blog/tags", tagRequest);

        // Act
        var response = await adminClient.GetAsync($"/api/blog/tags?search={tagRequest.Name}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<PostTagListDto>>();
        result.Should().NotBeNull();
        result.Should().Contain(t => t.Name == tagRequest.Name);
    }

    [Fact]
    public async Task CreateTag_ValidRequest_ShouldReturnCreatedTag()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestTagRequest();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/blog/tags", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tag = await response.Content.ReadFromJsonAsync<PostTagDto>();
        tag.Should().NotBeNull();
        tag!.Name.Should().Be(request.Name);
        tag.Slug.Should().Be(request.Slug);
    }

    [Fact]
    public async Task CreateTag_DuplicateSlug_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestTagRequest();

        // Create the first tag
        await adminClient.PostAsJsonAsync("/api/blog/tags", request);

        // Act - Try to create with same slug
        var response = await adminClient.PostAsJsonAsync("/api/blog/tags", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateTag_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new CreateTagRequest(
            Name: "",
            Slug: $"test-tag-{Guid.NewGuid():N}",
            Description: null,
            Color: null);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/blog/tags", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTag_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestTagRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/blog/tags", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTag_ValidRequest_ShouldReturnUpdatedTag()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a tag
        var createRequest = CreateTestTagRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/blog/tags", createRequest);
        var createdTag = await createResponse.Content.ReadFromJsonAsync<PostTagDto>();

        // Update the tag
        var updateRequest = new UpdateTagRequest(
            Name: "Updated Tag Name",
            Slug: createdTag!.Slug,
            Description: "Updated description",
            Color: "#FF5733");

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/blog/tags/{createdTag.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTag = await response.Content.ReadFromJsonAsync<PostTagDto>();
        updatedTag.Should().NotBeNull();
        updatedTag!.Name.Should().Be("Updated Tag Name");
        updatedTag.Color.Should().Be("#FF5733");
    }

    [Fact]
    public async Task UpdateTag_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateTagRequest(
            Name: "Updated Tag",
            Slug: "some-slug",
            Description: null,
            Color: null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/blog/tags/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTag_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a tag
        var request = CreateTestTagRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/blog/tags", request);
        var createdTag = await createResponse.Content.ReadFromJsonAsync<PostTagDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/blog/tags/{createdTag!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteTag_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/blog/tags/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task PostEndpoints_WithoutRequiredPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without admin permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/blog/posts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CategoryEndpoints_WithoutRequiredPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without admin permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/blog/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TagEndpoints_WithoutRequiredPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without admin permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var response = await userClient.GetAsync("/api/blog/tags");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePost_WithoutCreatePermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without admin permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = CreateTestPostRequest();

        // Act
        var response = await userClient.PostAsJsonAsync("/api/blog/posts", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCategory_WithoutCreatePermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without admin permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = CreateTestCategoryRequest();

        // Act
        var response = await userClient.PostAsJsonAsync("/api/blog/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateTag_WithoutCreatePermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without admin permissions
        var (_, _, auth) = await CreateTestUserAsync();
        var userClient = _factory.CreateAuthenticatedClient(auth.AccessToken);
        var request = CreateTestTagRequest();

        // Act
        var response = await userClient.PostAsJsonAsync("/api/blog/tags", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Integration Tests with Categories and Tags

    [Fact]
    public async Task CreatePost_WithCategory_ShouldAssignCategory()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create a category first
        var categoryRequest = CreateTestCategoryRequest();
        var categoryResponse = await adminClient.PostAsJsonAsync("/api/blog/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<PostCategoryDto>();

        // Create a post with the category
        var postRequest = new CreatePostRequest(
            Title: $"Test Post with Category {Guid.NewGuid():N}",
            Slug: $"test-post-with-category-{Guid.NewGuid():N}",
            Excerpt: "Test excerpt",
            ContentJson: null,
            ContentHtml: "<p>Test content</p>",
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: category!.Id,
            TagIds: null);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/blog/posts", postRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var post = await response.Content.ReadFromJsonWithEnumsAsync<PostDto>();
        post.Should().NotBeNull();
        post!.CategoryId.Should().Be(category.Id);
        // CategoryName may not be populated in create response - verify by fetching
        var getResponse = await adminClient.GetAsync($"/api/blog/posts/{post.Id}");
        var fetchedPost = await getResponse.Content.ReadFromJsonWithEnumsAsync<PostDto>();
        fetchedPost!.CategoryId.Should().Be(category.Id);
        fetchedPost.CategoryName.Should().Be(category.Name);
    }

    [Fact]
    public async Task CreatePost_WithTags_ShouldAssignTags()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create tags first
        var tag1Request = CreateTestTagRequest();
        var tag1Response = await adminClient.PostAsJsonAsync("/api/blog/tags", tag1Request);
        var tag1 = await tag1Response.Content.ReadFromJsonAsync<PostTagDto>();

        var tag2Request = CreateTestTagRequest();
        var tag2Response = await adminClient.PostAsJsonAsync("/api/blog/tags", tag2Request);
        var tag2 = await tag2Response.Content.ReadFromJsonAsync<PostTagDto>();

        // Create a post with tags
        var postRequest = new CreatePostRequest(
            Title: $"Test Post with Tags {Guid.NewGuid():N}",
            Slug: $"test-post-with-tags-{Guid.NewGuid():N}",
            Excerpt: "Test excerpt",
            ContentJson: null,
            ContentHtml: "<p>Test content</p>",
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: [tag1!.Id, tag2!.Id]);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/blog/posts", postRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var post = await response.Content.ReadFromJsonWithEnumsAsync<PostDto>();
        post.Should().NotBeNull();
        // Tags may not be populated in create response - verify by fetching
        var getResponse = await adminClient.GetAsync($"/api/blog/posts/{post!.Id}");
        var fetchedPost = await getResponse.Content.ReadFromJsonWithEnumsAsync<PostDto>();
        fetchedPost!.Tags.Should().HaveCount(2);
        fetchedPost.Tags.Should().Contain(t => t.Id == tag1.Id);
        fetchedPost.Tags.Should().Contain(t => t.Id == tag2.Id);
    }

    #endregion

    #region Helper Methods

    private static CreatePostRequest CreateTestPostRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        return new CreatePostRequest(
            Title: $"Test Post {uniqueId}",
            Slug: $"test-post-{uniqueId}",
            Excerpt: "Test excerpt for the post",
            ContentJson: null,
            ContentHtml: "<p>Test content</p>",
            FeaturedImageId: null,
            FeaturedImageUrl: null,
            FeaturedImageAlt: null,
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true,
            CategoryId: null,
            TagIds: null);
    }

    private static CreateCategoryRequest CreateTestCategoryRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        return new CreateCategoryRequest(
            Name: $"Test Category {uniqueId}",
            Slug: $"test-category-{uniqueId}",
            Description: "Test category description",
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);
    }

    private static CreateTagRequest CreateTestTagRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N");
        return new CreateTagRequest(
            Name: $"Test Tag {uniqueId}",
            Slug: $"test-tag-{uniqueId}",
            Description: "Test tag description",
            Color: "#3498db");
    }

    #endregion
}
