namespace NOIR.Application.Features.Crm.DTOs;

/// <summary>
/// Pipeline details with stages.
/// </summary>
public sealed record PipelineDto(
    Guid Id,
    string Name,
    bool IsDefault,
    IReadOnlyList<PipelineStageDto> Stages,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);

/// <summary>
/// Pipeline stage details.
/// </summary>
public sealed record PipelineStageDto(
    Guid Id,
    string Name,
    int SortOrder,
    string Color);

/// <summary>
/// Pipeline Kanban view with leads grouped by stage.
/// </summary>
public sealed record PipelineViewDto(
    Guid Id,
    string Name,
    IReadOnlyList<StageWithLeadsDto> Stages);

/// <summary>
/// Stage with its leads for Kanban view.
/// </summary>
public sealed record StageWithLeadsDto(
    Guid Id,
    string Name,
    int SortOrder,
    string Color,
    IReadOnlyList<LeadCardDto> Leads,
    decimal TotalValue,
    int LeadCount);

/// <summary>
/// Stage data for creating a pipeline.
/// </summary>
public sealed record CreatePipelineStageDto(
    string Name,
    int SortOrder,
    string Color = "#6366f1");

/// <summary>
/// Stage data for updating a pipeline. Null Id = new stage.
/// </summary>
public sealed record UpdatePipelineStageDto(
    Guid? Id,
    string Name,
    int SortOrder,
    string Color = "#6366f1");

/// <summary>
/// Request body for creating a pipeline.
/// </summary>
public sealed record CreatePipelineRequest(
    string Name,
    bool IsDefault,
    List<CreatePipelineStageDto> Stages);

/// <summary>
/// Request body for updating a pipeline.
/// </summary>
public sealed record UpdatePipelineRequest(
    string Name,
    bool IsDefault,
    List<UpdatePipelineStageDto> Stages);
