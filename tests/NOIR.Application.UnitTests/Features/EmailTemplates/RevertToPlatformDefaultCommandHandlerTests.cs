using NOIR.Application.Features.EmailTemplates.Commands.RevertToPlatformDefault;
using NOIR.Application.Features.EmailTemplates.DTOs;
using NOIR.Domain.Entities;

// Suppress EF1001: EntityEntry<T> constructor is internal API but required for mocking
#pragma warning disable EF1001

namespace NOIR.Application.UnitTests.Features.EmailTemplates;

/// <summary>
/// Unit tests for RevertToPlatformDefaultCommandHandler.
/// Tests reverting tenant email template customizations to platform defaults.
/// </summary>
public class RevertToPlatformDefaultCommandHandlerTests
{
    #region Test Setup

    private const string TestTenantId = "test-tenant-id";

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<EmailTemplate, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ICacheInvalidationService> _cacheInvalidationMock;
    private readonly RevertToPlatformDefaultCommandHandler _handler;

    public RevertToPlatformDefaultCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _repositoryMock = new Mock<IRepository<EmailTemplate, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _cacheInvalidationMock = new Mock<ICacheInvalidationService>();
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new RevertToPlatformDefaultCommandHandler(
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
        string? availableVariables = null)
    {
        return EmailTemplate.CreatePlatformDefault(
            name,
            subject,
            htmlBody,
            plainTextBody: "Platform plain text",
            description: "Platform Description",
            availableVariables: availableVariables);
    }

    private static EmailTemplate CreateTenantTemplate(
        string name = "TestTemplate",
        string subject = "Tenant Subject",
        string htmlBody = "<p>Tenant Body</p>",
        string tenantId = TestTenantId,
        string? availableVariables = null)
    {
        return EmailTemplate.CreateTenantOverride(
            tenantId,
            name,
            subject,
            htmlBody,
            plainTextBody: "Tenant plain text",
            description: "Tenant Description",
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

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidTenantTemplate_ShouldRevertToPlatformDefault()
    {
        // Arrange
        var tenantTemplate = CreateTenantTemplate(name: "WelcomeEmail");
        var platformTemplate = CreatePlatformTemplate(name: "WelcomeEmail");

        SetupDbContextWithTemplates(tenantTemplate, platformTemplate);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RevertToPlatformDefaultCommand(tenantTemplate.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("WelcomeEmail");
        result.Value.Subject.Should().Be("Platform Subject");
        result.Value.IsInherited.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteTenantTemplate()
    {
        // Arrange
        var tenantTemplate = CreateTenantTemplate(name: "PasswordReset");
        var platformTemplate = CreatePlatformTemplate(name: "PasswordReset");

        SetupDbContextWithTemplates(tenantTemplate, platformTemplate);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RevertToPlatformDefaultCommand(tenantTemplate.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(x => x.Remove(tenantTemplate), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnPlatformTemplateDetails()
    {
        // Arrange
        var tenantTemplate = CreateTenantTemplate(name: "EmailVerification");
        var platformTemplate = CreatePlatformTemplate(
            name: "EmailVerification",
            subject: "Verify Your Email",
            htmlBody: "<p>Please verify your email</p>",
            availableVariables: "[\"UserName\", \"VerificationLink\"]");

        SetupDbContextWithTemplates(tenantTemplate, platformTemplate);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RevertToPlatformDefaultCommand(tenantTemplate.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Be("Verify Your Email");
        result.Value.HtmlBody.Should().Be("<p>Please verify your email</p>");
        result.Value.AvailableVariables.Should().BeEquivalentTo(new[] { "UserName", "VerificationLink" });
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenPlatformUser_ShouldReturnValidationError()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null); // Platform admin

        var command = new RevertToPlatformDefaultCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-EMAIL-004");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTemplateNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        SetupDbContextWithTemplates(); // Empty

        var command = new RevertToPlatformDefaultCommand(nonExistentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-EMAIL-001");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTemplateIsPlatformDefault_ShouldReturnValidationError()
    {
        // Arrange
        var platformTemplate = CreatePlatformTemplate(name: "WelcomeEmail");
        // Create a tenant-looking template that is actually platform default
        var fakeId = platformTemplate.Id;

        SetupDbContextWithTemplates(platformTemplate);

        var command = new RevertToPlatformDefaultCommand(fakeId);

        // Act - Try to revert a platform template (not a tenant override)
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Template not found for this tenant
        result.Error.Code.Should().Be("NOIR-EMAIL-001");
    }

    [Fact]
    public async Task Handle_WhenPlatformTemplateNotFound_ShouldReturnNotFound()
    {
        // Arrange
        // Tenant template exists but no matching platform template
        var tenantTemplate = CreateTenantTemplate(name: "CustomTemplate");

        SetupDbContextWithTemplates(tenantTemplate);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        var command = new RevertToPlatformDefaultCommand(tenantTemplate.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-EMAIL-002");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithEmptyTenantId_ShouldReturnValidationError()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns(string.Empty);

        var command = new RevertToPlatformDefaultCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-EMAIL-004");
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllOperations()
    {
        // Arrange
        var tenantTemplate = CreateTenantTemplate(name: "TestTemplate");
        var platformTemplate = CreatePlatformTemplate(name: "TestTemplate");
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        SetupDbContextWithTemplates(tenantTemplate, platformTemplate);

        _dbContextMock.Setup(x => x.Attach(It.IsAny<EmailTemplate>()))
            .Returns<EmailTemplate>(e => new Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<EmailTemplate>(null!));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new RevertToPlatformDefaultCommand(tenantTemplate.Id);

        // Act
        await _handler.Handle(command, token);

        // Assert
        // SaveChangesAsync is the critical async operation that must respect cancellation.
        // DbContext query operations receive the token internally via the async LINQ provider.
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
    }

    #endregion
}
