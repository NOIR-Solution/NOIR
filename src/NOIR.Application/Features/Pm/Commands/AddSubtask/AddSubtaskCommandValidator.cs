namespace NOIR.Application.Features.Pm.Commands.AddSubtask;

public sealed class AddSubtaskCommandValidator : AbstractValidator<AddSubtaskCommand>
{
    public AddSubtaskCommandValidator()
    {
        RuleFor(x => x.ParentTaskId)
            .NotEmpty().WithMessage("Parent task ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
    }
}
