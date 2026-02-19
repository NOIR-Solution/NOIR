namespace NOIR.Application.Features.CustomerGroups.Commands.RemoveCustomersFromGroup;

/// <summary>
/// Validator for RemoveCustomersFromGroupCommand.
/// </summary>
public class RemoveCustomersFromGroupCommandValidator : AbstractValidator<RemoveCustomersFromGroupCommand>
{
    public RemoveCustomersFromGroupCommandValidator()
    {
        RuleFor(x => x.CustomerGroupId)
            .NotEmpty().WithMessage("Customer group ID is required.");

        RuleFor(x => x.CustomerIds)
            .NotEmpty().WithMessage("At least one customer ID is required.")
            .Must(ids => ids.Count <= 100).WithMessage("Cannot remove more than 100 customers at once.");
    }
}
