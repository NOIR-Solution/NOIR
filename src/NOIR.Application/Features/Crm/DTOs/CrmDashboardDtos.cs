namespace NOIR.Application.Features.Crm.DTOs;

/// <summary>
/// CRM dashboard aggregate data.
/// </summary>
public sealed record CrmDashboardDto(
    int TotalContacts,
    int TotalCompanies,
    int ActiveLeads,
    int WonLeads,
    int LostLeads,
    decimal TotalPipelineValue,
    decimal WonDealValue,
    int WonDealsThisMonth,
    decimal WonValueThisMonth,
    int LostDealsThisMonth,
    decimal ConversionRate,
    IReadOnlyList<LeadsByStageDto> LeadsByStage,
    IReadOnlyList<LeadsByOwnerDto> LeadsByOwner);

/// <summary>
/// Leads count by pipeline stage.
/// </summary>
public sealed record LeadsByStageDto(
    string StageName,
    string Color,
    int Count,
    decimal TotalValue);

/// <summary>
/// Leads count by owner.
/// </summary>
public sealed record LeadsByOwnerDto(
    string OwnerName,
    int Count,
    decimal TotalValue);
