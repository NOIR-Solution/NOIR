namespace NOIR.Application.Features.CustomerGroups.Commands.DeleteCustomerGroup;

/// <summary>
/// Validator for DeleteCustomerGroupCommand.
/// </summary>
public sealed class DeleteCustomerGroupCommandValidator : AbstractValidator<DeleteCustomerGroupCommand>
{
    public DeleteCustomerGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Customer group ID is required.");
    }
}
