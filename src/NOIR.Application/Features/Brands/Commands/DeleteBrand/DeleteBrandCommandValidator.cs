namespace NOIR.Application.Features.Brands.Commands.DeleteBrand;

/// <summary>
/// Validator for DeleteBrandCommand.
/// </summary>
public sealed class DeleteBrandCommandValidator : AbstractValidator<DeleteBrandCommand>
{
    public DeleteBrandCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Brand ID is required.");
    }
}
