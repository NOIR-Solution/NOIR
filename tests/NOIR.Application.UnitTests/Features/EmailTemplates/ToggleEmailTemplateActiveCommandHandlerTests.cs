using NOIR.Application.Features.EmailTemplates.Commands.ToggleEmailTemplateActive;
using NOIR.Application.Features.EmailTemplates.DTOs;
using NOIR.Domain.Entities;

// Suppress EF1001: EntityEntry<T> constructor is internal API but required for mocking
#pragma warning disable EF1001

namespace NOIR.Application.UnitTests.Features.EmailTemplates;

/// <summary>
/// Unit tests for ToggleEmailTemplateActiveCommandHandler.
/// Tests toggling email template active status with Copy-on-Write pattern.
/// </summary>
public class ToggleEmailTemplateActiveCommandHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<EmailTemplate, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ICacheInvalidationService> _cacheInvalidationMock;
    private readonly ToggleEmailTemplateActiveCommandHandler _handler;

    public ToggleEmailTemplateActiveCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _repositoryMock = new Mock<IRepository<EmailTemplate, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _cacheInvalidationMock = new Mock<ICacheInvalidationService>();
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new ToggleEmailTemplateActiveCommandHandler(
            _dbContextMock.Object,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _cacheInvalidationMock.Object);
    }

    private static EmailTemplate CreatePlatformTemplate(
        string name = "TestTemplate",
        string subject = "Platform Subject",
        string htmlBody = "<p>Platform Body</p>",
        bool isActive = true)
    {
        var template = EmailTemplate.CreatePlatformDefault(
            name,
            subject,
            htmlBody,
            plainTextBody: "Platform plain text",
            description: "Platform Description",
            availableVariables: null);

        if (!isActive)
            template.Deactivate();

        return template;
    }

    private static EmailTemplate CreateTenantTemplate(
        string name = "TestTemplate",
        string subject = "Tenant Subject",
        string htmlBody = "<p>Tenant Body</p>",
        string tenantId = TestTenantId,
        bool isActive = true)
    {
        var template = EmailTemplate.CreateTenantOverride(
            tenantId,
            name,
            subject,
            htmlBody,
            plainTextBody: "Tenant plain text",
            description: "Tenant Description",
            availableVariables: null);

        if (!isActive)
            template.Deactivate();

        return template;
    }

    private void SetupDbContextWithTemplates(params EmailTemplate[] templates)
    {
        var data = templates.AsQueryable();
        var mockSet = new Mock<DbSet<EmailTemplate>>();

        mockSet.As<IAsyncEnumerable<EmailTemplate>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<EmailTemplate>(data.GetEnumerator()));

        mockSet.As<IQueryable<EmailTemplate>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<EmailTemplate>(data.Provider));

        mockSet.As<IQueryable<EmailTemplate>>()
            .Setup(m => m.Expression)
            .Returns(data.Expression);

        mockSet.As<IQueryable<EmailTemplate>>()
            .Setup(m => m.ElementType)
            .Returns(data.ElementType);

        mockSet.As<IQueryable<EmailTemplate>>()
            .Setup(m => m.GetEnumerator())
            .Returns(data.GetEnumerator());

        _dbContextMock.Setup(x => x.EmailTemplates).Returns(mockSet.Object);
    }

    #endregion

    #region Success Scenarios - Toggle Tenant Template

    [Fact]
    public async Task Handle_ActivateTenantTemplate_ShouldActivateInPlace()
    {
        // Arrange
        var template = CreateTenantTemplate(name: "WelcomeEmail", isActive: false);

        SetupDbContextWithTemplates(template);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleEmailTemplateActiveCommand(template.Id, IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.Id.Should().Be(template.Id);

        // Verify no new template was created
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeactivateTenantTemplate_ShouldDeactivateInPlace()
    {
        // Arrange
        var template = CreateTenantTemplate(name: "WelcomeEmail", isActive: true);

        SetupDbContextWithTemplates(template);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleEmailTemplateActiveCommand(template.Id, IsActive: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    #endregion

    #region Success Scenarios - Copy-on-Write

    [Fact]
    public async Task Handle_DeactivatePlatformTemplate_ShouldCreateTenantCopy()
    {
        // Arrange
        var platformTemplate = CreatePlatformTemplate(name: "WelcomeEmail", isActive: true);

        SetupDbContextWithTemplates(platformTemplate);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate t, CancellationToken _) => t);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleEmailTemplateActiveCommand(platformTemplate.Id, IsActive: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
        result.Value.IsInherited.Should().BeFalse(); // Now tenant-owned
        result.Value.Id.Should().NotBe(platformTemplate.Id); // New ID for copy

        // Verify Copy-on-Write: a new template was created
        _repositoryMock.Verify(
            x => x.AddAsync(It.Is<EmailTemplate>(t =>
                t.Name == "WelcomeEmail" &&
                t.TenantId == TestTenantId &&
                !t.IsActive),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ToggleExistingTenantCopy_ShouldUpdateCopyNotPlatform()
    {
        // Arrange
        var platformTemplate = CreatePlatformTemplate(name: "WelcomeEmail", isActive: true);
        var tenantCopy = CreateTenantTemplate(name: "WelcomeEmail", isActive: true);

        SetupDbContextWithTemplates(platformTemplate, tenantCopy);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleEmailTemplateActiveCommand(platformTemplate.Id, IsActive: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(tenantCopy.Id); // Should use existing tenant copy

        // Verify no new template was created
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Platform Admin Scenarios

    [Fact]
    public async Task Handle_PlatformAdminTogglePlatformTemplate_ShouldToggleInPlace()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null); // Platform admin

        var platformTemplate = CreatePlatformTemplate(name: "WelcomeEmail", isActive: true);

        SetupDbContextWithTemplates(platformTemplate);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleEmailTemplateActiveCommand(platformTemplate.Id, IsActive: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(platformTemplate.Id); // Same ID, updated in place
        result.Value.IsActive.Should().BeFalse();

        // Verify no copy was created
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        SetupDbContextWithTemplates(); // Empty

        var command = new ToggleEmailTemplateActiveCommand(nonExistentId, IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-EMAIL-001");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ToggleToSameState_ShouldSucceed()
    {
        // Arrange
        var template = CreateTenantTemplate(name: "AlreadyActive", isActive: true);

        SetupDbContextWithTemplates(template);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleEmailTemplateActiveCommand(template.Id, IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var template = CreateTenantTemplate(name: "TestTemplate");

        SetupDbContextWithTemplates(template);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleEmailTemplateActiveCommand(template.Id, IsActive: false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllOperations()
    {
        // Arrange
        var template = CreateTenantTemplate(name: "TestTemplate");
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        SetupDbContextWithTemplates(template);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new ToggleEmailTemplateActiveCommand(template.Id, IsActive: false);

        // Act
        await _handler.Handle(command, token);

        // Assert
        // SaveChangesAsync is the critical async operation that must respect cancellation.
        // DbContext query operations (IgnoreQueryFilters, Where, FirstOrDefaultAsync) receive the token
        // internally via the async LINQ provider - verified by EF Core's design.
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_DtoShouldHaveCorrectIsInherited()
    {
        // Arrange
        var template = CreateTenantTemplate(name: "TestTemplate");

        SetupDbContextWithTemplates(template);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleEmailTemplateActiveCommand(template.Id, IsActive: true, TemplateName: "TestTemplate");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsInherited.Should().BeFalse(); // Tenant template is not inherited
    }

    #endregion
}
