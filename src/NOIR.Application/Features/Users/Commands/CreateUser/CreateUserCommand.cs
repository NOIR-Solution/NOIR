namespace NOIR.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Command for admin to create a new user.
/// </summary>
public sealed record CreateUserCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    IReadOnlyList<string>? RoleNames,
    bool SendWelcomeEmail = true) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => Email; // Use email as target before ID is created
    public string? GetTargetDisplayName() => DisplayName ?? Email;
    public string? GetActionDescription() => $"Created user '{GetTargetDisplayName()}'";
}
