namespace NOIR.Application.Features.Products.Commands.DuplicateProduct;

public class DuplicateProductCommandValidator : AbstractValidator<DuplicateProductCommand>
{
    public DuplicateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required.");
    }
}
