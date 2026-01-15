namespace NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplates;

/// <summary>
/// Query to get list of email templates with optional filtering.
/// </summary>
public sealed record GetEmailTemplatesQuery(
    string? Search = null);
