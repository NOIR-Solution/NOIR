namespace NOIR.Application.Features.Hr.Commands.AssignTagsToEmployee;

public sealed class AssignTagsToEmployeeCommandValidator : AbstractValidator<AssignTagsToEmployeeCommand>
{
    public AssignTagsToEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.TagIds)
            .NotEmpty().WithMessage("At least one tag ID is required.")
            .Must(ids => ids.Count <= 50).WithMessage("Cannot assign more than 50 tags at once.");
    }
}
