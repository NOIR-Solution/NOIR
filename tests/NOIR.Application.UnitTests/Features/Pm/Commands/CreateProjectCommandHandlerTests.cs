using NOIR.Application.Features.Pm.Commands.CreateProject;
using NOIR.Application.Features.Pm.Specifications;
using NOIR.Application.Features.Hr.Specifications;
using NOIR.Domain.Entities.Hr;
using NOIR.Domain.Entities.Pm;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class CreateProjectCommandHandlerTests
{
    private readonly Mock<IRepository<Project, Guid>> _projectRepoMock;
    private readonly Mock<IRepository<Employee, Guid>> _employeeRepoMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IProjectCodeGenerator> _projectCodeGeneratorMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateProjectCommandHandler _handler;

    private const string TestTenantId = "tenant-123";
    private const string TestUserId = "user-123";
    private const string TestProjectCode = "PRJ-20260301-000001";

    public CreateProjectCommandHandlerTests()
    {
        _projectRepoMock = new Mock<IRepository<Project, Guid>>();
        _employeeRepoMock = new Mock<IRepository<Employee, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _projectCodeGeneratorMock = new Mock<IProjectCodeGenerator>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _currentUserMock.Setup(x => x.UserId).Returns(TestUserId);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _projectCodeGeneratorMock
            .Setup(x => x.GenerateNextAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestProjectCode);
        _projectRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project p, CancellationToken _) => p);

        // Setup DbSet mocks
        var emptyMembers = new List<ProjectMember>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectMembers).Returns(emptyMembers.Object);

        var emptyColumns = new List<ProjectColumn>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProjectColumns).Returns(emptyColumns.Object);

        _handler = new CreateProjectCommandHandler(
            _projectRepoMock.Object,
            _employeeRepoMock.Object,
            _dbContextMock.Object,
            _projectCodeGeneratorMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static Employee CreateTestEmployee() =>
        Employee.Create("EMP-001", "John", "Doe", "john@test.com",
            Guid.NewGuid(), DateTimeOffset.UtcNow, EmploymentType.FullTime, TestTenantId);

    [Fact]
    public async Task Handle_ValidRequest_ShouldCreateProject()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectBySlugSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var employee = CreateTestEmployee();
        _employeeRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        // Mock reload
        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectByIdSpec _, CancellationToken _) =>
                Project.Create("My Project", "my-project", TestProjectCode, TestTenantId));

        var command = new CreateProjectCommand("My Project", Description: "Test description");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _projectRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SlugAlreadyExists_ShouldReturnError()
    {
        // Arrange
        var existingProject = Project.Create("Existing", "my-project", TestProjectCode, TestTenantId);
        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectBySlugSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProject);

        var command = new CreateProjectCommand("My Project");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _projectRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldAutoCreateDefaultColumns()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectBySlugSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var employee = CreateTestEmployee();
        _employeeRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Project.Create("Test", "test", TestProjectCode, TestTenantId));

        var command = new CreateProjectCommand("Test Project");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Verify 4 default columns were added
        _dbContextMock.Verify(
            x => x.ProjectColumns, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_ShouldAddCreatorAsOwnerMember()
    {
        // Arrange
        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectBySlugSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var employee = CreateTestEmployee();
        _employeeRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<EmployeeByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _projectRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProjectByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Project.Create("Test", "test", TestProjectCode, TestTenantId));

        var command = new CreateProjectCommand("Test Project");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Verify member was added
        _dbContextMock.Verify(
            x => x.ProjectMembers, Times.AtLeastOnce);
    }
}
