namespace NOIR.Application.Features.Reviews.Commands.BulkApproveReviews;

/// <summary>
/// Validator for BulkApproveReviewsCommand.
/// </summary>
public sealed class BulkApproveReviewsCommandValidator : AbstractValidator<BulkApproveReviewsCommand>
{
    public BulkApproveReviewsCommandValidator()
    {
        RuleFor(x => x.ReviewIds)
            .NotEmpty().WithMessage("At least one review ID is required.");

        RuleForEach(x => x.ReviewIds)
            .NotEmpty().WithMessage("Review ID cannot be empty.");
    }
}
