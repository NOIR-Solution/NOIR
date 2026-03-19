namespace NOIR.Application.Features.Crm.Commands.UpdateStage;

/// <summary>
/// Updates a pipeline stage. System stages (Won/Lost): only color can change.
/// </summary>
public sealed record UpdateStageCommand(
    Guid StageId,
    string Name,
    string Color) : IAuditableCommand<Features.Crm.DTOs.PipelineStageDto>
{
    public string? AuditUserId { get; init; }
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => StageId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => "Updated pipeline stage";
}
