using NOIR.Application.UnitTests.Common;

// Suppress EF1001: EntityEntry<T> constructor is internal API but required for mocking
#pragma warning disable EF1001

namespace NOIR.Application.UnitTests.Features.LegalPages;

/// <summary>
/// Unit tests for GetLegalPagesQueryHandler.
/// Tests fetching all legal pages with Copy-on-Write inheritance resolution.
/// </summary>
public class GetLegalPagesQueryHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetLegalPagesQueryHandler _handler;

    public GetLegalPagesQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new GetLegalPagesQueryHandler(
            _dbContextMock.Object,
            _currentUserMock.Object);
    }

    private static LegalPage CreateTestLegalPage(
        string slug,
        string title = "Test Page",
        string? tenantId = null,
        bool isDeleted = false)
    {
        var page = tenantId == null
            ? LegalPage.CreatePlatformDefault(slug, title, "<p>Content</p>")
            : LegalPage.CreateTenantOverride(tenantId, slug, title, "<p>Content</p>");

        if (isDeleted)
        {
            // Use reflection to set IsDeleted since it's protected
            var prop = typeof(LegalPage).BaseType?.BaseType?.GetProperty("IsDeleted");
            prop?.SetValue(page, true);
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

    #region Basic Query Tests

    [Fact]
    public async Task Handle_WhenNoPages_ShouldReturnEmptyList()
    {
        // Arrange
        SetupDbContextWithPages();
        var query = new GetLegalPagesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenOnlyPlatformPagesExist_ShouldReturnAsInherited()
    {
        // Arrange
        var termsPage = CreateTestLegalPage("terms-of-service", "Terms", tenantId: null);
        var privacyPage = CreateTestLegalPage("privacy-policy", "Privacy", tenantId: null);

        SetupDbContextWithPages(termsPage, privacyPage);
        var query = new GetLegalPagesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(2);
        result.Value.ShouldAllBe(p => p.IsInherited == true);
        result.Value.Select(p => p.Slug).ShouldContain("terms-of-service");
        result.Value.Select(p => p.Slug).ShouldContain("privacy-policy");
    }

    #endregion

    #region Copy-on-Write Inheritance Tests

    [Fact]
    public async Task Handle_WhenTenantHasCustomPage_ShouldReturnTenantVersionNotInherited()
    {
        // Arrange
        var platformTerms = CreateTestLegalPage("terms-of-service", "Platform Terms", tenantId: null);
        var tenantTerms = CreateTestLegalPage("terms-of-service", "Tenant Terms", tenantId: TestTenantId);

        SetupDbContextWithPages(platformTerms, tenantTerms);
        var query = new GetLegalPagesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(1);
        result.Value[0].Title.ShouldBe("Tenant Terms");
        result.Value[0].IsInherited.ShouldBe(false);
        result.Value[0].Id.ShouldBe(tenantTerms.Id);
    }

    [Fact]
    public async Task Handle_WhenMixedPages_ShouldCorrectlyResolveInheritance()
    {
        // Arrange
        var platformTerms = CreateTestLegalPage("terms-of-service", "Platform Terms", tenantId: null);
        var tenantTerms = CreateTestLegalPage("terms-of-service", "Tenant Terms", tenantId: TestTenantId);
        var platformPrivacy = CreateTestLegalPage("privacy-policy", "Platform Privacy", tenantId: null);
        // No tenant privacy - should inherit from platform

        SetupDbContextWithPages(platformTerms, tenantTerms, platformPrivacy);
        var query = new GetLegalPagesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(2);

        var terms = result.Value.FirstOrDefault(p => p.Slug == "terms-of-service");
        terms.ShouldNotBeNull();
        terms!.Title.ShouldBe("Tenant Terms");
        terms.IsInherited.ShouldBe(false);

        var privacy = result.Value.FirstOrDefault(p => p.Slug == "privacy-policy");
        privacy.ShouldNotBeNull();
        privacy!.Title.ShouldBe("Platform Privacy");
        privacy.IsInherited.ShouldBe(true);
    }

    #endregion

    #region Platform User Tests

    [Fact]
    public async Task Handle_WhenPlatformUser_ShouldReturnOnlyPlatformPagesNotInherited()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        var handler = new GetLegalPagesQueryHandler(_dbContextMock.Object, _currentUserMock.Object);

        var platformTerms = CreateTestLegalPage("terms-of-service", "Platform Terms", tenantId: null);
        var otherTenantTerms = CreateTestLegalPage("terms-of-service", "Other Tenant Terms", tenantId: "other-tenant");

        SetupDbContextWithPages(platformTerms, otherTenantTerms);
        var query = new GetLegalPagesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(1);
        result.Value[0].Title.ShouldBe("Platform Terms");
        result.Value[0].IsInherited.ShouldBe(false); // Platform pages are not inherited for platform users
    }

    #endregion

    #region Ordering Tests

    [Fact]
    public async Task Handle_ShouldReturnPagesOrderedBySlug()
    {
        // Arrange
        var zPage = CreateTestLegalPage("z-page", "Z Page", tenantId: null);
        var aPage = CreateTestLegalPage("a-page", "A Page", tenantId: null);
        var mPage = CreateTestLegalPage("m-page", "M Page", tenantId: null);

        SetupDbContextWithPages(zPage, aPage, mPage);
        var query = new GetLegalPagesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Count().ShouldBe(3);
        result.Value[0].Slug.ShouldBe("a-page");
        result.Value[1].Slug.ShouldBe("m-page");
        result.Value[2].Slug.ShouldBe("z-page");
    }

    #endregion
}
