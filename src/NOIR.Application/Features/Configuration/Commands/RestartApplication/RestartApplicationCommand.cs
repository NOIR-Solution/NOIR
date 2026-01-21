namespace NOIR.Application.Features.Configuration.Commands.RestartApplication;

/// <summary>
/// Command to initiate a graceful application restart.
/// Implements IAuditableCommand for audit trail.
/// </summary>
public sealed record RestartApplicationCommand(
    string Reason) : IAuditableCommand<RestartApplicationResult>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => null;

    public AuditOperationType OperationType => AuditOperationType.Execute;

    public string? GetTargetDisplayName() => "Application";

    public string? GetActionDescription() => $"Restarted application: {Reason}";
}

/// <summary>
/// Result of application restart command.
/// </summary>
public sealed record RestartApplicationResult(
    string Message,
    string Environment,
    DateTimeOffset InitiatedAt);
