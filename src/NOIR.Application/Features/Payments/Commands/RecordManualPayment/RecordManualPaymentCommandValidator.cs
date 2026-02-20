namespace NOIR.Application.Features.Payments.Commands.RecordManualPayment;

/// <summary>
/// Validator for RecordManualPaymentCommand.
/// </summary>
public sealed class RecordManualPaymentCommandValidator : AbstractValidator<RecordManualPaymentCommand>
{
    public RecordManualPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .MaximumLength(3).WithMessage("Currency code cannot exceed 3 characters.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Invalid payment method.");

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(200).WithMessage("Reference number cannot exceed 200 characters.")
            .When(x => x.ReferenceNumber is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}
