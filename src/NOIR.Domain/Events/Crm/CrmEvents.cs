namespace NOIR.Domain.Events.Crm;

/// <summary>
/// Raised when a new CRM contact is created.
/// </summary>
public sealed record ContactCreatedEvent(Guid ContactId) : DomainEvent;

/// <summary>
/// Raised when a new CRM company is created.
/// </summary>
public sealed record CompanyCreatedEvent(Guid CompanyId) : DomainEvent;

/// <summary>
/// Raised when a new lead is created.
/// </summary>
public sealed record LeadCreatedEvent(Guid LeadId) : DomainEvent;

/// <summary>
/// Raised when a lead is marked as won.
/// </summary>
public record LeadWonEvent(Guid LeadId, Guid ContactId, Guid? CustomerId) : DomainEvent;

/// <summary>
/// Raised when a lead is marked as lost.
/// </summary>
public record LeadLostEvent(Guid LeadId, string? Reason) : DomainEvent;

/// <summary>
/// Raised when a previously won or lost lead is reopened.
/// </summary>
public record LeadReopenedEvent(Guid LeadId) : DomainEvent;
