using NOIR.Application.Features.Hr.Commands.UpdateTag;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.UpdateTag;

public class UpdateTagCommandHandlerTests
{
    private readonly Mock<IRepository<EmployeeTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateTagCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public UpdateTagCommandHandlerTests()
    {
        _tagRepositoryMock = new Mock<IRepository<EmployeeTag, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new UpdateTagCommandHandler(
            _tagRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static UpdateTagCommand CreateValidCommand(Guid? id = null) =>
        new(
            Id: id ?? Guid.NewGuid(),
            Name: "Updated Tag",
            Category: EmployeeTagCategory.Skill,
            Color: "#3b82f6",
            Description: "Updated description",
            SortOrder: 1);

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateTag()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var command = CreateValidCommand(id: tagId);
        var existingTag = EmployeeTag.Create("Original", EmployeeTagCategory.Team, TestTenantId);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeTagByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeTagByNameAndCategorySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeTag?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateValidCommand();

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeTagByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmployeeTag?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithDuplicateNameAndCategory_ShouldReturnConflict()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var command = CreateValidCommand(id: tagId);
        var existingTag = EmployeeTag.Create("Original", EmployeeTagCategory.Team, TestTenantId);
        var duplicateTag = EmployeeTag.Create("Updated Tag", EmployeeTagCategory.Skill, TestTenantId);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeTagByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeTagByNameAndCategorySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicateTag);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
