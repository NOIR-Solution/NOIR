namespace NOIR.Application.Features.Payments.Commands.CancelPayment;

/// <summary>
/// Validator for CancelPaymentCommand.
/// </summary>
public sealed class CancelPaymentCommandValidator : AbstractValidator<CancelPaymentCommand>
{
    private const int MaxReasonLength = 500;

    public CancelPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentTransactionId)
            .NotEmpty().WithMessage("Payment transaction ID is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(MaxReasonLength).WithMessage($"Reason cannot exceed {MaxReasonLength} characters.")
            .When(x => x.Reason is not null);
    }
}
