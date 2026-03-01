namespace NOIR.Application.Features.Hr.Commands.ImportEmployees;

/// <summary>
/// Command to import employees from a CSV file.
/// </summary>
public sealed record ImportEmployeesCommand(
    byte[] FileData,
    string FileName) : IAuditableCommand<ImportResultDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => null;
    public string? GetTargetDisplayName() => FileName;
    public string? GetActionDescription() => $"Imported employees from {FileName}";
}
