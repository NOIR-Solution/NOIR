using NOIR.Application.Features.Crm.Commands.CreateLead;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class CreateLeadCommandHandlerTests
{
    private readonly Mock<IRepository<Lead, Guid>> _leadRepoMock;
    private readonly Mock<IRepository<CrmContact, Guid>> _contactRepoMock;
    private readonly Mock<IRepository<Pipeline, Guid>> _pipelineRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateLeadCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public CreateLeadCommandHandlerTests()
    {
        _leadRepoMock = new Mock<IRepository<Lead, Guid>>();
        _contactRepoMock = new Mock<IRepository<CrmContact, Guid>>();
        _pipelineRepoMock = new Mock<IRepository<Pipeline, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _leadRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lead l, CancellationToken _) => l);

        _handler = new CreateLeadCommandHandler(
            _leadRepoMock.Object,
            _contactRepoMock.Object,
            _pipelineRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private CrmContact CreateTestContact() =>
        CrmContact.Create("John", "Doe", "john@example.com", ContactSource.Web, TestTenantId);

    private Pipeline CreateTestPipelineWithStages()
    {
        var pipeline = Pipeline.Create("Sales", TestTenantId, isDefault: true);
        // We need to add stages. Since the Pipeline.Stages is a collection with private set,
        // we create it through reflection or by creating stages and adding them.
        // The handler reads pipeline.Stages.OrderBy(s => s.SortOrder).FirstOrDefault()
        // For the test, we mock the pipeline spec to return a pipeline with stages.
        return pipeline;
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateLead_InFirstStage()
    {
        // Arrange
        var contact = CreateTestContact();
        var pipeline = Pipeline.Create("Sales", TestTenantId, isDefault: true);
        var stage = PipelineStage.Create(pipeline.Id, "New", 0, TestTenantId, "#6B7280");

        // Add stage to pipeline's Stages collection
        pipeline.Stages.Add(stage);

        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdReadOnlySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ActiveLeadsByStageSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new CreateLeadCommand(
            "Enterprise Deal", contact.Id, pipeline.Id,
            Value: 50000m, Currency: "USD");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Title.ShouldBe("Enterprise Deal");
        result.Value.StageId.ShouldBe(stage.Id);

        _leadRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ContactNotFound_ShouldReturnError()
    {
        // Arrange
        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmContact?)null);

        var command = new CreateLeadCommand(
            "Deal", Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _leadRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PipelineNotFound_ShouldReturnError()
    {
        // Arrange
        var contact = CreateTestContact();

        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdReadOnlySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pipeline?)null);

        var command = new CreateLeadCommand(
            "Deal", contact.Id, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        _leadRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PipelineNoStages_ShouldReturnError()
    {
        // Arrange
        var contact = CreateTestContact();
        var pipeline = Pipeline.Create("Empty Pipeline", TestTenantId);
        // Pipeline has no stages

        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdReadOnlySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        var command = new CreateLeadCommand(
            "Deal", contact.Id, pipeline.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }
}
