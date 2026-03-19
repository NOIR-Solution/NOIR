namespace NOIR.Application.Features.Crm.Commands.DeleteStage;

/// <summary>
/// Deletes a pipeline stage, migrating all its leads to another stage first.
/// System stages (Won/Lost) cannot be deleted.
/// </summary>
public sealed record DeleteStageCommand(
    Guid StageId,
    Guid MoveLeadsToStageId) : IAuditableCommand<Features.Crm.DTOs.PipelineStageDto>
{
    public string? AuditUserId { get; init; }
    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => StageId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Deleted pipeline stage";
}
