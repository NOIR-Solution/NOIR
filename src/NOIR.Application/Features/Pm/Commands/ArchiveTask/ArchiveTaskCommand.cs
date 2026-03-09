namespace NOIR.Application.Features.Pm.Commands.ArchiveTask;

public sealed record ArchiveTaskCommand(Guid Id) : IAuditableCommand<Result<Guid>>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? AuditUserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Archived task";
}
