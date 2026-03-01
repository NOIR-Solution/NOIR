namespace NOIR.Application.Features.Hr.Commands.RemoveTagsFromEmployee;

public sealed class RemoveTagsFromEmployeeCommandValidator : AbstractValidator<RemoveTagsFromEmployeeCommand>
{
    public RemoveTagsFromEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required.");

        RuleFor(x => x.TagIds)
            .NotEmpty().WithMessage("At least one tag ID is required.");
    }
}
