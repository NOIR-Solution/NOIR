namespace NOIR.Application.Features.FilterAnalytics.Commands.CreateFilterEvent;

/// <summary>
/// Validator for CreateFilterEventCommand.
/// </summary>
public class CreateFilterEventCommandValidator : AbstractValidator<CreateFilterEventCommand>
{
    public CreateFilterEventCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required.")
            .MaximumLength(100)
            .WithMessage("Session ID must not exceed 100 characters.");

        RuleFor(x => x.EventType)
            .IsInEnum()
            .WithMessage("Invalid event type.");

        RuleFor(x => x.ProductCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Product count must be non-negative.");

        RuleFor(x => x.CategorySlug)
            .MaximumLength(200)
            .When(x => x.CategorySlug != null)
            .WithMessage("Category slug must not exceed 200 characters.");

        RuleFor(x => x.FilterCode)
            .MaximumLength(100)
            .When(x => x.FilterCode != null)
            .WithMessage("Filter code must not exceed 100 characters.");

        RuleFor(x => x.FilterValue)
            .MaximumLength(500)
            .When(x => x.FilterValue != null)
            .WithMessage("Filter value must not exceed 500 characters.");

        RuleFor(x => x.SearchQuery)
            .MaximumLength(500)
            .When(x => x.SearchQuery != null)
            .WithMessage("Search query must not exceed 500 characters.");
    }
}
