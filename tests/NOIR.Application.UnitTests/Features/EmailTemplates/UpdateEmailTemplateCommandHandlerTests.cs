using NOIR.Application.Features.EmailTemplates.Commands.UpdateEmailTemplate;
using NOIR.Application.Features.EmailTemplates.DTOs;
using NOIR.Domain.Entities;

// Suppress EF1001: EntityEntry<T> constructor is internal API but required for mocking
#pragma warning disable EF1001

namespace NOIR.Application.UnitTests.Features.EmailTemplates;

/// <summary>
/// Unit tests for UpdateEmailTemplateCommandHandler.
/// Tests email template update scenarios with Copy-on-Write pattern.
/// </summary>
public class UpdateEmailTemplateCommandHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<EmailTemplate, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ICacheInvalidationService> _cacheInvalidationMock;
    private readonly UpdateEmailTemplateCommandHandler _handler;

    public UpdateEmailTemplateCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _repositoryMock = new Mock<IRepository<EmailTemplate, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _cacheInvalidationMock = new Mock<ICacheInvalidationService>();
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new UpdateEmailTemplateCommandHandler(
            _dbContextMock.Object,
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _cacheInvalidationMock.Object);
    }

    private static EmailTemplate CreateTestEmailTemplate(
        string name = "TestTemplate",
        string subject = "Test Subject",
        string htmlBody = "<p>Test Body</p>",
        string? availableVariables = null,
        string? tenantId = null)
    {
        return tenantId == null
            ? EmailTemplate.CreatePlatformDefault(
                name,
                subject,
                htmlBody,
                plainTextBody: null,
                description: "Test Description",
                availableVariables: availableVariables)
            : EmailTemplate.CreateTenantOverride(
                tenantId,
                name,
                subject,
                htmlBody,
                plainTextBody: null,
                description: "Test Description",
                availableVariables: availableVariables);
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

    #region Success Scenarios - Update Tenant Template

    [Fact]
    public async Task Handle_UpdateTenantTemplate_ShouldUpdateInPlace()
    {
        // Arrange
        var template = CreateTestEmailTemplate(
            name: "WelcomeEmail",
            subject: "Old Subject",
            htmlBody: "<p>Old Body</p>",
            tenantId: TestTenantId); // Tenant owns this template

        SetupDbContextWithTemplates(template);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            template.Id,
            "New Subject",
            "<p>New Body</p>",
            "New plain text",
            "Updated description");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Be("New Subject");
        result.Value.HtmlBody.Should().Be("<p>New Body</p>");
        result.Value.IsInherited.Should().BeFalse();

        // Verify we did NOT create a new template
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdateTenantTemplate_ShouldIncrementVersion()
    {
        // Arrange
        var template = CreateTestEmailTemplate(tenantId: TestTenantId);

        SetupDbContextWithTemplates(template);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            template.Id,
            "New Subject",
            "<p>New Body</p>",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Version.Should().Be(2); // Version incremented from 1 to 2
    }

    #endregion

    #region Success Scenarios - Copy-on-Write

    [Fact]
    public async Task Handle_EditPlatformTemplate_ShouldCreateTenantCopy()
    {
        // Arrange
        var platformTemplate = CreateTestEmailTemplate(
            name: "WelcomeEmail",
            subject: "Platform Subject",
            htmlBody: "<p>Platform Body</p>",
            availableVariables: "[\"UserName\"]",
            tenantId: null); // Platform template

        SetupDbContextWithTemplates(platformTemplate);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate t, CancellationToken _) => t);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            platformTemplate.Id,
            "Tenant Subject",
            "<p>Tenant Body</p>",
            "Tenant plain text",
            "Tenant description");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Be("Tenant Subject");
        result.Value.HtmlBody.Should().Be("<p>Tenant Body</p>");
        result.Value.IsInherited.Should().BeFalse(); // After save, it's tenant-owned
        result.Value.Id.Should().NotBe(platformTemplate.Id); // New ID for copy

        // Verify Copy-on-Write: a new template was created
        _repositoryMock.Verify(
            x => x.AddAsync(It.Is<EmailTemplate>(t =>
                t.Name == "WelcomeEmail" &&
                t.Subject == "Tenant Subject" &&
                t.TenantId == TestTenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EditPlatformTemplate_ShouldPreserveAvailableVariables()
    {
        // Arrange
        var platformTemplate = CreateTestEmailTemplate(
            name: "PasswordReset",
            availableVariables: "[\"UserName\", \"OtpCode\"]",
            tenantId: null);

        SetupDbContextWithTemplates(platformTemplate);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<EmailTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplate t, CancellationToken _) => t);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            platformTemplate.Id,
            "New Subject",
            "<p>New Body</p>",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvailableVariables.Should().BeEquivalentTo(new[] { "UserName", "OtpCode" });
    }

    #endregion

    #region Platform Admin Scenarios

    [Fact]
    public async Task Handle_PlatformAdminEditPlatformTemplate_ShouldUpdateInPlace()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null); // Platform admin

        var platformTemplate = CreateTestEmailTemplate(
            name: "WelcomeEmail",
            subject: "Old Subject",
            tenantId: null);

        SetupDbContextWithTemplates(platformTemplate);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            platformTemplate.Id,
            "New Subject",
            "<p>New Body</p>",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(platformTemplate.Id); // Same ID, updated in place

        // Verify no copy was created
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<EmailTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        SetupDbContextWithTemplates(); // Empty

        var command = new UpdateEmailTemplateCommand(
            nonExistentId,
            "New Subject",
            "<p>New Body</p>",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-EMAIL-001");
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldCallSaveChanges()
    {
        // Arrange
        var template = CreateTestEmailTemplate(tenantId: TestTenantId);

        SetupDbContextWithTemplates(template);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            template.Id,
            "New Subject",
            "<p>New Body</p>",
            null,
            null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullPlainTextBody_ShouldUpdateSuccessfully()
    {
        // Arrange
        var template = CreateTestEmailTemplate(tenantId: TestTenantId);

        SetupDbContextWithTemplates(template);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEmailTemplateCommand(
            template.Id,
            "New Subject",
            "<p>New Body</p>",
            null,  // No plain text body
            null); // No description

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlainTextBody.Should().BeNull();
    }

    #endregion
}
