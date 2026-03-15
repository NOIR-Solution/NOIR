using NOIR.Application.Features.Crm.Queries.GetActivities;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Queries;

public class GetActivitiesQueryHandlerTests
{
    private readonly Mock<IRepository<CrmActivity, Guid>> _activityRepoMock;
    private readonly GetActivitiesQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetActivitiesQueryHandlerTests()
    {
        _activityRepoMock = new Mock<IRepository<CrmActivity, Guid>>();

        _handler = new GetActivitiesQueryHandler(_activityRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ByContact_ShouldReturnActivities()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var performedById = Guid.NewGuid();

        var activity = CrmActivity.Create(
            ActivityType.Call, "Follow-up", performedById,
            DateTimeOffset.UtcNow.AddHours(-1), TestTenantId,
            contactId: contactId);

        _activityRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ActivitiesFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmActivity> { activity });

        _activityRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ActivitiesCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetActivitiesQuery(ContactId: contactId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1);
        result.Value.Items[0].Subject.ShouldBe("Follow-up");
        result.Value.Items[0].Type.ShouldBe(ActivityType.Call);
    }

    [Fact]
    public async Task Handle_ByLead_ShouldReturnActivities()
    {
        // Arrange
        var leadId = Guid.NewGuid();
        var performedById = Guid.NewGuid();

        var activity = CrmActivity.Create(
            ActivityType.Meeting, "Deal review", performedById,
            DateTimeOffset.UtcNow.AddHours(-2), TestTenantId,
            leadId: leadId, durationMinutes: 60);

        _activityRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ActivitiesFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmActivity> { activity });

        _activityRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ActivitiesCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetActivitiesQuery(LeadId: leadId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1);
        result.Value.Items[0].Subject.ShouldBe("Deal review");
        result.Value.Items[0].Type.ShouldBe(ActivityType.Meeting);
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyPage()
    {
        // Arrange
        _activityRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ActivitiesFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmActivity>());

        _activityRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ActivitiesCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetActivitiesQuery(ContactId: Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }
}
