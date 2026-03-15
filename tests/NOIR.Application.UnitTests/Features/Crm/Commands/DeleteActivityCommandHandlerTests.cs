using NOIR.Application.Features.Crm.Commands.DeleteActivity;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class DeleteActivityCommandHandlerTests
{
    private readonly Mock<IRepository<CrmActivity, Guid>> _activityRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeleteActivityCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public DeleteActivityCommandHandlerTests()
    {
        _activityRepoMock = new Mock<IRepository<CrmActivity, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeleteActivityCommandHandler(
            _activityRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingActivity_ShouldDelete()
    {
        // Arrange
        var activity = CrmActivity.Create(
            ActivityType.Call, "Follow-up", Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddHours(-1), TestTenantId,
            contactId: Guid.NewGuid());

        _activityRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActivityByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        var command = new DeleteActivityCommand(activity.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _activityRepoMock.Verify(x => x.Remove(activity), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentActivity_ShouldReturnNotFound()
    {
        // Arrange
        _activityRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActivityByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmActivity?)null);

        var command = new DeleteActivityCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _activityRepoMock.Verify(x => x.Remove(It.IsAny<CrmActivity>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
