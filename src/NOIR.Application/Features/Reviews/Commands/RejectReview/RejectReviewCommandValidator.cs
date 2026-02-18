namespace NOIR.Application.Features.Reviews.Commands.RejectReview;

/// <summary>
/// Validator for RejectReviewCommand.
/// </summary>
public sealed class RejectReviewCommandValidator : AbstractValidator<RejectReviewCommand>
{
    public RejectReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty().WithMessage("Review ID is required.");
    }
}
