namespace NOIR.Application.Features.LegalPages.Commands.UpdateLegalPage;

/// <summary>
/// Command to update a legal page's content.
/// Implements Copy-on-Write: when a tenant edits a platform page, a tenant-specific copy is created.
/// </summary>
public sealed record UpdateLegalPageCommand(
    Guid Id,
    string Title,
    string HtmlContent,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool AllowIndexing = true) : IAuditableCommand<LegalPageDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Title;
    public string? GetActionDescription() => $"Updated legal page '{Title}'";
}
