using NOIR.Application.Features.Hr.Commands.CreateTag;
using NOIR.Application.Features.Hr.DTOs;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;

namespace NOIR.Application.UnitTests.Features.Hr.Commands.CreateTag;

public class CreateTagCommandHandlerTests
{
    private readonly Mock<IRepository<EmployeeTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateTagCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CreateTagCommandHandlerTests()
    {
        _tagRepositoryMock = new Mock<IRepository<EmployeeTag, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new CreateTagCommandHandler(
            _tagRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static CreateTagCommand CreateValidCommand(
        string name = "Senior Developer",
        EmployeeTagCategory category = EmployeeTagCategory.Skill,
        string? color = "#ef4444",
        string? description = "Senior-level devs") =>
        new(Name: name, Category: category, Color: color, Description: description);

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateTag()
    {
        // Arrange
        var command = CreateValidCommand();

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
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Senior Developer");
        result.Value.Category.ShouldBe(EmployeeTagCategory.Skill);

        _tagRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<EmployeeTag>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateNameAndCategory_ShouldReturnConflict()
    {
        // Arrange
        var command = CreateValidCommand();
        var existingTag = EmployeeTag.Create("Senior Developer", EmployeeTagCategory.Skill, TestTenantId);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeTagByNameAndCategorySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);

        _tagRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<EmployeeTag>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
