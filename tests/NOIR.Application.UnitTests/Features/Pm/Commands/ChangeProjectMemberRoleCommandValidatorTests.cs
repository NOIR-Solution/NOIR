using NOIR.Application.Features.Pm.Commands.ChangeProjectMemberRole;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class ChangeProjectMemberRoleCommandValidatorTests
{
    private readonly ChangeProjectMemberRoleCommandValidator _validator;

    public ChangeProjectMemberRoleCommandValidatorTests()
    {
        _validator = new ChangeProjectMemberRoleCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new ChangeProjectMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), ProjectMemberRole.Manager);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = new ChangeProjectMemberRoleCommand(Guid.Empty, Guid.NewGuid(), ProjectMemberRole.Manager);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_EmptyMemberId_ShouldFail()
    {
        // Arrange
        var command = new ChangeProjectMemberRoleCommand(Guid.NewGuid(), Guid.Empty, ProjectMemberRole.Manager);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MemberId);
    }

    [Fact]
    public void Validate_InvalidRole_ShouldFail()
    {
        // Arrange
        var command = new ChangeProjectMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), (ProjectMemberRole)999);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}
