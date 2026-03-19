namespace NOIR.Application.Features.Crm.Commands.ReorderStages;

/// <summary>
/// Reorders the active stages of a pipeline. System stages (Won/Lost) are always kept at the end.
/// </summary>
public sealed record ReorderStagesCommand(
    Guid PipelineId,
    List<Guid> StageIds) : IAuditableCommand<List<Features.Crm.DTOs.PipelineStageDto>>
{
    public string? AuditUserId { get; init; }
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => PipelineId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Reordered pipeline stages";
}
