namespace NOIR.Application.Features.Payments.Commands.ConfirmCodCollection;

/// <summary>
/// Validator for ConfirmCodCollectionCommand.
/// </summary>
public sealed class ConfirmCodCollectionCommandValidator : AbstractValidator<ConfirmCodCollectionCommand>
{
    private const int MaxNotesLength = 500;

    public ConfirmCodCollectionCommandValidator()
    {
        RuleFor(x => x.PaymentTransactionId)
            .NotEmpty().WithMessage("Payment transaction ID is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(MaxNotesLength).WithMessage($"Notes cannot exceed {MaxNotesLength} characters.")
            .When(x => x.Notes is not null);
    }
}
