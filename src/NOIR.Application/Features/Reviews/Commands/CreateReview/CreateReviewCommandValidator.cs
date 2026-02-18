namespace NOIR.Application.Features.Reviews.Commands.CreateReview;

/// <summary>
/// Validator for CreateReviewCommand.
/// </summary>
public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    private const int MaxTitleLength = 200;
    private const int MinContentLength = 10;
    private const int MaxContentLength = 2000;
    private const int MaxMediaItems = 5;
    private const int MaxUrlLength = 500;

    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Title)
            .MaximumLength(MaxTitleLength).WithMessage($"Title cannot exceed {MaxTitleLength} characters.")
            .When(x => x.Title is not null);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Review content is required.")
            .MinimumLength(MinContentLength).WithMessage($"Review content must be at least {MinContentLength} characters.")
            .MaximumLength(MaxContentLength).WithMessage($"Review content cannot exceed {MaxContentLength} characters.");

        RuleFor(x => x.MediaUrls)
            .Must(urls => urls is null || urls.Count <= MaxMediaItems)
            .WithMessage($"Maximum {MaxMediaItems} media items allowed.");

        RuleForEach(x => x.MediaUrls)
            .NotEmpty().WithMessage("Media URL cannot be empty.")
            .MaximumLength(MaxUrlLength).WithMessage($"Media URL cannot exceed {MaxUrlLength} characters.")
            .When(x => x.MediaUrls is not null);
    }
}
