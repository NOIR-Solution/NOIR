namespace NOIR.Application.Features.Reviews.Commands.VoteReview;

/// <summary>
/// Validator for VoteReviewCommand.
/// </summary>
public sealed class VoteReviewCommandValidator : AbstractValidator<VoteReviewCommand>
{
    public VoteReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty().WithMessage("Review ID is required.");
    }
}
