namespace NOIR.Application.Features.Customers.Commands.DeleteCustomerAddress;

/// <summary>
/// Validator for DeleteCustomerAddressCommand.
/// </summary>
public sealed class DeleteCustomerAddressCommandValidator : AbstractValidator<DeleteCustomerAddressCommand>
{
    public DeleteCustomerAddressCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.AddressId)
            .NotEmpty().WithMessage("Address ID is required.");
    }
}
