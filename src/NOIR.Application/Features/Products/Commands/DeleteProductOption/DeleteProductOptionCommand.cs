namespace NOIR.Application.Features.Products.Commands.DeleteProductOption;

/// <summary>
/// Command to delete a product option.
/// </summary>
public sealed record DeleteProductOptionCommand(
    Guid ProductId,
    Guid OptionId,
    string? OptionName = null) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => OptionId;
    public string? GetTargetDisplayName() => OptionName ?? OptionId.ToString();
    public string? GetActionDescription() => $"Deleted option '{GetTargetDisplayName()}'";
}
