namespace NOIR.Application.Features.Crm.DTOs;

/// <summary>
/// Full contact details for detail view.
/// </summary>
public sealed record ContactDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? JobTitle,
    Guid? CompanyId,
    string? CompanyName,
    Guid? OwnerId,
    string? OwnerName,
    ContactSource Source,
    Guid? CustomerId,
    string? Notes,
    IReadOnlyList<LeadBriefDto> Leads,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);

/// <summary>
/// Simplified contact for list/table views.
/// </summary>
public sealed record ContactListDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? JobTitle,
    string? CompanyName,
    string? OwnerName,
    ContactSource Source,
    bool HasCustomer,
    DateTimeOffset CreatedAt);

/// <summary>
/// Brief lead info nested in ContactDto.
/// </summary>
public sealed record LeadBriefDto(
    Guid Id,
    string Title,
    decimal Value,
    string Currency,
    LeadStatus Status,
    string? StageName);

/// <summary>
/// Request body for creating a contact.
/// </summary>
public sealed record CreateContactRequest(
    string FirstName,
    string LastName,
    string Email,
    ContactSource Source,
    string? Phone = null,
    string? JobTitle = null,
    Guid? CompanyId = null,
    Guid? OwnerId = null,
    string? Notes = null);

/// <summary>
/// Request body for updating a contact.
/// </summary>
public sealed record UpdateContactRequest(
    string FirstName,
    string LastName,
    string Email,
    ContactSource Source,
    string? Phone = null,
    string? JobTitle = null,
    Guid? CompanyId = null,
    Guid? OwnerId = null,
    Guid? CustomerId = null,
    string? Notes = null);
