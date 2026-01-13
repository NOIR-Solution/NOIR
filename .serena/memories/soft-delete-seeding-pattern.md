# Soft-Delete + Seeding Pattern

## Problem

When entities have soft-delete query filters (`.HasQueryFilter(e => !e.IsDeleted)`), database seeders must handle the scenario where required system data has been accidentally soft-deleted.

### The Issue

If a seeder checks for existing entities using `IgnoreQueryFilters()` but doesn't restore soft-deleted entities:
1. The seeder finds the soft-deleted entity
2. The seeder doesn't create a new one (entity exists)
3. Normal queries can't find the entity (filtered out by query filter)
4. System functionality breaks (e.g., multi-tenant resolution fails)

## Solution Pattern

### For Record Types (like Tenant)

Use the record `with` expression and EF Core's `SetValues`:

```csharp
var existingTenant = await context.TenantInfo
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(t => t.Identifier == "default");

if (existingTenant.IsDeleted)
{
    var restoredTenant = existingTenant with
    {
        IsDeleted = false,
        DeletedAt = null,
        DeletedBy = null,
        IsActive = true,
        ModifiedAt = DateTimeOffset.UtcNow
    };
    context.TenantInfo.Entry(existingTenant).CurrentValues.SetValues(restoredTenant);
    await context.SaveChangesAsync();
}
```

### For Entity Classes with Protected Setters

Use EF Core's Entry API for protected properties:

```csharp
var existing = await context.Set<EmailTemplate>()
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(t => t.Name == templateName);

if (existing.IsDeleted)
{
    var entry = context.Entry(existing);
    entry.Property(e => e.IsDeleted).CurrentValue = false;
    entry.Property(e => e.DeletedAt).CurrentValue = null;
    entry.Property(e => e.DeletedBy).CurrentValue = null;
    entry.Property(e => e.ModifiedAt).CurrentValue = DateTimeOffset.UtcNow;
    existing.Activate(); // Use domain method for business properties
    await context.SaveChangesAsync();
}
```

## Affected Seeders

| Seeder | Entity | Issue Fixed |
|--------|--------|-------------|
| `SeedDefaultTenantAsync` | Tenant | ✅ Fixed - restores soft-deleted default tenant |
| `SeedEmailTemplatesAsync` | EmailTemplate | ✅ Fixed - restores soft-deleted templates |
| `SeedRolesAsync` | IdentityRole | N/A - Identity roles don't have soft-delete |
| `SeedAdminUserAsync` | ApplicationUser | Low risk - no query filter on ApplicationUser |

## Testing Considerations

Standard integration tests use fresh databases and don't catch soft-delete issues because:
- Tests start with empty/clean database state
- Seeders run on fresh data, never encounter soft-deleted entities

### Add Explicit Soft-Delete Tests

```csharp
[Fact]
public async Task SeedDefaultTenantAsync_WhenTenantSoftDeleted_ShouldRestoreTenant()
{
    // Arrange - Soft delete the entity
    var entity = await context.Entities.IgnoreQueryFilters()
        .FirstOrDefaultAsync(e => e.Key == "default");
    
    // Soft delete using Entry API or record with expression
    var entry = context.Entry(entity);
    entry.Property(e => e.IsDeleted).CurrentValue = true;
    await context.SaveChangesAsync();

    // Act - Run seeder
    await Seeder.SeedAsync(context, logger);

    // Assert - Entity should be restored
    var restored = await context.Entities.IgnoreQueryFilters()
        .FirstOrDefaultAsync(e => e.Key == "default");
    restored.IsDeleted.Should().BeFalse();
}
```

## Key Takeaways

1. **Always use `IgnoreQueryFilters()`** when checking for seed data existence
2. **Always restore soft-deleted entities** rather than leaving them soft-deleted
3. **Use EF Core Entry API** for entities with protected setters
4. **Add explicit tests** for soft-delete restoration scenarios
5. **Document expected system data** that must never be permanently deleted
