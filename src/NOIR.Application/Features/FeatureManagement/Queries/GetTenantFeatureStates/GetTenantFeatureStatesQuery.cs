namespace NOIR.Application.Features.FeatureManagement.Queries.GetTenantFeatureStates;

/// <summary>
/// Query to retrieve module states for a specific tenant (platform admin view).
/// </summary>
public sealed record GetTenantFeatureStatesQuery(string TenantId);
