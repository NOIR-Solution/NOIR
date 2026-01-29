namespace NOIR.Application.Features.ProductAttributes.Commands.UpdateProductAttribute;

/// <summary>
/// Validator for UpdateProductAttributeCommand.
/// </summary>
public class UpdateProductAttributeCommandValidator : AbstractValidator<UpdateProductAttributeCommand>
{
    public UpdateProductAttributeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Attribute ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Attribute code is required.")
            .MaximumLength(100).WithMessage("Attribute code must not exceed 100 characters.")
            .Matches("^[a-z0-9_]+$").WithMessage("Code can only contain lowercase letters, numbers, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Attribute name is required.")
            .MaximumLength(200).WithMessage("Attribute name must not exceed 200 characters.");

        RuleFor(x => x.Unit)
            .MaximumLength(50).WithMessage("Unit must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.Unit));

        RuleFor(x => x.ValidationRegex)
            .MaximumLength(500).WithMessage("Validation regex must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.ValidationRegex));

        RuleFor(x => x.MinValue)
            .LessThanOrEqualTo(x => x.MaxValue)
            .When(x => x.MinValue.HasValue && x.MaxValue.HasValue)
            .WithMessage("Minimum value must be less than or equal to maximum value.");

        RuleFor(x => x.MaxLength)
            .GreaterThan(0).WithMessage("Maximum length must be greater than 0.")
            .When(x => x.MaxLength.HasValue);

        RuleFor(x => x.DefaultValue)
            .MaximumLength(500).WithMessage("Default value must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.DefaultValue));

        RuleFor(x => x.Placeholder)
            .MaximumLength(200).WithMessage("Placeholder must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Placeholder));

        RuleFor(x => x.HelpText)
            .MaximumLength(500).WithMessage("Help text must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.HelpText));

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be non-negative.");
    }
}
