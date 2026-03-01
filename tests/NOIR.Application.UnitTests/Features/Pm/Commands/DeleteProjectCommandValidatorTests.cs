using NOIR.Application.Features.Pm.Commands.DeleteProject;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class DeleteProjectCommandValidatorTests
{
    private readonly DeleteProjectCommandValidator _validator;

    public DeleteProjectCommandValidatorTests()
    {
        _validator = new DeleteProjectCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new DeleteProjectCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProjectCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}
