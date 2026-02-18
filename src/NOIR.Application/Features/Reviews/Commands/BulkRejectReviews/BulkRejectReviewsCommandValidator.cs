namespace NOIR.Application.Features.Reviews.Commands.BulkRejectReviews;

/// <summary>
/// Validator for BulkRejectReviewsCommand.
/// </summary>
public sealed class BulkRejectReviewsCommandValidator : AbstractValidator<BulkRejectReviewsCommand>
{
    public BulkRejectReviewsCommandValidator()
    {
        RuleFor(x => x.ReviewIds)
            .NotEmpty().WithMessage("At least one review ID is required.");

        RuleForEach(x => x.ReviewIds)
            .NotEmpty().WithMessage("Review ID cannot be empty.");
    }
}
