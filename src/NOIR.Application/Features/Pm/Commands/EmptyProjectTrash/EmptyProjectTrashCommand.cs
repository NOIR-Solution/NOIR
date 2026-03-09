namespace NOIR.Application.Features.Pm.Commands.EmptyProjectTrash;

public sealed record EmptyProjectTrashCommand(Guid ProjectId) : IAuditableCommand<Result<int>>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? AuditUserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => ProjectId;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Emptied project trash bin";
}
