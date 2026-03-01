using NOIR.Application.Features.Pm.Commands.CreateTaskLabel;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class CreateTaskLabelCommandValidatorTests
{
    private readonly CreateTaskLabelCommandValidator _validator;

    public CreateTaskLabelCommandValidatorTests()
    {
        _validator = new CreateTaskLabelCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new CreateTaskLabelCommand(Guid.NewGuid(), "Bug", "#EF4444");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = new CreateTaskLabelCommand(Guid.Empty, "Bug", "#EF4444");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        // Arrange
        var command = new CreateTaskLabelCommand(Guid.NewGuid(), "", "#EF4444");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new CreateTaskLabelCommand(Guid.NewGuid(), new string('A', 51), "#EF4444");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_EmptyColor_ShouldFail()
    {
        // Arrange
        var command = new CreateTaskLabelCommand(Guid.NewGuid(), "Bug", "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }

    [Fact]
    public void Validate_ColorExceedsMaxLength_ShouldFail()
    {
        // Arrange
        var command = new CreateTaskLabelCommand(Guid.NewGuid(), "Bug", new string('A', 21));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Color);
    }
}
