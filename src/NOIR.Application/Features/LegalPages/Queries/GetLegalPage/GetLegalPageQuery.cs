namespace NOIR.Application.Features.LegalPages.Queries.GetLegalPage;

/// <summary>
/// Query to get a single legal page by ID for admin editing.
/// </summary>
public sealed record GetLegalPageQuery(Guid Id);
