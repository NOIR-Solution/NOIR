namespace NOIR.Application.Features.Payments.Commands.RejectRefund;

/// <summary>
/// Validator for RejectRefundCommand.
/// </summary>
public sealed class RejectRefundCommandValidator : AbstractValidator<RejectRefundCommand>
{
    private const int MaxReasonLength = 1000;

    public RejectRefundCommandValidator()
    {
        RuleFor(x => x.RefundId)
            .NotEmpty().WithMessage("Refund ID is required.");

        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(MaxReasonLength).WithMessage($"Rejection reason cannot exceed {MaxReasonLength} characters.");
    }
}
