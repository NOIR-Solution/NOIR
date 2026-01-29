namespace NOIR.Application.Features.Brands.Commands.UpdateBrand;

/// <summary>
/// Validator for UpdateBrandCommand.
/// </summary>
public class UpdateBrandCommandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Brand ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Brand name is required.")
            .MaximumLength(200)
            .WithMessage("Brand name cannot exceed 200 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Brand slug is required.")
            .MaximumLength(200)
            .WithMessage("Brand slug cannot exceed 200 characters.")
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must contain only lowercase letters, numbers, and hyphens.");

        RuleFor(x => x.Description)
            .MaximumLength(5000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description cannot exceed 5000 characters.");

        RuleFor(x => x.Website)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("Website URL cannot exceed 500 characters.");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.LogoUrl))
            .WithMessage("Logo URL cannot exceed 500 characters.");

        RuleFor(x => x.BannerUrl)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.BannerUrl))
            .WithMessage("Banner URL cannot exceed 500 characters.");

        RuleFor(x => x.MetaTitle)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.MetaTitle))
            .WithMessage("Meta title cannot exceed 200 characters.");

        RuleFor(x => x.MetaDescription)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.MetaDescription))
            .WithMessage("Meta description cannot exceed 500 characters.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Sort order must be zero or greater.");
    }
}
