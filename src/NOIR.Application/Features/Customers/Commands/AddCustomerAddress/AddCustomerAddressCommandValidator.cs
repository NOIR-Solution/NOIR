namespace NOIR.Application.Features.Customers.Commands.AddCustomerAddress;

/// <summary>
/// Validator for AddCustomerAddressCommand.
/// </summary>
public sealed class AddCustomerAddressCommandValidator : AbstractValidator<AddCustomerAddressCommand>
{
    public AddCustomerAddressCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.AddressType)
            .IsInEnum().WithMessage("Invalid address type.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address line 1 is required.")
            .MaximumLength(200).WithMessage("Address line 1 cannot exceed 200 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(200).WithMessage("Address line 2 cannot exceed 200 characters.");

        RuleFor(x => x.Ward)
            .MaximumLength(100).WithMessage("Ward cannot exceed 100 characters.");

        RuleFor(x => x.District)
            .MaximumLength(100).WithMessage("District cannot exceed 100 characters.");

        RuleFor(x => x.Province)
            .NotEmpty().WithMessage("Province is required.")
            .MaximumLength(100).WithMessage("Province cannot exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.");
    }
}
