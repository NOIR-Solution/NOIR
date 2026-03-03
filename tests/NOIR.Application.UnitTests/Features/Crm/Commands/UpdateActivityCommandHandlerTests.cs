using NOIR.Application.Features.Crm.Commands.UpdateActivity;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class UpdateActivityCommandHandlerTests
{
    private readonly Mock<IRepository<CrmActivity, Guid>> _activityRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateActivityCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public UpdateActivityCommandHandlerTests()
    {
        _activityRepoMock = new Mock<IRepository<CrmActivity, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateActivityCommandHandler(
            _activityRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingActivity_ShouldUpdate()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var activity = CrmActivity.Create(
            ActivityType.Call, "Original subject", Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(-2), TestTenantId,
            contactId: contactId);

        _activityRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActivityByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        var command = new UpdateActivityCommand(
            activity.Id, ActivityType.Meeting, "Updated subject",
            DateTimeOffset.UtcNow.AddHours(-1),
            Description: "Updated description",
            ContactId: contactId,
            DurationMinutes: 60);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        activity.Type.Should().Be(ActivityType.Meeting);
        activity.Subject.Should().Be("Updated subject");
        activity.Description.Should().Be("Updated description");
        activity.DurationMinutes.Should().Be(60);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentActivity_ShouldReturnNotFound()
    {
        // Arrange
        _activityRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActivityByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmActivity?)null);

        var command = new UpdateActivityCommand(
            Guid.NewGuid(), ActivityType.Call, "Subject",
            DateTimeOffset.UtcNow.AddHours(-1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
