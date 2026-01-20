# Email Templates Architecture

## Overview

NOIR uses a **database-driven email template system** with multi-tenant support. Email templates are stored in the `EmailTemplate` table and loaded dynamically at runtime.

## Key Concepts

### Database-Driven Templates

- **Source of Truth**: `EmailTemplate` table in the database
- **Seeded by**: `ApplicationDbContextSeeder.cs` in the Infrastructure project
- **Loaded via**: `EmailService.SendTemplateAsync<T>()` using `GetTemplateWithFallbackAsync()`
- **Not used**: `.cshtml` files in `src/NOIR.Web/EmailTemplates/` (legacy/unused)

### Multi-Tenant Architecture

Email templates follow a **platform-default with tenant-override** pattern:

1. **Platform Templates** (`TenantId = null`)
   - Created during database seeding
   - Visible to all tenants by default
   - Cannot be deleted by tenants
   - Marked with `IsPlatformDefault = true`

2. **Tenant Override Templates** (`TenantId = specific tenant`)
   - Created when a tenant customizes a platform template
   - Only visible to that specific tenant
   - Shadows the platform template for that tenant
   - Can be deleted to revert to platform default

### Copy-on-Write Pattern

When a tenant edits a platform template:
1. System creates a copy with `TenantId = current tenant`
2. Copy has same `Name` as platform template
3. Tenant's copy takes precedence via `GetTemplateWithFallbackAsync()`
4. Deleting tenant's copy reveals platform template again

## Email Template Flow

### Sending an Email

```csharp
// 1. Create a typed model
var model = new WelcomeEmailModel(
    UserName: "John Doe",
    Email: "john@example.com",
    TemporaryPassword: "TempPass123!",
    LoginUrl: "https://app.com/login",
    ApplicationName: "NOIR");

// 2. Send via EmailService
await _emailService.SendTemplateAsync(
    to: "john@example.com",
    subject: "Welcome to NOIR",
    templateName: "WelcomeEmail",  // Matches EmailTemplate.Name
    model: model,
    cancellationToken);
```

### Template Loading Logic

```csharp
// GetTemplateWithFallbackAsync() in EmailService.cs
// 1. Query for tenant-specific template (TenantId = current tenant, Name = templateName)
// 2. If not found, query for platform template (TenantId = null, Name = templateName)
// 3. Return first match or null
```

### Placeholder Replacement

Templates use `{{Variable}}` syntax replaced via reflection:

```html
<p>Hello {{UserName}},</p>
<p>Your temporary password is: {{TemporaryPassword}}</p>
```

The `ReplacePlaceholders<T>()` method:
1. Finds all `{{PropertyName}}` patterns
2. Uses reflection to get property value from model
3. Replaces placeholder with actual value

## Available Templates

| Template Name | Purpose | Model Type | Variables |
|---------------|---------|------------|-----------|
| `PasswordResetOtp` | Password reset OTP code | `PasswordResetOtpEmailModel` | UserName, OtpCode, ExpiryMinutes |
| `EmailChangeOtp` | Email change verification OTP | `EmailChangeOtpEmailModel` | UserName, OtpCode, ExpiryMinutes |
| `WelcomeEmail` | New user welcome with temp password | `WelcomeEmailModel` | UserName, Email, TemporaryPassword, LoginUrl, ApplicationName |

## Template Styling

All templates share consistent modern styling:

- **Font**: `Arial, sans-serif`
- **Header Gradient**: `linear-gradient(135deg, #1e40af 0%, #0891b2 100%)`
- **Background**: `#f9fafb` for content area
- **Typography**: Clear hierarchy with h2 (`#1e40af`), body text (`#333`), muted text (`#6b7280`)
- **Code Display**: Prominent boxes with white text on blue background
- **Footer**: Consistent copyright notice

## Revert to Platform Default

Tenants can revert customized templates back to platform defaults:

### Backend Implementation

```csharp
// Command
public sealed record RevertToPlatformDefaultCommand(Guid Id);

// Handler logic
1. Verify user is a tenant user (not platform)
2. Find tenant's custom template by ID
3. Verify it's not a platform template
4. Find corresponding platform template by Name
5. Soft delete tenant's custom template
6. Return platform template with IsInherited = true
```

### Frontend Usage

```typescript
const handleRevert = async () => {
  const reverted = await revertToPlatformDefault(templateId)
  // UI updates to show platform template
}
```

### API Endpoint

```
DELETE /api/email-templates/{id}/revert
Authorization: Permissions.EmailTemplatesUpdate
```

## Welcome Email Service

Centralized service for sending welcome emails to newly created users:

### Interface

```csharp
public interface IWelcomeEmailService
{
    Task SendWelcomeEmailAsync(
        string email,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken = default);
}
```

### Implementation

```csharp
public class WelcomeEmailService : IWelcomeEmailService, IScopedService
{
    // Automatically registered via IScopedService marker
    // Builds login URL, creates model, sends email
    // Fire-and-forget pattern (logs errors, doesn't throw)
}
```

### Usage

**CreateUserCommandHandler** (admin creates user):
```csharp
if (command.SendWelcomeEmail)
{
    _ = _welcomeEmailService.SendWelcomeEmailAsync(email, userName, password, ct);
}
```

**ProvisionTenantCommandHandler** (tenant provisioning):
```csharp
// Always sends welcome email after creating tenant admin
await _welcomeEmailService.SendWelcomeEmailAsync(email, userName, password, ct);
```

## Adding New Templates

### 1. Create Model

```csharp
// In Application/Features/YourFeature/DTOs/
public record YourEmailModel(
    string Variable1,
    string Variable2);
```

### 2. Add to Database Seeder

```csharp
// In ApplicationDbContextSeeder.cs
private static string GetYourEmailHtmlBody() => """
    <!DOCTYPE html>
    <html>
    <head>...</head>
    <body>
        <div>{{Variable1}}</div>
        <div>{{Variable2}}</div>
    </body>
    </html>
    """;

// In SeedEmailTemplatesAsync()
var yourTemplate = EmailTemplate.CreatePlatformDefault(
    "YourEmail",
    "Subject Line",
    GetYourEmailHtmlBody(),
    GetYourEmailPlainTextBody(),
    isActive: true,
    availableVariables: "Variable1,Variable2",
    description: "Description for admins");
```

### 3. Add Sample Data for Preview

```typescript
// In frontend/src/services/emailTemplates.ts
export function getDefaultSampleData(variables: string[]): Record<string, string> {
  const sampleValues: Record<string, string> = {
    // ... existing values
    Variable1: 'Sample Value 1',
    Variable2: 'Sample Value 2',
  }
  // ...
}
```

### 4. Send Email

```csharp
var model = new YourEmailModel("Value1", "Value2");
await _emailService.SendTemplateAsync(
    to: email,
    subject: "Your Subject",
    templateName: "YourEmail",
    model: model,
    cancellationToken);
```

## Best Practices

### DO ✅

- Store templates in database (seeded by ApplicationDbContextSeeder)
- Use typed models for template data
- Follow platform/tenant pattern for multi-tenancy
- Use `IWelcomeEmailService` for welcome emails (avoid duplication)
- Add new variables to `getDefaultSampleData()` for preview
- Use `{{Variable}}` syntax for placeholders
- Include both HTML and plain text versions
- Fire-and-forget email sending (log errors, don't throw)

### DON'T ❌

- Create `.cshtml` files in `src/NOIR.Web/EmailTemplates/` (not used)
- Hard-delete templates (use soft delete for tenant overrides)
- Skip sample data (breaks preview functionality)
- Inline email HTML in handlers (use database templates)
- Throw exceptions on email failure (should be non-blocking)
- Duplicate email sending logic (use shared services)

## Troubleshooting

### Template Not Found

```csharp
// Check logs for:
[ERR] Email template '{TemplateName}' not found (checked tenant and platform level)

// Solutions:
1. Verify template exists in database
2. Check template Name matches exactly
3. Ensure template IsActive = true
4. Run database seeder if missing
```

### Tenant Can't See Template

```csharp
// Debug GetTemplateWithFallbackAsync():
1. Check current tenant ID
2. Verify platform template exists (TenantId = null)
3. Check if tenant has override (same Name, their TenantId)
4. Ensure template IsDeleted = false
```

### Placeholders Not Replaced

```csharp
// Check model properties:
1. Property names must match exactly (case-sensitive)
2. Model must have public getters
3. Template must use {{PropertyName}} syntax
4. Use ReplacePlaceholders<T>() with correct model type
```

## Related Files

- **Service**: `src/NOIR.Infrastructure/Services/EmailService.cs`
- **Seeder**: `src/NOIR.Infrastructure/Persistence/ApplicationDbContextSeeder.cs`
- **Entity**: `src/NOIR.Domain/Entities/EmailTemplate.cs`
- **Endpoints**: `src/NOIR.Web/Endpoints/EmailTemplateEndpoints.cs`
- **Frontend**: `src/NOIR.Web/frontend/src/services/emailTemplates.ts`
- **Welcome Service**: `src/NOIR.Infrastructure/Services/WelcomeEmailService.cs`
- **Interface**: `src/NOIR.Application/Common/Interfaces/IWelcomeEmailService.cs`
