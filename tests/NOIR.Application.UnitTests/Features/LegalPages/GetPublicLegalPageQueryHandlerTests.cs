using NOIR.Application.UnitTests.Common;

// Suppress EF1001: EntityEntry<T> constructor is internal API but required for mocking
#pragma warning disable EF1001

namespace NOIR.Application.UnitTests.Features.LegalPages;

/// <summary>
/// Unit tests for GetPublicLegalPageQueryHandler.
/// Tests public legal page retrieval by slug with tenant resolution.
/// </summary>
public class GetPublicLegalPageQueryHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetPublicLegalPageQueryHandler _handler;

    public GetPublicLegalPageQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new GetPublicLegalPageQueryHandler(
            _dbContextMock.Object,
            _currentUserMock.Object);
    }

    private static LegalPage CreateTestLegalPage(
        string slug,
        string title = "Test Page",
        string htmlContent = "<p>Test Content</p>",
        string? tenantId = null,
        bool isActive = true)
    {
        var page = tenantId == null
            ? LegalPage.CreatePlatformDefault(
                slug, title, htmlContent,
                metaTitle: "Meta Title",
                metaDescription: "Meta Description",
                canonicalUrl: "https://example.com/" + slug,
                allowIndexing: true)
            : LegalPage.CreateTenantOverride(
                tenantId, slug, title, htmlContent,
                metaTitle: "Meta Title",
                metaDescription: "Meta Description",
                canonicalUrl: "https://example.com/" + slug,
                allowIndexing: true);

        if (!isActive)
        {
            page.Deactivate();
        }

        return page;
    }

    private void SetupDbContextWithPages(params LegalPage[] pages)
    {
        var data = pages.AsQueryable();
        var mockSet = new Mock<DbSet<LegalPage>>();

        mockSet.As<IAsyncEnumerable<LegalPage>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<LegalPage>(data.GetEnumerator()));

        mockSet.As<IQueryable<LegalPage>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<LegalPage>(data.Provider));

        mockSet.As<IQueryable<LegalPage>>()
            .Setup(m => m.Expression)
            .Returns(data.Expression);

        mockSet.As<IQueryable<LegalPage>>()
            .Setup(m => m.ElementType)
            .Returns(data.ElementType);

        mockSet.As<IQueryable<LegalPage>>()
            .Setup(m => m.GetEnumerator())
            .Returns(data.GetEnumerator());

        _dbContextMock.Setup(x => x.LegalPages).Returns(mockSet.Object);
    }

    #endregion

    #region Tenant Resolution Tests

    [Fact]
    public async Task Handle_WhenTenantPageExists_ShouldReturnTenantPage()
    {
        // Arrange
        var platformTerms = CreateTestLegalPage("terms-of-service", "Platform Terms", tenantId: null);
        var tenantTerms = CreateTestLegalPage("terms-of-service", "Tenant Terms", tenantId: TestTenantId);

        SetupDbContextWithPages(platformTerms, tenantTerms);
        var query = new GetPublicLegalPageQuery("terms-of-service");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Title.Should().Be("Tenant Terms"); // Tenant page takes priority
        result.Value.Slug.Should().Be("terms-of-service");
    }

    [Fact]
    public async Task Handle_WhenNoTenantPageExists_ShouldReturnPlatformPage()
    {
        // Arrange
        var platformPrivacy = CreateTestLegalPage("privacy-policy", "Platform Privacy", tenantId: null);

        SetupDbContextWithPages(platformPrivacy);
        var query = new GetPublicLegalPageQuery("privacy-policy");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Platform Privacy");
    }

    [Fact]
    public async Task Handle_WhenOnlyOtherTenantPageExists_ShouldFallbackToPlatform()
    {
        // Arrange
        var platformTerms = CreateTestLegalPage("terms-of-service", "Platform Terms", tenantId: null);
        var otherTenantTerms = CreateTestLegalPage("terms-of-service", "Other Tenant", tenantId: "other-tenant");

        SetupDbContextWithPages(platformTerms, otherTenantTerms);
        var query = new GetPublicLegalPageQuery("terms-of-service");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Platform Terms"); // Not the other tenant's page
    }

    #endregion

    #region Active Status Tests

    [Fact]
    public async Task Handle_WhenPageIsInactive_ShouldReturnNotFound()
    {
        // Arrange
        var inactivePage = CreateTestLegalPage("terms-of-service", "Terms", tenantId: null, isActive: false);

        SetupDbContextWithPages(inactivePage);
        var query = new GetPublicLegalPageQuery("terms-of-service");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOIR-LEGAL-002");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTenantPageInactiveAndPlatformActive_ShouldReturnPlatform()
    {
        // Arrange
        var platformTerms = CreateTestLegalPage("terms-of-service", "Platform Terms", tenantId: null, isActive: true);
        var inactiveTenantTerms = CreateTestLegalPage("terms-of-service", "Inactive Tenant", tenantId: TestTenantId, isActive: false);

        SetupDbContextWithPages(platformTerms, inactiveTenantTerms);
        var query = new GetPublicLegalPageQuery("terms-of-service");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Platform Terms"); // Falls back to platform since tenant is inactive
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task Handle_WhenSlugNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        SetupDbContextWithPages(); // Empty
        var query = new GetPublicLegalPageQuery("non-existent-page");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOIR-LEGAL-002");
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("non-existent-page");
    }

    [Fact]
    public async Task Handle_WhenDifferentSlugExists_ShouldReturnNotFound()
    {
        // Arrange
        var page = CreateTestLegalPage("privacy-policy", "Privacy", tenantId: null);
        SetupDbContextWithPages(page);
        var query = new GetPublicLegalPageQuery("terms-of-service"); // Different slug

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region DTO Mapping Tests

    [Fact]
    public async Task Handle_ShouldReturnAllPublicPageProperties()
    {
        // Arrange
        var page = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Terms of Service",
            htmlContent: "<h1>Terms</h1>",
            tenantId: null);

        SetupDbContextWithPages(page);
        var query = new GetPublicLegalPageQuery("terms-of-service");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Slug.Should().Be("terms-of-service");
        dto.Title.Should().Be("Terms of Service");
        dto.HtmlContent.Should().Be("<h1>Terms</h1>");
        dto.MetaTitle.Should().Be("Meta Title");
        dto.MetaDescription.Should().Be("Meta Description");
        dto.CanonicalUrl.Should().Be("https://example.com/terms-of-service");
        dto.AllowIndexing.Should().BeTrue();
        dto.LastModified.Should().NotBe(default);
    }

    #endregion

    #region Platform User Tests

    [Fact]
    public async Task Handle_WhenPlatformUser_ShouldReturnOnlyPlatformPage()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null); // Platform user (anonymous)
        var handler = new GetPublicLegalPageQueryHandler(_dbContextMock.Object, _currentUserMock.Object);

        var platformTerms = CreateTestLegalPage("terms-of-service", "Platform Terms", tenantId: null);

        SetupDbContextWithPages(platformTerms);
        var query = new GetPublicLegalPageQuery("terms-of-service");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Platform Terms");
    }

    #endregion
}
