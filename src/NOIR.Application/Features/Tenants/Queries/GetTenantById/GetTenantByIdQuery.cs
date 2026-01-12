namespace NOIR.Application.Features.Tenants.Queries.GetTenantById;

/// <summary>
/// Query to get a tenant by its ID.
/// </summary>
public sealed record GetTenantByIdQuery(Guid TenantId);
