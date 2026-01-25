namespace NOIR.Application.Features.Payments.Commands.ApproveRefund;

/// <summary>
/// Validator for ApproveRefundCommand.
/// </summary>
public sealed class ApproveRefundCommandValidator : AbstractValidator<ApproveRefundCommand>
{
    private const int MaxNotesLength = 500;

    public ApproveRefundCommandValidator()
    {
        RuleFor(x => x.RefundId)
            .NotEmpty().WithMessage("Refund ID is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(MaxNotesLength).WithMessage($"Notes cannot exceed {MaxNotesLength} characters.")
            .When(x => x.Notes is not null);
    }
}
