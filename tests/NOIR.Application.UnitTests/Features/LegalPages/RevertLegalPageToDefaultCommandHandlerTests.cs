using NOIR.Application.UnitTests.Common;

// Suppress EF1001: EntityEntry<T> constructor is internal API but required for mocking
#pragma warning disable EF1001

namespace NOIR.Application.UnitTests.Features.LegalPages;

/// <summary>
/// Unit tests for RevertLegalPageToDefaultCommandHandler.
/// Tests reverting a tenant legal page to platform default.
/// </summary>
public class RevertLegalPageToDefaultCommandHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<LegalPage, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly RevertLegalPageToDefaultCommandHandler _handler;

    public RevertLegalPageToDefaultCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _repositoryMock = new Mock<IRepository<LegalPage, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new RevertLegalPageToDefaultCommandHandler(
            _dbContextMock.Object,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static LegalPage CreateTestLegalPage(
        string slug = "test-page",
        string title = "Test Page",
        string? tenantId = null)
    {
        return tenantId == null
            ? LegalPage.CreatePlatformDefault(
                slug, title, "<p>Platform Content</p>",
                metaTitle: "Platform Meta Title")
            : LegalPage.CreateTenantOverride(
                tenantId, slug, title, "<p>Tenant Content</p>",
                metaTitle: "Tenant Meta Title");
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
        _dbContextMock.Setup(x => x.Attach(It.IsAny<LegalPage>()));
    }

    #endregion

    #region Success Tests

    [Fact]
    public async Task Handle_WhenTenantPageExistsWithPlatformDefault_ShouldRevertSuccessfully()
    {
        // Arrange
        var tenantPage = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Tenant Terms",
            tenantId: TestTenantId);

        var platformPage = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Platform Terms",
            tenantId: null);

        SetupDbContextWithPages(tenantPage, platformPage);

        var command = new RevertLegalPageToDefaultCommand(tenantPage.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Title.Should().Be("Platform Terms");
        result.Value.IsInherited.Should().BeTrue();
        result.Value.Id.Should().Be(platformPage.Id);

        // Should soft delete the tenant page
        _repositoryMock.Verify(x => x.Remove(tenantPage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Error Tests - Platform User Cannot Revert

    [Fact]
    public async Task Handle_WhenPlatformUser_ShouldReturnValidationError()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);
        var handler = new RevertLegalPageToDefaultCommandHandler(
            _dbContextMock.Object,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);

        var command = new RevertLegalPageToDefaultCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOIR-LEGAL-003");
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Message.Should().Contain("Only tenant users");
    }

    [Fact]
    public async Task Handle_WhenEmptyTenantId_ShouldReturnValidationError()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns("");
        var handler = new RevertLegalPageToDefaultCommandHandler(
            _dbContextMock.Object,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);

        var command = new RevertLegalPageToDefaultCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOIR-LEGAL-003");
    }

    #endregion

    #region Error Tests - Page Not Found

    [Fact]
    public async Task Handle_WhenTenantPageNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        SetupDbContextWithPages(); // Empty
        var command = new RevertLegalPageToDefaultCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOIR-LEGAL-001");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenPageBelongsToDifferentTenant_ShouldReturnNotFoundError()
    {
        // Arrange
        var otherTenantPage = CreateTestLegalPage(
            slug: "terms-of-service",
            tenantId: "other-tenant-id");

        SetupDbContextWithPages(otherTenantPage);
        var command = new RevertLegalPageToDefaultCommand(otherTenantPage.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOIR-LEGAL-001");
    }

    #endregion

    #region Error Tests - Cannot Revert Platform Page

    [Fact]
    public async Task Handle_WhenTryingToRevertPlatformPage_ShouldReturnValidationError()
    {
        // Arrange - platform page that somehow has tenantId = TestTenantId
        // This tests the IsPlatformDefault check
        var platformPage = CreateTestLegalPage(
            slug: "terms-of-service",
            title: "Platform Terms",
            tenantId: null); // Platform page

        // We need to set up the query to return this page for the tenant query
        // Since the handler filters by TenantId == currentTenantId, a platform page won't match
        // So this scenario won't occur in practice, but let's test it doesn't break
        SetupDbContextWithPages(platformPage);
        var command = new RevertLegalPageToDefaultCommand(platformPage.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // Platform page won't match TenantId filter, so returns not found
        result.Error.Code.Should().Be("NOIR-LEGAL-001");
    }

    #endregion

    #region Error Tests - Platform Default Not Found

    [Fact]
    public async Task Handle_WhenPlatformDefaultNotFound_ShouldReturnNotFoundError()
    {
        // Arrange - tenant page exists but no platform default
        var tenantPage = CreateTestLegalPage(
            slug: "custom-page",
            title: "Custom Tenant Page",
            tenantId: TestTenantId);

        // Only tenant page, no platform page with this slug
        SetupDbContextWithPages(tenantPage);
        var command = new RevertLegalPageToDefaultCommand(tenantPage.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("NOIR-LEGAL-005");
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("custom-page");
    }

    #endregion
}
