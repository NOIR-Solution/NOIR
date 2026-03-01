namespace NOIR.Application.Features.Hr.Commands.BulkAssignTags;

public class BulkAssignTagsCommandValidator : AbstractValidator<BulkAssignTagsCommand>
{
    public BulkAssignTagsCommandValidator()
    {
        RuleFor(x => x.EmployeeIds)
            .NotEmpty()
            .WithMessage("At least one employee ID is required.");

        RuleFor(x => x.EmployeeIds.Count)
            .LessThanOrEqualTo(100)
            .WithMessage("Maximum 100 employees per operation.");

        RuleForEach(x => x.EmployeeIds)
            .NotEmpty()
            .WithMessage("Employee ID cannot be empty.");

        RuleFor(x => x.TagIds)
            .NotEmpty()
            .WithMessage("At least one tag ID is required.");

        RuleFor(x => x.TagIds.Count)
            .LessThanOrEqualTo(50)
            .WithMessage("Maximum 50 tags per operation.");

        RuleForEach(x => x.TagIds)
            .NotEmpty()
            .WithMessage("Tag ID cannot be empty.");
    }
}
