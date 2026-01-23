using NOIR.Application.UnitTests.Common;

// Suppress EF1001: EntityEntry<T> constructor is internal API but required for mocking
#pragma warning disable EF1001

namespace NOIR.Application.UnitTests.Features.LegalPages;

/// <summary>
/// Unit tests for UpdateLegalPageCommandHandler.
/// Tests legal page update scenarios with Copy-on-Write pattern.
/// </summary>
public class UpdateLegalPageCommandHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<LegalPage, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly UpdateLegalPageCommandHandler _handler;

    public UpdateLegalPageCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _repositoryMock = new Mock<IRepository<LegalPage, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new UpdateLegalPageCommandHandler(
            _dbContextMock.Object,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
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
                slug,
                title,
                htmlContent,
                metaTitle: "Test Meta Title",
                metaDescription: "Test Meta Description",
                canonicalUrl: null,
                allowIndexing: true)
            : LegalPage.CreateTenantOverride(
                tenantId,
                slug,
                title,
                htmlContent,
                metaTitle: "Test Meta Title",
                metaDescription: "Test Meta Description",
                canonicalUrl: null,
                allowIndexing: true);
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

        // Setup Attach for entity tracking
        _dbContextMock.Setup(x => x.Attach(It.IsAny<LegalPage>()));
    }

    #endregion

    #region Update Existing Tenant Page Tests

    [Fact]
    public async Task Handle_WhenUpdatingExistingTenantPage_ShouldUpdateInPlace()
    {
        // Arrange
        var tenantPage = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Old Title",
            tenantId: TestTenantId);

        SetupDbContextWithPages(tenantPage);

        var command = new UpdateLegalPageCommand(
            Id: tenantPage.Id,
            Title: "New Title",
            HtmlContent: "<p>Updated Content</p>",
            MetaTitle: "New Meta Title",
            MetaDescription: "New Meta Description",
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Title.Should().Be("New Title");
        result.Value.HtmlContent.Should().Be("<p>Updated Content</p>");
        result.Value.IsInherited.Should().BeFalse();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<LegalPage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Copy-on-Write Tests

    [Fact]
    public async Task Handle_WhenUpdatingPlatformPageWithTenantContext_ShouldCreateTenantCopy()
    {
        // Arrange
        var platformPage = CreateTestLegalPage(
            slug: "privacy-policy",
            title: "Platform Privacy Policy",
            tenantId: null); // Platform page

        SetupDbContextWithPages(platformPage);

        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<LegalPage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LegalPage page, CancellationToken _) => page);

        var command = new UpdateLegalPageCommand(
            Id: platformPage.Id,
            Title: "Tenant Privacy Policy",
            HtmlContent: "<p>Custom Privacy Content</p>",
            MetaTitle: "Custom Meta Title",
            MetaDescription: "Custom Meta Description",
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Title.Should().Be("Tenant Privacy Policy");
        result.Value.IsInherited.Should().BeFalse();

        // Should create a new tenant copy
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<LegalPage>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUpdatingPlatformPageAndTenantCopyExists_ShouldUpdateExistingTenantCopy()
    {
        // Arrange
        var platformPage = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Platform Terms",
            tenantId: null);

        var existingTenantCopy = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Existing Tenant Terms",
            tenantId: TestTenantId);

        SetupDbContextWithPages(platformPage, existingTenantCopy);

        var command = new UpdateLegalPageCommand(
            Id: platformPage.Id,
            Title: "Updated Tenant Terms",
            HtmlContent: "<p>Updated Tenant Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Updated Tenant Terms");
        result.Value.IsInherited.Should().BeFalse();

        // Should NOT create new copy - should update existing
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<LegalPage>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Handle_WhenPageNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        SetupDbContextWithPages(); // Empty - no pages

        var command = new UpdateLegalPageCommand(
            Id: Guid.NewGuid(),
            Title: "New Title",
            HtmlContent: "<p>Content</p>",
            MetaTitle: null,
            MetaDescription: null,
            CanonicalUrl: null,
            AllowIndexing: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOIR-LEGAL-001");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region Platform Admin Update Tests

    [Fact]
    public async Task Handle_WhenPlatformUserUpdatingPlatformPage_ShouldUpdateInPlace()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null); // Platform user

        var handler = new UpdateLegalPageCommandHandler(
            _dbContextMock.Object,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);

        var platformPage = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Platform Terms",
            tenantId: null);

        SetupDbContextWithPages(platformPage);

        var command = new UpdateLegalPageCommand(
            Id: platformPage.Id,
            Title: "Updated Platform Terms",
            HtmlContent: "<p>Updated Platform Content</p>",
            MetaTitle: "Meta",
            MetaDescription: "Desc",
            CanonicalUrl: null,
            AllowIndexing: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Updated Platform Terms");

        // Should update in place, not create a copy
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<LegalPage>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
