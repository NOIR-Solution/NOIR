---
name: noir-migration
description: Create an EF Core migration in NOIR with the correct --context flag and output directory. Use when the user asks to add, generate, create, or roll back a database migration. Prevents the common mistake of running `dotnet ef migrations add` without `--context`, which creates the migration against the wrong DbContext and corrupts the Migrations/App vs Migrations/Tenant separation.
---

# noir-migration — Safe EF Core migrations

NOIR has **two DbContexts** and the CLI silently picks one if `--context` is omitted. Migrations end up in the wrong folder or apply to the wrong database. This skill enforces the correct invocation.

## Which context?

| Table scope | Context | Output dir |
|---|---|---|
| Tenant-scoped data (orders, products, customers, CRM, HR, PM, blog, etc.) — **99% of migrations** | `ApplicationDbContext` | `Migrations/App` |
| Tenant registry itself (Tenant rows, tenant-to-database mapping) | `TenantStoreDbContext` | `Migrations/Tenant` |

**Rule of thumb:** if you're not sure, it's `ApplicationDbContext`. Only touch `TenantStoreDbContext` when modifying the `Tenant` / `TenantStore` entities themselves in `NOIR.Domain/Tenancy/`.

## Workflow

### 1. Confirm with the user

Ask (unless obvious from the conversation):
- **Migration name** (PascalCase, describe the change): e.g. `AddInvoiceEntity`, `AddIndexToProductSlug`, `BackfillCustomerTier`
- **Which context** — App (default) or Tenant?
- **Is this additive only?** If it drops or renames columns, warn the user about production rollback concerns. Review CLAUDE.md Rule 4 (soft delete).

### 2. Pre-flight checks

```bash
# Must pass before generating migration
dotnet build src/NOIR.sln
```

Verify:
- Entity changes compile cleanly
- `{Entity}Configuration` includes `TenantId` in unique indexes (Rule 18)
- Soft delete filter (`HasQueryFilter`) preserved if entity implements `ISoftDeletable`

### 3. Generate the migration

**App context** (default):
```bash
dotnet ef migrations add <MigrationName> \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App
```

**Tenant context**:
```bash
dotnet ef migrations add <MigrationName> \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context TenantStoreDbContext \
  --output-dir Migrations/Tenant
```

### 4. Review generated SQL

Open `{timestamp}_{Name}.cs` in the target Migrations folder. Inspect the `Up`/`Down` methods. Reject the migration if:
- It drops a column that still has production data (needs a 2-phase migration: nullable column first, backfill, then drop in next release)
- It renames a column without `RenameColumn` (produces DROP + CREATE which loses data — EF usually infers rename from model config, but verify)
- Foreign key `OnDelete` is `Cascade` on a SQL Server path that already has another cascade — SQL Server rejects multiple cascade paths (see `gotchas_backend.md`)
- Unique index is missing `TenantId` (Rule 18) — fails silently across tenants

### 5. Apply to local database

```bash
# App
dotnet ef database update \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext

# Tenant
dotnet ef database update \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context TenantStoreDbContext
```

### 6. Post-apply verification

```bash
dotnet test src/NOIR.sln   # all tests must pass — integration tests will catch schema mismatches
```

Check the snapshot file got updated:
- App: `Migrations/App/ApplicationDbContextModelSnapshot.cs`
- Tenant: `Migrations/Tenant/TenantStoreDbContextModelSnapshot.cs`

## Rollback

If the migration hasn't been committed/pushed yet:
```bash
# Rollback DB first
dotnet ef database update <PreviousMigration> --context ApplicationDbContext \
  --project src/NOIR.Infrastructure --startup-project src/NOIR.Web

# Then remove the migration file
dotnet ef migrations remove --context ApplicationDbContext \
  --project src/NOIR.Infrastructure --startup-project src/NOIR.Web
```

If already pushed/merged: create a NEW forward migration that reverses the change. Never edit or delete a committed migration.

## Common mistakes this skill prevents

- Running `dotnet ef migrations add X` without `--context` → picks first DbContext alphabetically, migration lands in wrong folder
- Forgetting `--output-dir` → migration lands in `Migrations/` root instead of `App/` or `Tenant/` subdirectory
- Unique index without `TenantId` → silently fails for second tenant with same value (Rule 18)
- Cascade delete conflict on SQL Server multi-path FK
- Dropping a NOT NULL column with existing data — migration applies clean locally but fails in prod
- Renaming a column in the entity without running migration — model snapshot drifts from DB
- Editing a committed migration instead of creating a forward one
