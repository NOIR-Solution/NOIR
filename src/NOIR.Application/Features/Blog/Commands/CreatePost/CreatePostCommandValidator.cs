namespace NOIR.Application.Features.Blog.Commands.CreatePost;

/// <summary>
/// Validator for CreatePostCommand.
/// </summary>
public sealed class CreatePostCommandValidator : AbstractValidator<CreatePostCommand>
{
    private const int MaxTitleLength = 500;
    private const int MaxSlugLength = 500;
    private const int MaxExcerptLength = 1000;
    private const int MaxMetaTitleLength = 200;
    private const int MaxMetaDescriptionLength = 500;
    private const int MaxUrlLength = 2000;
    private const int MaxAltTextLength = 500;

    public CreatePostCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(MaxTitleLength).WithMessage($"Title cannot exceed {MaxTitleLength} characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(MaxSlugLength).WithMessage($"Slug cannot exceed {MaxSlugLength} characters.")
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");

        RuleFor(x => x.Excerpt)
            .MaximumLength(MaxExcerptLength).WithMessage($"Excerpt cannot exceed {MaxExcerptLength} characters.")
            .When(x => x.Excerpt is not null);

        RuleFor(x => x.MetaTitle)
            .MaximumLength(MaxMetaTitleLength).WithMessage($"Meta title cannot exceed {MaxMetaTitleLength} characters.")
            .When(x => x.MetaTitle is not null);

        RuleFor(x => x.MetaDescription)
            .MaximumLength(MaxMetaDescriptionLength).WithMessage($"Meta description cannot exceed {MaxMetaDescriptionLength} characters.")
            .When(x => x.MetaDescription is not null);

        RuleFor(x => x.FeaturedImageUrl)
            .MaximumLength(MaxUrlLength).WithMessage($"Featured image URL cannot exceed {MaxUrlLength} characters.")
            .When(x => x.FeaturedImageUrl is not null);

        RuleFor(x => x.FeaturedImageAlt)
            .MaximumLength(MaxAltTextLength).WithMessage($"Featured image alt text cannot exceed {MaxAltTextLength} characters.")
            .When(x => x.FeaturedImageAlt is not null);

        RuleFor(x => x.CanonicalUrl)
            .MaximumLength(MaxUrlLength).WithMessage($"Canonical URL cannot exceed {MaxUrlLength} characters.")
            .When(x => x.CanonicalUrl is not null);
    }
}
