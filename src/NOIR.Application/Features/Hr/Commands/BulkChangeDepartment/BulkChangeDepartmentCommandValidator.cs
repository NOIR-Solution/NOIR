namespace NOIR.Application.Features.Hr.Commands.BulkChangeDepartment;

public class BulkChangeDepartmentCommandValidator : AbstractValidator<BulkChangeDepartmentCommand>
{
    public BulkChangeDepartmentCommandValidator()
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

        RuleFor(x => x.NewDepartmentId)
            .NotEmpty()
            .WithMessage("New department ID is required.");
    }
}
