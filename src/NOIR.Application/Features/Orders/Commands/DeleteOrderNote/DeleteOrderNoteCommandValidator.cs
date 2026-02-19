namespace NOIR.Application.Features.Orders.Commands.DeleteOrderNote;

/// <summary>
/// Validator for DeleteOrderNoteCommand.
/// </summary>
public sealed class DeleteOrderNoteCommandValidator : AbstractValidator<DeleteOrderNoteCommand>
{
    public DeleteOrderNoteCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");

        RuleFor(x => x.NoteId)
            .NotEmpty().WithMessage("Note ID is required.");
    }
}
