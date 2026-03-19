namespace NOIR.Application.Features.Crm.Commands.DeleteStage;

public class DeleteStageCommandValidator : AbstractValidator<DeleteStageCommand>
{
    public DeleteStageCommandValidator()
    {
        RuleFor(x => x.StageId).NotEmpty();
        RuleFor(x => x.MoveLeadsToStageId).NotEmpty();
    }
}
