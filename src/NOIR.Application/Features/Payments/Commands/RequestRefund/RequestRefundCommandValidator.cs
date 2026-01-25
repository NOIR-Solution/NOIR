namespace NOIR.Application.Features.Payments.Commands.RequestRefund;

/// <summary>
/// Validator for RequestRefundCommand.
/// </summary>
public sealed class RequestRefundCommandValidator : AbstractValidator<RequestRefundCommand>
{
    private const int MaxNotesLength = 1000;

    public RequestRefundCommandValidator()
    {
        RuleFor(x => x.PaymentTransactionId)
            .NotEmpty().WithMessage("Payment transaction ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Refund amount must be greater than zero.");

        RuleFor(x => x.Reason)
            .IsInEnum().WithMessage("Invalid refund reason.");

        RuleFor(x => x.Notes)
            .MaximumLength(MaxNotesLength).WithMessage($"Notes cannot exceed {MaxNotesLength} characters.")
            .When(x => x.Notes is not null);
    }
}
