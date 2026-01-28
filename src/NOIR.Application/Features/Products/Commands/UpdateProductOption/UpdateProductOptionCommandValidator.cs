namespace NOIR.Application.Features.Products.Commands.UpdateProductOption;

/// <summary>
/// Validator for UpdateProductOptionCommand.
/// </summary>
public sealed class UpdateProductOptionCommandValidator : AbstractValidator<UpdateProductOptionCommand>
{
    private const int MaxNameLength = 50;
    private const int MaxDisplayNameLength = 100;

    public UpdateProductOptionCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.OptionId)
            .NotEmpty().WithMessage("Option ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Option name is required.")
            .MaximumLength(MaxNameLength).WithMessage($"Option name cannot exceed {MaxNameLength} characters.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(MaxDisplayNameLength).WithMessage($"Display name cannot exceed {MaxDisplayNameLength} characters.")
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");
    }
}
