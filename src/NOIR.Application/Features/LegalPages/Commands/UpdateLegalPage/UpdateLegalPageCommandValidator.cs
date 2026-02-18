namespace NOIR.Application.Features.LegalPages.Commands.UpdateLegalPage;

/// <summary>
/// Validator for UpdateLegalPageCommand.
/// </summary>
public sealed class UpdateLegalPageCommandValidator : AbstractValidator<UpdateLegalPageCommand>
{
    public UpdateLegalPageCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Page ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.HtmlContent)
            .NotEmpty().WithMessage("Content is required.");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(60).WithMessage("Meta title must not exceed 60 characters.");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(160).WithMessage("Meta description must not exceed 160 characters.");

        RuleFor(x => x.CanonicalUrl)
            .Must(url => string.IsNullOrEmpty(url) || (Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("Canonical URL must be a valid absolute URL.");
    }
}
