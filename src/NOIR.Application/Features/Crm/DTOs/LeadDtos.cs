namespace NOIR.Application.Features.Crm.DTOs;

/// <summary>
/// Full lead details for detail view.
/// </summary>
public sealed record LeadDto(
    Guid Id,
    string Title,
    Guid ContactId,
    string ContactName,
    string? ContactEmail,
    Guid? CompanyId,
    string? CompanyName,
    decimal Value,
    string Currency,
    Guid? OwnerId,
    string? OwnerName,
    Guid PipelineId,
    string PipelineName,
    Guid StageId,
    string StageName,
    string? StageColor,
    LeadStatus Status,
    double SortOrder,
    DateTimeOffset? ExpectedCloseDate,
    DateTimeOffset? WonAt,
    DateTimeOffset? LostAt,
    string? LostReason,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);

/// <summary>
/// Lead card for Kanban board view.
/// </summary>
public sealed record LeadCardDto(
    Guid Id,
    string Title,
    string ContactName,
    string? CompanyName,
    decimal Value,
    string Currency,
    string? OwnerName,
    LeadStatus Status,
    double SortOrder,
    DateTimeOffset? ExpectedCloseDate,
    DateTimeOffset CreatedAt);

/// <summary>
/// Request body for creating a lead.
/// </summary>
public sealed record CreateLeadRequest(
    string Title,
    Guid ContactId,
    Guid PipelineId,
    Guid? CompanyId = null,
    decimal Value = 0,
    string Currency = "USD",
    Guid? OwnerId = null,
    DateTimeOffset? ExpectedCloseDate = null,
    string? Notes = null);

/// <summary>
/// Request body for updating a lead.
/// </summary>
public sealed record UpdateLeadRequest(
    string Title,
    Guid ContactId,
    Guid? CompanyId = null,
    decimal Value = 0,
    string Currency = "USD",
    Guid? OwnerId = null,
    DateTimeOffset? ExpectedCloseDate = null,
    string? Notes = null);

/// <summary>
/// Request body for moving a lead to a different stage.
/// </summary>
public sealed record MoveLeadStageRequest(
    Guid NewStageId,
    double NewSortOrder);

/// <summary>
/// Request body for losing a lead.
/// </summary>
public sealed record LoseLeadRequest(
    string? Reason = null);
