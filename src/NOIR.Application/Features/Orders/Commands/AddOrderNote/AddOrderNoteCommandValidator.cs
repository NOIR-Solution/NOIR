namespace NOIR.Application.Features.Orders.Commands.AddOrderNote;

/// <summary>
/// Validator for AddOrderNoteCommand.
/// </summary>
public sealed class AddOrderNoteCommandValidator : AbstractValidator<AddOrderNoteCommand>
{
    public AddOrderNoteCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Note content is required.")
            .MaximumLength(2000).WithMessage("Note content cannot exceed 2000 characters.");
    }
}
