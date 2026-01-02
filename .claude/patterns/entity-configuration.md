# Entity Configuration Pattern

**Created:** 2025-12-31
**Based on:** EF Core IEntityTypeConfiguration research

---

## Overview

NOIR uses **IEntityTypeConfiguration** classes for clean, organized entity configuration with automatic discovery via `ApplyConfigurationsFromAssembly`.

---

## File Locations

```
src/NOIR.Infrastructure/Persistence/Configurations/
├── _Shared/
│   └── BaseEntityConfiguration.cs    # Base classes for common patterns
├── RefreshTokenConfiguration.cs
├── AuditLogConfiguration.cs
└── PermissionConfiguration.cs
```

---

## Creating a New Configuration

### 1. Create configuration class

```csharp
namespace NOIR.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.Email).IsUnique();
    }
}
```

### 2. Done - Auto-discovered!

`ApplyConfigurationsFromAssembly` in `ApplicationDbContext.OnModelCreating` finds it automatically.

---

## Base Configuration Classes

### For entities with `Entity<Guid>` base:
```csharp
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : Entity<Guid>
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.CreatedAt).IsRequired();
    }
}
```

### For auditable entities:
```csharp
public abstract class AuditableEntityConfiguration<TEntity> : BaseEntityConfiguration<TEntity>
    where TEntity : Entity<Guid>, IAuditableEntity
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        builder.Property(e => e.CreatedBy).HasMaxLength(450);
        builder.Property(e => e.ModifiedBy).HasMaxLength(450);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => e.IsDeleted);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
```

---

## Global Conventions

In `ApplicationDbContext.ConfigureConventions`:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    // Default string length
    configurationBuilder.Properties<string>()
        .HaveMaxLength(500);

    // Decimal precision
    configurationBuilder.Properties<decimal>()
        .HavePrecision(18, 2);

    // UTC DateTimeOffset storage
    configurationBuilder.Properties<DateTimeOffset>()
        .HaveConversion<UtcDateTimeOffsetConverter>();

    // Store enums as strings
    configurationBuilder.Properties<Enum>()
        .HaveConversion<string>();
}
```

---

## String Length Convention

`StringLengthByNameConvention` auto-applies max lengths by property name:

| Property Name Pattern | Max Length |
|-----------------------|------------|
| `*Name`, `*Title` | 200 |
| `*Email` | 256 |
| `*Description` | 2000 |
| `*IpAddress` | 45 |
| `*Url` | 2000 |

---

## Key Points

1. **Never configure entities inline** in `OnModelCreating` - use separate configuration classes
2. **No manual ApplyConfiguration** needed - assembly scanning handles discovery
3. **Use conventions** for common patterns to reduce repetition
4. **Inherit base classes** for shared configuration (audit, soft delete)
