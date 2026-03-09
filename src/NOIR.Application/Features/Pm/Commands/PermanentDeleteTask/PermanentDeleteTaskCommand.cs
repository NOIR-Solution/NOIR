namespace NOIR.Application.Features.Pm.Commands.PermanentDeleteTask;

public sealed record PermanentDeleteTaskCommand(Guid Id) : IAuditableCommand<Result<Guid>>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? AuditUserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => null;
    public string? GetActionDescription() => "Permanently deleted archived task";
}
