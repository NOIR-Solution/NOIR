namespace NOIR.Application.Features.Blog.Commands.UpdateTag;

/// <summary>
/// Validator for UpdateTagCommand.
/// </summary>
public sealed class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
{
    private const int MaxNameLength = 100;
    private const int MaxSlugLength = 100;
    private const int MaxDescriptionLength = 500;
    private const int MaxColorLength = 20;

    public UpdateTagCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Tag ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithMessage($"Name cannot exceed {MaxNameLength} characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(MaxSlugLength).WithMessage($"Slug cannot exceed {MaxSlugLength} characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(MaxDescriptionLength).WithMessage($"Description cannot exceed {MaxDescriptionLength} characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Color)
            .MaximumLength(MaxColorLength).WithMessage($"Color cannot exceed {MaxColorLength} characters.")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color code (e.g., #3B82F6).")
            .When(x => x.Color is not null);
    }
}
