using NOIR.Application.Features.Pm.Commands.RemoveProjectMember;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class RemoveProjectMemberCommandValidatorTests
{
    private readonly RemoveProjectMemberCommandValidator _validator;

    public RemoveProjectMemberCommandValidatorTests()
    {
        _validator = new RemoveProjectMemberCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new RemoveProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = new RemoveProjectMemberCommand(Guid.Empty, Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_EmptyMemberId_ShouldFail()
    {
        // Arrange
        var command = new RemoveProjectMemberCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MemberId);
    }
}
