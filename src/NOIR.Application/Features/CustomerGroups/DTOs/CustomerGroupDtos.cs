namespace NOIR.Application.Features.CustomerGroups.DTOs;

/// <summary>
/// Full customer group details DTO.
/// </summary>
public sealed record CustomerGroupDto(
    Guid Id,
    string Name,
    string? Description,
    string Slug,
    bool IsActive,
    int MemberCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Customer group list item DTO (minimal data for listings).
/// </summary>
public sealed record CustomerGroupListDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    int MemberCount);

/// <summary>
/// Request to create a new customer group.
/// </summary>
public sealed record CreateCustomerGroupRequest(
    string Name,
    string? Description);

/// <summary>
/// Request to update an existing customer group.
/// </summary>
public sealed record UpdateCustomerGroupRequest(
    string Name,
    string? Description,
    bool IsActive);

/// <summary>
/// Request to assign customers to a group.
/// </summary>
public sealed record AssignCustomersToGroupRequest(
    List<Guid> CustomerIds);

/// <summary>
/// Request to remove customers from a group.
/// </summary>
public sealed record RemoveCustomersFromGroupRequest(
    List<Guid> CustomerIds);
