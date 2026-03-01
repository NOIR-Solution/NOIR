namespace NOIR.Domain.Events.Pm;

/// <summary>
/// Raised when a new project is created.
/// </summary>
public sealed record ProjectCreatedEvent(Guid ProjectId) : DomainEvent;

/// <summary>
/// Raised when a project is archived.
/// </summary>
public sealed record ProjectArchivedEvent(Guid ProjectId, string ProjectName) : DomainEvent;

/// <summary>
/// Raised when a project is completed.
/// </summary>
public sealed record ProjectCompletedEvent(Guid ProjectId, string ProjectName) : DomainEvent;

/// <summary>
/// Raised when a task is moved to a different column.
/// </summary>
public sealed record TaskMovedEvent(Guid TaskId, Guid? OldColumnId, Guid NewColumnId) : DomainEvent;

/// <summary>
/// Raised when a task is completed.
/// </summary>
public sealed record TaskCompletedEvent(Guid TaskId, Guid ProjectId, string TaskTitle) : DomainEvent;

/// <summary>
/// Raised when a task is assigned to an employee.
/// </summary>
public sealed record TaskAssignedEvent(Guid TaskId, Guid ProjectId, Guid? AssigneeId, string TaskTitle) : DomainEvent;
