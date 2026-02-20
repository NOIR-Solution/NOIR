namespace NOIR.Application.Features.Payments.Commands.RefreshPaymentStatus;

/// <summary>
/// Validator for RefreshPaymentStatusCommand.
/// </summary>
public sealed class RefreshPaymentStatusCommandValidator : AbstractValidator<RefreshPaymentStatusCommand>
{
    public RefreshPaymentStatusCommandValidator()
    {
        RuleFor(x => x.PaymentTransactionId)
            .NotEmpty().WithMessage("Payment transaction ID is required.");
    }
}
