namespace NOIR.Application.Features.Configuration.Commands.UpdateConfiguration;

/// <summary>
/// Command to update a configuration section at runtime.
/// Implements IAuditableCommand for full audit trail with before/after diff.
/// </summary>
public sealed record UpdateConfigurationCommand(
    string SectionName,
    string NewValueJson) : IAuditableCommand<ConfigurationBackupDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => SectionName;

    public AuditOperationType OperationType => AuditOperationType.Update;

    public string? GetTargetDisplayName() => $"Config:{SectionName}";

    public string? GetActionDescription() =>
        $"Updated configuration section '{SectionName}'";
}
