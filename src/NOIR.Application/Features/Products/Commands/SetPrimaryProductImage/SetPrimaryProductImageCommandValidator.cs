namespace NOIR.Application.Features.Products.Commands.SetPrimaryProductImage;

/// <summary>
/// Validator for SetPrimaryProductImageCommand.
/// </summary>
public sealed class SetPrimaryProductImageCommandValidator : AbstractValidator<SetPrimaryProductImageCommand>
{
    public SetPrimaryProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.ImageId)
            .NotEmpty().WithMessage("Image ID is required.");
    }
}
