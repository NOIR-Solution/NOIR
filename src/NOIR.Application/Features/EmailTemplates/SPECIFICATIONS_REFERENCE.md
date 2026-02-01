# EmailTemplate Specifications Reference

This document serves as a quick reference for working with EmailTemplate specifications.

## Available Specifications

### For Reading (No AsTracking)

#### `EmailTemplatesWithSearchSpec`
Search for multiple templates with optional filtering.

```csharp
var spec = new EmailTemplatesWithSearchSpec(searchTerm: "password");
var templates = await _repository.ListAsync(spec, ct);
```

**Features:**
- Optional search by name or subject
- Ignores query filters (Copy-on-Write pattern)
- Ordered by name

---

#### `EmailTemplateByIdIgnoreFiltersSpec`
Get a single template by ID (read-only).

```csharp
var spec = new EmailTemplateByIdIgnoreFiltersSpec(templateId);
var template = await _repository.FirstOrDefaultAsync(spec, ct);
```

**Features:**
- Ignores query filters (can access platform templates)
- Read-only (no tracking)
- Typical use: display template details

---

#### `PlatformEmailTemplateByNameSpec`
Get a platform-level template by name.

```csharp
var spec = new PlatformEmailTemplateByNameSpec("PasswordReset");
var platformTemplate = await _repository.FirstOrDefaultAsync(spec, ct);
```

**Features:**
- Finds only platform templates (TenantId = null, IsPlatformDefault = true)
- Ignores query filters
- Read-only

---

### For Mutations (With AsTracking)

#### `EmailTemplateByIdForUpdateSpec`
Get a template by ID for updating/deleting.

```csharp
// For updating
var spec = new EmailTemplateByIdForUpdateSpec(templateId);
var template = await _repository.FirstOrDefaultAsync(spec, ct);

// Modify and save
template.Update(newSubject, newHtmlBody, newPlainText, newDescription, variables);
await _unitOfWork.SaveChangesAsync(ct);
```

**Features:**
- Tracked (EF Core change detection enabled)
- Ignores query filters (Copy-on-Write support)
- Typical use: update, toggle, revert operations

---

#### `EmailTemplateByNameAndTenantIdForUpdateSpec`
Get a tenant's custom copy of a template (for checking existence).

```csharp
var spec = new EmailTemplateByNameAndTenantIdForUpdateSpec("PasswordReset", currentTenantId);
var tenantCopy = await _repository.FirstOrDefaultAsync(spec, ct);

if (tenantCopy != null)
{
    // Tenant already has a copy - update it
    tenantCopy.Update(...);
    await _unitOfWork.SaveChangesAsync(ct);
}
else
{
    // No tenant copy - create new one
    var newCopy = EmailTemplate.CreateTenantOverride(...);
    await _repository.AddAsync(newCopy, ct);
    await _unitOfWork.SaveChangesAsync(ct);
}
```

**Features:**
- Tracked (for mutations)
- Ignores query filters
- Typical use: Copy-on-Write checks in toggle/update

---

## Copy-on-Write Pattern

The Copy-on-Write pattern allows tenants to inherit platform templates while optionally customizing them.

### Pattern Flow

1. **Get template** → Use `EmailTemplateByIdForUpdateSpec` to retrieve
2. **Check inheritance** → If `IsPlatformDefault && tenantId != null` → it's inherited
3. **On edit:**
   - If inherited → Use `EmailTemplate.CreateTenantOverride()` to create tenant copy
   - If not inherited → Update existing template in place
4. **On revert** → Use `PlatformEmailTemplateByNameSpec` to find platform version, soft-delete tenant copy

---

## Common Patterns

### Get a template for reading
```csharp
var spec = new EmailTemplateByIdIgnoreFiltersSpec(templateId);
var template = await _repository.FirstOrDefaultAsync(spec, cancellationToken);
```

### Get a template for updating
```csharp
var spec = new EmailTemplateByIdForUpdateSpec(templateId);
var template = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

if (template != null)
{
    template.Update(...);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
}
```

### List all templates with search
```csharp
var spec = new EmailTemplatesWithSearchSpec(searchTerm);
var templates = await _repository.ListAsync(spec, cancellationToken);
```

### Check if tenant has custom copy
```csharp
var spec = new EmailTemplateByNameAndTenantIdForUpdateSpec("PasswordReset", tenantId);
var tenantCopy = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

if (tenantCopy != null)
{
    // Tenant has customized this template
}
```

---

## Important Notes

### AsTracking vs No AsTracking

- **With AsTracking**: Use for mutations (update, delete). EF tracks changes automatically.
- **Without AsTracking**: Use for reading. More efficient, no tracking overhead.

### IgnoreQueryFilters

All EmailTemplate specs use `.IgnoreQueryFilters()` to support:
1. Accessing both tenant and platform templates in single query
2. Copy-on-Write pattern (showing platform templates as fallback)
3. Multi-tenancy inheritance

### Soft Deletes

Templates use soft delete (IsDeleted = true). All specs filter with `.Where(t => !t.IsDeleted)`.

---

## When to Create a New Specification

Create a new spec if:
1. You need a query that's not covered by existing specs
2. The query is reused across multiple handlers
3. The query is complex (multiple filters, joins, etc.)

**Name it:** `EmailTemplate[Filter1][Filter2]Spec` (e.g., `EmailTemplateByNameForDeleteSpec`)

**Template:**
```csharp
public sealed class EmailTemplate[YourSpec] : Specification<EmailTemplate>
{
    public EmailTemplate[YourSpec](/* params */)
    {
        Query.Where(t => /* your conditions */)
             .IgnoreQueryFilters() // if needed for Copy-on-Write
             .AsTracking()         // if needed for mutations
             .TagWith("DescriptiveMethodName");
    }
}
```

---

## Related Documentation

- **CLAUDE.md** - Project rules and conventions
- **Backend Patterns** - docs/backend/patterns/
- **Email Template Entity** - src/NOIR.Domain/Entities/EmailTemplate.cs
