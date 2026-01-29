namespace NOIR.Application.Features.Brands.Commands.DeleteBrand;

/// <summary>
/// Command to delete a brand (soft delete).
/// </summary>
public sealed record DeleteBrandCommand(
    Guid Id,
    string? BrandName = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => BrandName ?? Id.ToString();
    public string? GetActionDescription() => $"Deleted brand '{GetTargetDisplayName()}'";
}
