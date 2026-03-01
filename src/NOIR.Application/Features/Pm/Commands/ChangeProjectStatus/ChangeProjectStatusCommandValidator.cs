namespace NOIR.Application.Features.Pm.Commands.ChangeProjectStatus;

public sealed class ChangeProjectStatusCommandValidator : AbstractValidator<ChangeProjectStatusCommand>
{
    public ChangeProjectStatusCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid project status.");
    }
}
