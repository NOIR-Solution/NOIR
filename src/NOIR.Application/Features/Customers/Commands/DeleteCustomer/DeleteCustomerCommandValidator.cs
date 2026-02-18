namespace NOIR.Application.Features.Customers.Commands.DeleteCustomer;

/// <summary>
/// Validator for DeleteCustomerCommand.
/// </summary>
public sealed class DeleteCustomerCommandValidator : AbstractValidator<DeleteCustomerCommand>
{
    public DeleteCustomerCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Customer ID is required.");
    }
}
