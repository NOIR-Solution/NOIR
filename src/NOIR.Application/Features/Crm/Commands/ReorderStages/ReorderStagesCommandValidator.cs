namespace NOIR.Application.Features.Crm.Commands.ReorderStages;

public class ReorderStagesCommandValidator : AbstractValidator<ReorderStagesCommand>
{
    public ReorderStagesCommandValidator()
    {
        RuleFor(x => x.PipelineId).NotEmpty();
        RuleFor(x => x.StageIds).NotNull().NotEmpty()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Stage IDs must be unique.");
    }
}
