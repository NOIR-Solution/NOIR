namespace NOIR.Application.Features.LegalPages.Queries.GetPublicLegalPage;

/// <summary>
/// Query to get a legal page by slug for public website display.
/// Resolves tenant override if available, otherwise returns platform default.
/// </summary>
public sealed record GetPublicLegalPageQuery(string Slug);
