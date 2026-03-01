using NOIR.Application.Features.Pm.Commands.UpdateTask;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class UpdateTaskCommandValidatorTests
{
    private readonly UpdateTaskCommandValidator _validator;

    public UpdateTaskCommandValidatorTests()
    {
        _validator = new UpdateTaskCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new UpdateTaskCommand(Guid.NewGuid(), Title: "Updated Title");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        // Arrange
        var command = new UpdateTaskCommand(Guid.Empty, Title: "Title");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_TitleExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new UpdateTaskCommand(Guid.NewGuid(), Title: new string('A', 501));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new UpdateTaskCommand(Guid.NewGuid(), Description: new string('A', 5001));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_NegativeEstimatedHours_ShouldFail()
    {
        // Arrange
        var command = new UpdateTaskCommand(Guid.NewGuid(), EstimatedHours: -1m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EstimatedHours);
    }

    [Fact]
    public void Validate_ZeroEstimatedHours_ShouldFail()
    {
        // Arrange
        var command = new UpdateTaskCommand(Guid.NewGuid(), EstimatedHours: 0m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EstimatedHours);
    }

    [Fact]
    public void Validate_NegativeActualHours_ShouldFail()
    {
        // Arrange
        var command = new UpdateTaskCommand(Guid.NewGuid(), ActualHours: -1m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ActualHours);
    }

    [Fact]
    public void Validate_ZeroActualHours_ShouldPass()
    {
        // Arrange
        var command = new UpdateTaskCommand(Guid.NewGuid(), ActualHours: 0m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ActualHours);
    }

    [Fact]
    public void Validate_NullOptionalFields_ShouldPass()
    {
        // Arrange
        var command = new UpdateTaskCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
