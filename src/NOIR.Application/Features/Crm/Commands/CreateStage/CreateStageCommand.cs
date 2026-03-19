namespace NOIR.Application.Features.Crm.Commands.CreateStage;

/// <summary>
/// Adds a new active stage to a pipeline (before Won/Lost system stages).
/// </summary>
public sealed record CreateStageCommand(
    Guid PipelineId,
    string Name,
    string Color = "#6366f1") : IAuditableCommand<Features.Crm.DTOs.PipelineStageDto>
{
    public string? AuditUserId { get; init; }
    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => PipelineId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Added stage '{Name}' to pipeline";
}
