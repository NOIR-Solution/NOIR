using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NOIR.Domain.Entities.Analytics;

namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Interface for the application database context.
/// Defines the contract for data access in the application layer.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Permission templates DbSet for direct access.
    /// </summary>
    DbSet<PermissionTemplate> PermissionTemplates { get; }

    /// <summary>
    /// Email templates DbSet for direct access.
    /// Used by Copy-on-Write pattern to query across tenant boundaries.
    /// </summary>
    DbSet<EmailTemplate> EmailTemplates { get; }

    /// <summary>
    /// Legal pages DbSet for direct access.
    /// Used by Copy-on-Write pattern to query across tenant boundaries.
    /// </summary>
    DbSet<LegalPage> LegalPages { get; }

    /// <summary>
    /// Product categories DbSet for direct access.
    /// Used by ProductFilter for category hierarchy lookups.
    /// </summary>
    DbSet<ProductCategory> ProductCategories { get; }

    /// <summary>
    /// Product attributes DbSet for direct access.
    /// Used by ProductFilter for facet calculations.
    /// </summary>
    DbSet<ProductAttribute> ProductAttributes { get; }

    /// <summary>
    /// Product attribute values DbSet for direct access.
    /// Predefined values for select/multi-select type attributes.
    /// </summary>
    DbSet<ProductAttributeValue> ProductAttributeValues { get; }

    /// <summary>
    /// Category attributes DbSet for direct access.
    /// Junction table linking categories to their assigned product attributes.
    /// </summary>
    DbSet<CategoryAttribute> CategoryAttributes { get; }

    /// <summary>
    /// Product attribute assignments DbSet for direct access.
    /// Stores a product's actual attribute values with polymorphic value storage.
    /// </summary>
    DbSet<ProductAttributeAssignment> ProductAttributeAssignments { get; }

    /// <summary>
    /// Product filter indexes DbSet for direct access.
    /// Denormalized table for high-performance product filtering.
    /// </summary>
    DbSet<ProductFilterIndex> ProductFilterIndexes { get; }

    /// <summary>
    /// Filter analytics events DbSet for direct access.
    /// Used for tracking and analyzing filter usage patterns.
    /// </summary>
    DbSet<FilterAnalyticsEvent> FilterAnalyticsEvents { get; }

    /// <summary>
    /// Order notes DbSet for non-aggregate entity CRUD.
    /// OrderNote is a TenantEntity (not AggregateRoot), so IRepository is not applicable.
    /// </summary>
    DbSet<OrderNote> OrderNotes { get; }

    /// <summary>
    /// Customer group memberships DbSet for junction table CRUD.
    /// CustomerGroupMembership is a TenantEntity (not AggregateRoot), so IRepository is not applicable.
    /// </summary>
    DbSet<CustomerGroupMembership> CustomerGroupMemberships { get; }

    /// <summary>
    /// Tenant module states DbSet for feature management CRUD.
    /// TenantModuleState is a TenantEntity (not AggregateRoot), so IRepository is not applicable.
    /// </summary>
    DbSet<TenantModuleState> TenantModuleStates { get; }

    /// <summary>
    /// Webhook delivery logs DbSet for non-aggregate entity CRUD.
    /// WebhookDeliveryLog is a TenantEntity (not AggregateRoot), so IRepository is not applicable.
    /// </summary>
    DbSet<WebhookDeliveryLog> WebhookDeliveryLogs { get; }

    /// <summary>
    /// Employee tag assignments DbSet for junction table CRUD.
    /// EmployeeTagAssignment is a TenantEntity (not AggregateRoot), so IRepository is not applicable.
    /// </summary>
    DbSet<EmployeeTagAssignment> EmployeeTagAssignments { get; }

    /// <summary>
    /// Employees DbSet for aggregate queries (GROUP BY reports).
    /// </summary>
    DbSet<Employee> Employees { get; }

    /// <summary>
    /// Departments DbSet for aggregate queries (GROUP BY reports).
    /// </summary>
    DbSet<Department> Departments { get; }

    /// <summary>
    /// Employee tags DbSet for aggregate queries (GROUP BY reports).
    /// </summary>
    DbSet<EmployeeTag> EmployeeTags { get; }

    /// <summary>
    /// CRM contacts DbSet for aggregate queries.
    /// </summary>
    DbSet<CrmContact> CrmContacts { get; }

    /// <summary>
    /// CRM companies DbSet for aggregate queries.
    /// </summary>
    DbSet<CrmCompany> CrmCompanies { get; }

    /// <summary>
    /// CRM leads DbSet for aggregate queries.
    /// </summary>
    DbSet<Lead> Leads { get; }

    /// <summary>
    /// CRM pipelines DbSet for aggregate queries.
    /// </summary>
    DbSet<Pipeline> Pipelines { get; }

    /// <summary>
    /// CRM pipeline stages DbSet for direct stage mutations.
    /// </summary>
    DbSet<PipelineStage> PipelineStages { get; }

    /// <summary>
    /// CRM activities DbSet for aggregate queries.
    /// </summary>
    DbSet<CrmActivity> CrmActivities { get; }

    /// <summary>
    /// PM projects DbSet for search queries.
    /// </summary>
    DbSet<Project> Projects { get; }

    /// <summary>
    /// PM project tasks DbSet for search queries.
    /// </summary>
    DbSet<ProjectTask> ProjectTasks { get; }

    /// <summary>
    /// PM project members DbSet for non-aggregate entity CRUD.
    /// </summary>
    DbSet<ProjectMember> ProjectMembers { get; }

    /// <summary>
    /// PM project columns DbSet for non-aggregate entity CRUD.
    /// </summary>
    DbSet<ProjectColumn> ProjectColumns { get; }

    /// <summary>
    /// PM task labels DbSet for non-aggregate entity CRUD.
    /// </summary>
    DbSet<TaskLabel> TaskLabels { get; }

    /// <summary>
    /// PM task comments DbSet for non-aggregate entity CRUD.
    /// </summary>
    DbSet<TaskComment> TaskComments { get; }

    /// <summary>
    /// PM task-label junction DbSet for non-aggregate entity CRUD.
    /// </summary>
    DbSet<ProjectTaskLabel> ProjectTaskLabels { get; }

    /// <summary>
    /// API keys DbSet for auth middleware lookups by KeyIdentifier.
    /// </summary>
    DbSet<Domain.Entities.ApiKey> ApiKeys { get; }

    /// <summary>
    /// Attaches an entity to the context for tracking.
    EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
