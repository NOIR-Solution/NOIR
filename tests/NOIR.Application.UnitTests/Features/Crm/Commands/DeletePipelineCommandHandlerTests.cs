using NOIR.Application.Features.Crm.Commands.DeletePipeline;
using NOIR.Application.Features.Crm.Specifications;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Commands;

public class DeletePipelineCommandHandlerTests
{
    private readonly Mock<IRepository<Pipeline, Guid>> _pipelineRepoMock;
    private readonly Mock<IRepository<Lead, Guid>> _leadRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly DeletePipelineCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public DeletePipelineCommandHandlerTests()
    {
        _pipelineRepoMock = new Mock<IRepository<Pipeline, Guid>>();
        _leadRepoMock = new Mock<IRepository<Lead, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new DeletePipelineCommandHandler(
            _pipelineRepoMock.Object,
            _leadRepoMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    [Fact]
    public async Task Handle_DefaultPipeline_ShouldReturnError()
    {
        // Arrange
        var pipeline = Pipeline.Create("Sales Pipeline", TestTenantId, isDefault: true);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        var command = new DeletePipelineCommand(pipeline.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _pipelineRepoMock.Verify(x => x.Remove(It.IsAny<Pipeline>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HasActiveLeads_ShouldReturnError()
    {
        // Arrange
        var pipeline = Pipeline.Create("Custom Pipeline", TestTenantId, isDefault: false);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ActiveLeadsByPipelineSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var command = new DeletePipelineCommand(pipeline.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _pipelineRepoMock.Verify(x => x.Remove(It.IsAny<Pipeline>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoActiveLeads_ShouldDelete()
    {
        // Arrange
        var pipeline = Pipeline.Create("Custom Pipeline", TestTenantId, isDefault: false);

        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        _leadRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ActiveLeadsByPipelineSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new DeletePipelineCommand(pipeline.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _pipelineRepoMock.Verify(x => x.Remove(pipeline), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PipelineNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _pipelineRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PipelineByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pipeline?)null);

        var command = new DeletePipelineCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}
