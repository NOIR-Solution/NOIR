namespace NOIR.Application.Features.Audit.Queries.GetActivityDetails;

/// <summary>
/// Query to get detailed information about a specific activity entry.
/// </summary>
public sealed record GetActivityDetailsQuery(
    /// <summary>
    /// ID of the handler audit log entry.
    /// </summary>
    Guid Id);
