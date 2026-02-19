using NOIR.Application.Features.Orders.Commands.AddOrderNote;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.AddOrderNote;

/// <summary>
/// Unit tests for AddOrderNoteCommandValidator.
/// </summary>
public class AddOrderNoteCommandValidatorTests
{
    private readonly AddOrderNoteCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new AddOrderNoteCommand(Guid.NewGuid(), "This is a valid note");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyOrderId_ShouldFail()
    {
        var command = new AddOrderNoteCommand(Guid.Empty, "Valid content");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void Validate_WithEmptyContent_ShouldFail()
    {
        var command = new AddOrderNoteCommand(Guid.NewGuid(), "");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WithNullContent_ShouldFail()
    {
        var command = new AddOrderNoteCommand(Guid.NewGuid(), null!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WithContentExceeding2000Chars_ShouldFail()
    {
        var longContent = new string('x', 2001);
        var command = new AddOrderNoteCommand(Guid.NewGuid(), longContent);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Note content cannot exceed 2000 characters.");
    }

    [Fact]
    public void Validate_WithContentExactly2000Chars_ShouldPass()
    {
        var content = new string('x', 2000);
        var command = new AddOrderNoteCommand(Guid.NewGuid(), content);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }
}
