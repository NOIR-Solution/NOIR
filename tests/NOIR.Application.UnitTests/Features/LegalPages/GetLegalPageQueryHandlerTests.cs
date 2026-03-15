using NOIR.Application.UnitTests.Common;

// Suppress EF1001: EntityEntry<T> constructor is internal API but required for mocking
#pragma warning disable EF1001

namespace NOIR.Application.UnitTests.Features.LegalPages;

/// <summary>
/// Unit tests for GetLegalPageQueryHandler.
/// Tests fetching a single legal page by ID with inheritance resolution.
/// </summary>
public class GetLegalPageQueryHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetLegalPageQueryHandler _handler;

    public GetLegalPageQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new GetLegalPageQueryHandler(
            _dbContextMock.Object,
            _currentUserMock.Object);
    }

    private static LegalPage CreateTestLegalPage(
        string slug = "test-page",
        string title = "Test Page",
        string htmlContent = "<p>Test Content</p>",
        string? tenantId = null)
    {
        return tenantId == null
            ? LegalPage.CreatePlatformDefault(
                slug, title, htmlContent,
                metaTitle: "Meta Title",
                metaDescription: "Meta Description")
            : LegalPage.CreateTenantOverride(
                tenantId, slug, title, htmlContent,
                metaTitle: "Meta Title",
                metaDescription: "Meta Description");
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

    #region Success Tests

    [Fact]
    public async Task Handle_WhenTenantPageExists_ShouldReturnNotInherited()
    {
        // Arrange
        var tenantPage = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Tenant Terms",
            tenantId: TestTenantId);

        SetupDbContextWithPages(tenantPage);
        var query = new GetLegalPageQuery(tenantPage.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(tenantPage.Id);
        result.Value.Title.ShouldBe("Tenant Terms");
        result.Value.Slug.ShouldBe("terms-of-service");
        result.Value.IsInherited.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_WhenPlatformPageViaTenant_ShouldReturnAsInherited()
    {
        // Arrange
        var platformPage = CreateTestLegalPage(
            slug: "privacy-policy",
            title: "Platform Privacy",
            tenantId: null); // Platform page

        SetupDbContextWithPages(platformPage);
        var query = new GetLegalPageQuery(platformPage.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Title.ShouldBe("Platform Privacy");
        result.Value.IsInherited.ShouldBe(true); // Tenant sees platform page as inherited
    }

    [Fact]
    public async Task Handle_WhenPlatformUserViewsPlatformPage_ShouldReturnNotInherited()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null); // Platform user
        var handler = new GetLegalPageQueryHandler(_dbContextMock.Object, _currentUserMock.Object);

        var platformPage = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Platform Terms",
            tenantId: null);

        SetupDbContextWithPages(platformPage);
        var query = new GetLegalPageQuery(platformPage.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsInherited.ShouldBe(false); // Platform users own platform pages
    }

    [Fact]
    public async Task Handle_ShouldReturnAllPageProperties()
    {
        // Arrange
        var page = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Terms of Service",
            htmlContent: "<h1>Terms</h1><p>Please read.</p>",
            tenantId: TestTenantId);

        SetupDbContextWithPages(page);
        var query = new GetLegalPageQuery(page.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;
        dto.Id.ShouldBe(page.Id);
        dto.Slug.ShouldBe("terms-of-service");
        dto.Title.ShouldBe("Terms of Service");
        dto.HtmlContent.ShouldBe("<h1>Terms</h1><p>Please read.</p>");
        dto.MetaTitle.ShouldBe("Meta Title");
        dto.MetaDescription.ShouldBe("Meta Description");
        dto.AllowIndexing.ShouldBe(true);
        dto.IsActive.ShouldBe(true);
        dto.Version.ShouldBe(1);
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task Handle_WhenPageNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        SetupDbContextWithPages(); // Empty
        var query = new GetLegalPageQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Code.ShouldBe("NOIR-LEGAL-001");
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenPageExistsButDifferentId_ShouldReturnNotFoundError()
    {
        // Arrange
        var page = CreateTestLegalPage(tenantId: TestTenantId);
        SetupDbContextWithPages(page);
        var query = new GetLegalPageQuery(Guid.NewGuid()); // Different ID

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    #endregion
}
