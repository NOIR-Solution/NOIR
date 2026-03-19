namespace NOIR.Application.Features.Crm.Commands.CreateStage;

public class CreateStageCommandValidator : AbstractValidator<CreateStageCommand>
{
    public CreateStageCommandValidator()
    {
        RuleFor(x => x.PipelineId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(7)
            .Matches(@"^#[0-9a-fA-F]{6}$").WithMessage("Color must be a valid hex color (e.g. #6366f1).");
    }
}
