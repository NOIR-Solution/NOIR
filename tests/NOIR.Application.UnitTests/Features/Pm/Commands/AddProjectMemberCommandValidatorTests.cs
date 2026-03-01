using NOIR.Application.Features.Pm.Commands.AddProjectMember;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class AddProjectMemberCommandValidatorTests
{
    private readonly AddProjectMemberCommandValidator _validator;

    public AddProjectMemberCommandValidatorTests()
    {
        _validator = new AddProjectMemberCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), ProjectMemberRole.Member);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = new AddProjectMemberCommand(Guid.Empty, Guid.NewGuid(), ProjectMemberRole.Member);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_EmptyEmployeeId_ShouldFail()
    {
        // Arrange
        var command = new AddProjectMemberCommand(Guid.NewGuid(), Guid.Empty, ProjectMemberRole.Member);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmployeeId);
    }

    [Fact]
    public void Validate_InvalidRole_ShouldFail()
    {
        // Arrange
        var command = new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), (ProjectMemberRole)999);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}
