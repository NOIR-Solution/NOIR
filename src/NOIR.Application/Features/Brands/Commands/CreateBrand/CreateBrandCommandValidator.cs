namespace NOIR.Application.Features.Brands.Commands.CreateBrand;

/// <summary>
/// Validator for CreateBrandCommand.
/// </summary>
public class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Brand name is required.")
            .MaximumLength(200).WithMessage("Brand name must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Brand slug is required.")
            .MaximumLength(200).WithMessage("Brand slug must not exceed 200 characters.")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("Logo URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.LogoUrl));

        RuleFor(x => x.BannerUrl)
            .MaximumLength(500).WithMessage("Banner URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.BannerUrl));

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Website)
            .MaximumLength(500).WithMessage("Website URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200).WithMessage("Meta title must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.MetaTitle));

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500).WithMessage("Meta description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.MetaDescription));
    }
}
