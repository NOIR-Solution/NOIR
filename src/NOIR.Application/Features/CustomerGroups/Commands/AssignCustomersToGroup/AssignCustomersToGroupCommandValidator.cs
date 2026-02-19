namespace NOIR.Application.Features.CustomerGroups.Commands.AssignCustomersToGroup;

/// <summary>
/// Validator for AssignCustomersToGroupCommand.
/// </summary>
public class AssignCustomersToGroupCommandValidator : AbstractValidator<AssignCustomersToGroupCommand>
{
    public AssignCustomersToGroupCommandValidator()
    {
        RuleFor(x => x.CustomerGroupId)
            .NotEmpty().WithMessage("Customer group ID is required.");

        RuleFor(x => x.CustomerIds)
            .NotEmpty().WithMessage("At least one customer ID is required.")
            .Must(ids => ids.Count <= 100).WithMessage("Cannot assign more than 100 customers at once.");
    }
}
