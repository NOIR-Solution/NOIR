namespace NOIR.Application.Features.Pm.Commands.ReorderTask;

public sealed class ReorderTaskCommandValidator : AbstractValidator<ReorderTaskCommand>
{
    public ReorderTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty().WithMessage("Task ID is required.");
    }
}
