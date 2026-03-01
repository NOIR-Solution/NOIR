using NOIR.Application.Features.Pm.Commands.DeleteColumn;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class DeleteColumnCommandValidatorTests
{
    private readonly DeleteColumnCommandValidator _validator;

    public DeleteColumnCommandValidatorTests()
    {
        _validator = new DeleteColumnCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new DeleteColumnCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = new DeleteColumnCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_EmptyColumnId_ShouldFail()
    {
        // Arrange
        var command = new DeleteColumnCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColumnId);
    }

    [Fact]
    public void Validate_EmptyMoveToColumnId_ShouldFail()
    {
        // Arrange
        var command = new DeleteColumnCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MoveToColumnId);
    }
}
