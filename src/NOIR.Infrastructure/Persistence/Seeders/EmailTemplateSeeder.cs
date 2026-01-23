namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds platform-level email templates (TenantId = null).
/// These are shared defaults that all tenants inherit from.
/// Tenants can customize these templates using the copy-on-edit pattern.
/// </summary>
public class EmailTemplateSeeder : ISeeder
{
    /// <summary>
    /// Email templates can be seeded after basic system setup.
    /// </summary>
    public int Order => 40;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        // Platform email templates (TenantId = null) are shared defaults across all tenants.
        // Tenants inherit these templates and can create their own copies via copy-on-edit.
        //
        // Smart upsert logic for platform templates:
        // 1. If template doesn't exist at platform level -> Add it
        // 2. If template exists AND Version = 1 -> Update it (never customized)
        // 3. If template exists AND Version > 1 -> Skip it (platform admin customized it)

        // Get existing platform-level templates (TenantId = null)
        var existingTemplates = await context.DbContext.Set<EmailTemplate>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetExistingEmailTemplates")
            .Where(t => t.TenantId == null && !t.IsDeleted)
            .ToListAsync(ct);

        var templateDefinitions = GetEmailTemplateDefinitions();
        var addedCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;

        foreach (var definition in templateDefinitions)
        {
            var existing = existingTemplates.FirstOrDefault(t => t.Name == definition.Name);

            if (existing == null)
            {
                // Template doesn't exist at platform level - add it
                await context.DbContext.Set<EmailTemplate>().AddAsync(definition, ct);
                addedCount++;
            }
            else if (existing.Version == 1)
            {
                // Template exists but was never modified by user - update it
                existing.Update(
                    definition.Subject,
                    definition.HtmlBody,
                    definition.PlainTextBody,
                    definition.Description,
                    definition.AvailableVariables);

                // Reset version back to 1 since this is a seed update, not a user update
                existing.ResetVersionForSeeding();
                updatedCount++;
            }
            else
            {
                // Template was customized by user (Version > 1) - skip it
                skippedCount++;
                context.Logger.LogDebug(
                    "Skipping email template '{TemplateName}' - user customized (Version={Version})",
                    existing.Name, existing.Version);
            }
        }

        if (addedCount > 0 || updatedCount > 0)
        {
            await context.DbContext.SaveChangesAsync(ct);
            context.Logger.LogInformation(
                "Platform email templates: {Added} added, {Updated} updated, {Skipped} skipped (customized)",
                addedCount, updatedCount, skippedCount);
        }
    }

    private static List<EmailTemplate> GetEmailTemplateDefinitions()
    {
        var templates = new List<EmailTemplate>();

        // Password Reset OTP
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "PasswordResetOtp",
            subject: "Password Reset Code: {{OtpCode}}",
            htmlBody: GetPasswordResetOtpHtmlBody(),
            plainTextBody: GetPasswordResetOtpPlainTextBody(),
            description: "Email sent when user requests password reset with OTP code.",
            availableVariables: "[\"UserName\", \"OtpCode\", \"ExpiryMinutes\"]"));

        // Email Change OTP
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "EmailChangeOtp",
            subject: "Email Change Verification Code: {{OtpCode}}",
            htmlBody: GetEmailChangeOtpHtmlBody(),
            plainTextBody: GetEmailChangeOtpPlainTextBody(),
            description: "Email sent when user requests to change their email address with OTP code.",
            availableVariables: "[\"UserName\", \"OtpCode\", \"ExpiryMinutes\"]"));

        // Welcome Email (used when admin creates user)
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "WelcomeEmail",
            subject: "Welcome to NOIR - Your Account Has Been Created",
            htmlBody: GetWelcomeEmailHtmlBody(),
            plainTextBody: GetWelcomeEmailPlainTextBody(),
            description: "Email sent to users when their account is created by an administrator.",
            availableVariables: "[\"UserName\", \"Email\", \"TemporaryPassword\", \"LoginUrl\", \"ApplicationName\"]"));

        return templates;
    }

    #region Email Template Content

    private static string GetPasswordResetOtpHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Password Reset</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">NOIR</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{UserName}},</h2>
                <p>You have requested to reset your password. Use the OTP code below to continue:</p>
                <div style="background: #1e40af; color: white; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 8px; border-radius: 8px; margin: 20px 0;">
                    {{OtpCode}}
                </div>
                <p style="color: #6b7280; font-size: 14px;">This code will expire in <strong>{{ExpiryMinutes}} minutes</strong>.</p>
                <p style="color: #6b7280; font-size: 14px;">If you did not request a password reset, please ignore this email.</p>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 NOIR. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetPasswordResetOtpPlainTextBody() => """
        NOIR - Password Reset

        Hello {{UserName}},

        You have requested to reset your password. Use the OTP code below:

        OTP Code: {{OtpCode}}

        This code will expire in {{ExpiryMinutes}} minutes.

        If you did not request a password reset, please ignore this email.

        © 2024 NOIR
        """;

    private static string GetEmailChangeOtpHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Email Change Verification</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">NOIR</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{UserName}},</h2>
                <p>You have requested to change your email address. Use the OTP code below to verify your new email:</p>
                <div style="background: #1e40af; color: white; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 8px; border-radius: 8px; margin: 20px 0;">
                    {{OtpCode}}
                </div>
                <p style="color: #6b7280; font-size: 14px;">This code will expire in <strong>{{ExpiryMinutes}} minutes</strong>.</p>
                <p style="color: #6b7280; font-size: 14px;">If you did not request an email change, please ignore this email and your email address will remain unchanged.</p>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 NOIR. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetEmailChangeOtpPlainTextBody() => """
        NOIR - Email Change Verification

        Hello {{UserName}},

        You have requested to change your email address. Use the OTP code below to verify your new email:

        OTP Code: {{OtpCode}}

        This code will expire in {{ExpiryMinutes}} minutes.

        If you did not request an email change, please ignore this email and your email address will remain unchanged.

        © 2024 NOIR
        """;

    private static string GetWelcomeEmailHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Welcome to NOIR</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">NOIR</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Welcome, {{UserName}}!</h2>
                <p>An administrator has created an account for you in <strong>{{ApplicationName}}</strong>.</p>
                <p>Here are your login credentials:</p>
                <div style="background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="margin: 5px 0;"><strong>Email:</strong> {{Email}}</p>
                </div>
                <p style="margin-bottom: 5px;"><strong>Your temporary password:</strong></p>
                <div style="background: #1e40af; color: white; padding: 20px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 4px; border-radius: 8px; margin: 10px 0 20px 0; font-family: monospace;">
                    {{TemporaryPassword}}
                </div>
                <div style="background: #fef3c7; border-left: 4px solid #f59e0b; padding: 12px 15px; margin: 20px 0; border-radius: 0 8px 8px 0;">
                    <p style="margin: 0; color: #92400e; font-size: 14px;"><strong>⚠ Important:</strong> Please change your password immediately after your first login.</p>
                </div>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{LoginUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">Log In Now</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{ApplicationName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetWelcomeEmailPlainTextBody() => """
        {{ApplicationName}} - Welcome!

        Hello {{UserName}},

        An administrator has created an account for you in {{ApplicationName}}.

        Email: {{Email}}
        Temporary Password: {{TemporaryPassword}}

        ⚠️ IMPORTANT: Please change your password immediately after your first login.

        Log in at: {{LoginUrl}}

        If you have any questions, please contact your administrator.

        © 2024 {{ApplicationName}}
        """;

    #endregion
}
