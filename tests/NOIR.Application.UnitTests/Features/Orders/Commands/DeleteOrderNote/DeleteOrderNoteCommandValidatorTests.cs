using NOIR.Application.Features.Orders.Commands.DeleteOrderNote;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.DeleteOrderNote;

/// <summary>
/// Unit tests for DeleteOrderNoteCommandValidator.
/// </summary>
public class DeleteOrderNoteCommandValidatorTests
{
    private readonly DeleteOrderNoteCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new DeleteOrderNoteCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyOrderId_ShouldFail()
    {
        var command = new DeleteOrderNoteCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void Validate_WithEmptyNoteId_ShouldFail()
    {
        var command = new DeleteOrderNoteCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NoteId);
    }
}
