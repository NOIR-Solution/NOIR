namespace NOIR.Application.Features.EmailTemplates.DTOs;

/// <summary>
/// Full email template details for editing.
/// </summary>
public sealed record EmailTemplateDto(
    Guid Id,
    string Name,
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    bool IsActive,
    int Version,
    string? Description,
    List<string> AvailableVariables,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Simplified email template for list views.
/// </summary>
public sealed record EmailTemplateListDto(
    Guid Id,
    string Name,
    string Subject,
    bool IsActive,
    int Version,
    string? Description,
    List<string> AvailableVariables);

/// <summary>
/// Request to update an email template.
/// </summary>
public sealed record UpdateEmailTemplateRequest(
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    string? Description);

/// <summary>
/// Request to send a test email.
/// </summary>
public sealed record SendTestEmailRequest(
    string RecipientEmail,
    Dictionary<string, string> SampleData);

/// <summary>
/// Request to preview an email template with sample data.
/// </summary>
public sealed record PreviewEmailTemplateRequest(
    Dictionary<string, string> SampleData);

/// <summary>
/// Response from preview/test email operations.
/// </summary>
public sealed record EmailPreviewResponse(
    string Subject,
    string HtmlBody,
    string? PlainTextBody);
