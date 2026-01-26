namespace NOIR.Application.Features.Products.Commands.ArchiveProduct;

/// <summary>
/// Validator for ArchiveProductCommand.
/// </summary>
public sealed class ArchiveProductCommandValidator : AbstractValidator<ArchiveProductCommand>
{
    public ArchiveProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product ID is required.");
    }
}
