namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds the default CRM pipeline with 5 stages per tenant.
/// Only runs if no pipeline exists for the current tenant.
/// </summary>
public class CrmPipelineSeeder : ISeeder
{
    /// <summary>
    /// Runs after tenant admin creation, before permissions.
    /// </summary>
    public int Order => 60;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        // Only seed if a tenant context is set (pipeline is tenant-scoped)
        if (context.DefaultTenant is null)
        {
            return;
        }

        var tenantId = context.DefaultTenant.Id;
        var db = context.DbContext;

        // Idempotent: skip if any pipeline already exists for this tenant
        var exists = await db.Pipelines
            .TagWith("CrmPipelineSeeder")
            .AnyAsync(p => p.TenantId == tenantId, ct);

        if (exists)
        {
            context.Logger.LogInformation("[Seeder] CRM pipeline already exists for tenant {TenantId}. Skipping.", tenantId);
            return;
        }

        var pipeline = Pipeline.Create("Sales Pipeline", tenantId, isDefault: true);

        var stages = new[]
        {
            PipelineStage.Create(pipeline.Id, "New", 0, tenantId, "#6366f1"),
            PipelineStage.Create(pipeline.Id, "Contacted", 1, tenantId, "#3b82f6"),
            PipelineStage.Create(pipeline.Id, "Qualified", 2, tenantId, "#f59e0b"),
            PipelineStage.Create(pipeline.Id, "Proposal", 3, tenantId, "#10b981"),
            PipelineStage.Create(pipeline.Id, "Negotiation", 4, tenantId, "#ef4444"),
        };

        db.Pipelines.Add(pipeline);
        db.Set<PipelineStage>().AddRange(stages);
        await db.SaveChangesAsync(ct);

        context.Logger.LogInformation(
            "[Seeder] Seeded default CRM pipeline '{Name}' with {Count} stages for tenant {TenantId}.",
            pipeline.Name, stages.Length, tenantId);
    }
}
