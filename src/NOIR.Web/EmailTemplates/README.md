# Email Templates - LEGACY/UNUSED

⚠️ **IMPORTANT**: These .cshtml files are **NOT USED** by the email sending system.

## Architecture

The NOIR email system loads templates from the **database**, not from .cshtml files.

- **Source of Truth**: `EmailTemplate` table in the database
- **Seeded by**: `ApplicationDbContextSeeder.cs` (Infrastructure project)
- **Multi-tenant**: Platform-level defaults (TenantId = null) with tenant overrides
- **Editing**: Via Admin UI at `/portal/email-templates`

## Email Flow

1. `EmailService.SendTemplateAsync<T>()` is called
2. `GetTemplateWithFallbackAsync()` queries the database
3. Template HTML is loaded from `EmailTemplate.HtmlBody` column
4. Placeholders like `{{UserName}}` are replaced using reflection
5. Email is sent via FluentEmail

## Why .cshtml Files Exist

These files appear to be legacy artifacts from an earlier implementation approach or experimentation. They are not compiled or referenced by the email sending infrastructure.

## Recommendation

- **Do NOT edit** these .cshtml files - changes won't affect sent emails
- **Edit database templates** via the Admin UI or update `ApplicationDbContextSeeder.cs`
- **Consider removing** these files to avoid confusion

## Current Database Templates

| Template Name | Description |
|---------------|-------------|
| PasswordResetOtp | OTP code for password reset |
| EmailChangeOtp | OTP code for email address change |
| WelcomeEmail | Welcome email with temporary password |

All templates use modern styling with gradient headers and consistent branding.
