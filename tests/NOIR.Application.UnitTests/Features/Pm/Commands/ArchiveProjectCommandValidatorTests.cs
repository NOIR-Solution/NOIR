using NOIR.Application.Features.Pm.Commands.ArchiveProject;

namespace NOIR.Application.UnitTests.Features.Pm.Commands;

public class ArchiveProjectCommandValidatorTests
{
    private readonly ArchiveProjectCommandValidator _validator;

    public ArchiveProjectCommandValidatorTests()
    {
        _validator = new ArchiveProjectCommandValidator();
    }

    [Fact]
    public void Validate_ValidInput_ShouldPass()
    {
        // Arrange
        var command = new ArchiveProjectCommand(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        // Arrange
        var command = new ArchiveProjectCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}
