namespace NOIR.Application.Features.Reviews.Commands.ApproveReview;

/// <summary>
/// Validator for ApproveReviewCommand.
/// </summary>
public sealed class ApproveReviewCommandValidator : AbstractValidator<ApproveReviewCommand>
{
    public ApproveReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty().WithMessage("Review ID is required.");
    }
}
