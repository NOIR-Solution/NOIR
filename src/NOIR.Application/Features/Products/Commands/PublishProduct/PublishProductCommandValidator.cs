namespace NOIR.Application.Features.Products.Commands.PublishProduct;

/// <summary>
/// Validator for PublishProductCommand.
/// </summary>
public sealed class PublishProductCommandValidator : AbstractValidator<PublishProductCommand>
{
    public PublishProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required.");
    }
}
