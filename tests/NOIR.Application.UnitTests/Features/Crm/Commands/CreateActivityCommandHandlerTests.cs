using NOIR.Application.Features.Crm.Commands.CreateActivity;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class CreateActivityCommandHandlerTests
{
    private readonly Mock<IRepository<CrmActivity, Guid>> _activityRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateActivityCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public CreateActivityCommandHandlerTests()
    {
        _activityRepoMock = new Mock<IRepository<CrmActivity, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _activityRepoMock
            .Setup(x => x.AddAsync(It.IsAny<CrmActivity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmActivity a, CancellationToken _) => a);

        _handler = new CreateActivityCommandHandler(
            _activityRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateActivity()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var performedById = Guid.NewGuid();

        var command = new CreateActivityCommand(
            ActivityType.Call, "Follow-up call", performedById,
            DateTimeOffset.UtcNow.AddHours(-1),
            Description: "Discussed pricing",
            ContactId: contactId,
            DurationMinutes: 30);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(ActivityType.Call);
        result.Value.Subject.Should().Be("Follow-up call");

        _activityRepoMock.Verify(
            x => x.AddAsync(It.IsAny<CrmActivity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoContactOrLead_ShouldReturnError()
    {
        // Arrange
        var command = new CreateActivityCommand(
            ActivityType.Note, "General note", Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(-1));
        // Neither ContactId nor LeadId set

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _activityRepoMock.Verify(
            x => x.AddAsync(It.IsAny<CrmActivity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FuturePerformedAt_ShouldReturnError()
    {
        // Arrange
        var command = new CreateActivityCommand(
            ActivityType.Meeting, "Future meeting", Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(2), // Far in the future
            ContactId: Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _activityRepoMock.Verify(
            x => x.AddAsync(It.IsAny<CrmActivity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithBothContactAndLead_ShouldSucceed()
    {
        // Arrange
        var command = new CreateActivityCommand(
            ActivityType.Email, "Deal discussion", Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(-30),
            ContactId: Guid.NewGuid(),
            LeadId: Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
