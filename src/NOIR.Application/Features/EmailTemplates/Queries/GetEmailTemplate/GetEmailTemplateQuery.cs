namespace NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplate;

/// <summary>
/// Query to get a single email template by ID.
/// </summary>
public sealed record GetEmailTemplateQuery(Guid Id);
