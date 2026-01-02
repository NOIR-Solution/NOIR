namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Query to get the complete audit trail for a correlation ID.
/// Returns HTTP request, handler executions, and entity changes.
/// </summary>
public sealed record GetAuditTrailQuery(string CorrelationId);
