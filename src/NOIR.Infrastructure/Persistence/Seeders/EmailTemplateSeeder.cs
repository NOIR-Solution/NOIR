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

        // Order Confirmation
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "order_confirmation",
            subject: "Your order {{OrderNumber}} has been confirmed",
            htmlBody: GetOrderConfirmationHtmlBody(),
            plainTextBody: GetOrderConfirmationPlainTextBody(),
            description: "Email sent to customers when their order is confirmed.",
            availableVariables: "[\"CustomerName\", \"OrderNumber\", \"OrderDate\", \"OrderTotal\", \"OrderDetailsUrl\", \"StoreName\"]"));

        // Order Shipped
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "order_shipped",
            subject: "Your order {{OrderNumber}} has been shipped",
            htmlBody: GetOrderShippedHtmlBody(),
            plainTextBody: GetOrderShippedPlainTextBody(),
            description: "Email sent to customers when their order has been shipped.",
            availableVariables: "[\"CustomerName\", \"OrderNumber\", \"TrackingNumber\", \"CarrierName\", \"EstimatedDelivery\", \"TrackingUrl\", \"StoreName\"]"));

        // Order Delivered
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "order_delivered",
            subject: "Your order {{OrderNumber}} has been delivered",
            htmlBody: GetOrderDeliveredHtmlBody(),
            plainTextBody: GetOrderDeliveredPlainTextBody(),
            description: "Email sent to customers when their order has been delivered.",
            availableVariables: "[\"CustomerName\", \"OrderNumber\", \"DeliveredDate\", \"OrderDetailsUrl\", \"ReviewUrl\", \"StoreName\"]"));

        // Order Cancelled
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "order_cancelled",
            subject: "Your order {{OrderNumber}} has been cancelled",
            htmlBody: GetOrderCancelledHtmlBody(),
            plainTextBody: GetOrderCancelledPlainTextBody(),
            description: "Email sent to customers when their order has been cancelled.",
            availableVariables: "[\"CustomerName\", \"OrderNumber\", \"CancellationReason\", \"RefundAmount\", \"SupportUrl\", \"StoreName\"]"));

        // Order Refunded
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "order_refunded",
            subject: "Your order {{OrderNumber}} has been refunded",
            htmlBody: GetOrderRefundedHtmlBody(),
            plainTextBody: GetOrderRefundedPlainTextBody(),
            description: "Email sent to customers when a refund has been processed for their order.",
            availableVariables: "[\"CustomerName\", \"OrderNumber\", \"RefundAmount\", \"RefundMethod\", \"RefundBusinessDays\", \"SupportUrl\", \"StoreName\"]"));

        // Review Approved
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "review_approved",
            subject: "Your review has been approved",
            htmlBody: GetReviewApprovedHtmlBody(),
            plainTextBody: GetReviewApprovedPlainTextBody(),
            description: "Email sent to customers when their product review has been approved.",
            availableVariables: "[\"CustomerName\", \"ProductName\", \"ReviewTitle\", \"ProductUrl\", \"StoreName\"]"));

        // Review Rejected
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "review_rejected",
            subject: "Your review needs attention",
            htmlBody: GetReviewRejectedHtmlBody(),
            plainTextBody: GetReviewRejectedPlainTextBody(),
            description: "Email sent to customers when their product review has been rejected.",
            availableVariables: "[\"CustomerName\", \"ProductName\", \"RejectionReason\", \"SupportUrl\", \"StoreName\"]"));

        // Customer Welcome
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "customer_welcome",
            subject: "Welcome to {{StoreName}}!",
            htmlBody: GetCustomerWelcomeHtmlBody(),
            plainTextBody: GetCustomerWelcomePlainTextBody(),
            description: "Email sent to new customers when they register a customer account.",
            availableVariables: "[\"CustomerName\", \"StoreName\", \"ShopUrl\", \"SupportUrl\"]"));

        // Customer Tier Upgrade
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "customer_tier_upgrade",
            subject: "Congratulations! You've been upgraded to {{NewTier}}",
            htmlBody: GetCustomerTierUpgradeHtmlBody(),
            plainTextBody: GetCustomerTierUpgradePlainTextBody(),
            description: "Email sent to customers when they are upgraded to a higher loyalty tier.",
            availableVariables: "[\"CustomerName\", \"OldTier\", \"NewTier\", \"TierBenefits\", \"ShopUrl\", \"StoreName\"]"));

        // Cart Abandoned Recovery
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "cart_abandoned_recovery",
            subject: "You left something behind!",
            htmlBody: GetCartAbandonedRecoveryHtmlBody(),
            plainTextBody: GetCartAbandonedRecoveryPlainTextBody(),
            description: "Email sent to customers who have abandoned their shopping cart.",
            availableVariables: "[\"CustomerName\", \"CartItems\", \"CartTotal\", \"RecoveryUrl\", \"ExpiryHours\", \"StoreName\"]"));

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

    private static string GetOrderConfirmationHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Order Confirmation</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">{{StoreName}}</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{CustomerName}},</h2>
                <p>Thank you for your order! We have received your order and it is being processed.</p>
                <div style="background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="margin: 5px 0;"><strong>Order Number:</strong> {{OrderNumber}}</p>
                    <p style="margin: 5px 0;"><strong>Order Date:</strong> {{OrderDate}}</p>
                    <p style="margin: 5px 0;"><strong>Order Total:</strong> {{OrderTotal}}</p>
                </div>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{OrderDetailsUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">View Order Details</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{StoreName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetOrderConfirmationPlainTextBody() => """
        Order Confirmed - {{StoreName}}

        Hello {{CustomerName}},

        Thank you for your order! We have received your order and it is being processed.

        Order Number: {{OrderNumber}}
        Order Date: {{OrderDate}}
        Order Total: {{OrderTotal}}

        View your order details at: {{OrderDetailsUrl}}

        © 2024 {{StoreName}}
        """;

    private static string GetOrderShippedHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Order Shipped</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">{{StoreName}}</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{CustomerName}},</h2>
                <p>Great news! Your order is on its way.</p>
                <div style="background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="margin: 5px 0;"><strong>Order Number:</strong> {{OrderNumber}}</p>
                    <p style="margin: 5px 0;"><strong>Carrier:</strong> {{CarrierName}}</p>
                    <p style="margin: 5px 0;"><strong>Tracking Number:</strong> {{TrackingNumber}}</p>
                    <p style="margin: 5px 0;"><strong>Estimated Delivery:</strong> {{EstimatedDelivery}}</p>
                </div>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{TrackingUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">Track Your Package</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{StoreName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetOrderShippedPlainTextBody() => """
        Order Shipped - {{StoreName}}

        Hello {{CustomerName}},

        Great news! Your order is on its way.

        Order Number: {{OrderNumber}}
        Carrier: {{CarrierName}}
        Tracking Number: {{TrackingNumber}}
        Estimated Delivery: {{EstimatedDelivery}}

        Track your package at: {{TrackingUrl}}

        © 2024 {{StoreName}}
        """;

    private static string GetOrderDeliveredHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Order Delivered</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">{{StoreName}}</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{CustomerName}},</h2>
                <p>Your order has been delivered! We hope you enjoy your purchase.</p>
                <div style="background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="margin: 5px 0;"><strong>Order Number:</strong> {{OrderNumber}}</p>
                    <p style="margin: 5px 0;"><strong>Delivered On:</strong> {{DeliveredDate}}</p>
                </div>
                <p>Happy with your order? Share your experience by leaving a review!</p>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{ReviewUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">Leave a Review</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{StoreName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetOrderDeliveredPlainTextBody() => """
        Order Delivered - {{StoreName}}

        Hello {{CustomerName}},

        Your order has been delivered! We hope you enjoy your purchase.

        Order Number: {{OrderNumber}}
        Delivered On: {{DeliveredDate}}

        Happy with your order? Leave a review at: {{ReviewUrl}}

        View your order details at: {{OrderDetailsUrl}}

        © 2024 {{StoreName}}
        """;

    private static string GetOrderCancelledHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Order Cancelled</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">{{StoreName}}</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{CustomerName}},</h2>
                <p>We're sorry to inform you that your order has been cancelled.</p>
                <div style="background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="margin: 5px 0;"><strong>Order Number:</strong> {{OrderNumber}}</p>
                    <p style="margin: 5px 0;"><strong>Cancellation Reason:</strong> {{CancellationReason}}</p>
                    <p style="margin: 5px 0;"><strong>Refund Amount:</strong> {{RefundAmount}}</p>
                </div>
                <p>If you have any questions, please contact our support team.</p>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{SupportUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">Contact Support</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{StoreName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetOrderCancelledPlainTextBody() => """
        Order Cancelled - {{StoreName}}

        Hello {{CustomerName}},

        We're sorry to inform you that your order has been cancelled.

        Order Number: {{OrderNumber}}
        Cancellation Reason: {{CancellationReason}}
        Refund Amount: {{RefundAmount}}

        If you have any questions, please contact our support team at: {{SupportUrl}}

        © 2024 {{StoreName}}
        """;

    private static string GetOrderRefundedHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Order Refunded</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">{{StoreName}}</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{CustomerName}},</h2>
                <p>Your refund has been processed successfully.</p>
                <div style="background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="margin: 5px 0;"><strong>Order Number:</strong> {{OrderNumber}}</p>
                    <p style="margin: 5px 0;"><strong>Refund Amount:</strong> {{RefundAmount}}</p>
                    <p style="margin: 5px 0;"><strong>Refund Method:</strong> {{RefundMethod}}</p>
                    <p style="margin: 5px 0;"><strong>Processing Time:</strong> {{RefundBusinessDays}} business days</p>
                </div>
                <p>If you have any questions about your refund, please contact our support team.</p>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{SupportUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">Contact Support</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{StoreName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetOrderRefundedPlainTextBody() => """
        Order Refunded - {{StoreName}}

        Hello {{CustomerName}},

        Your refund has been processed successfully.

        Order Number: {{OrderNumber}}
        Refund Amount: {{RefundAmount}}
        Refund Method: {{RefundMethod}}
        Processing Time: {{RefundBusinessDays}} business days

        If you have any questions, please contact our support team at: {{SupportUrl}}

        © 2024 {{StoreName}}
        """;

    private static string GetReviewApprovedHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Review Approved</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">{{StoreName}}</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{CustomerName}},</h2>
                <p>Your review has been approved and is now live on our website. Thank you for sharing your feedback!</p>
                <div style="background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="margin: 5px 0;"><strong>Product:</strong> {{ProductName}}</p>
                    <p style="margin: 5px 0;"><strong>Review Title:</strong> {{ReviewTitle}}</p>
                </div>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{ProductUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">View Product</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{StoreName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetReviewApprovedPlainTextBody() => """
        Review Approved - {{StoreName}}

        Hello {{CustomerName}},

        Your review has been approved and is now live on our website. Thank you for sharing your feedback!

        Product: {{ProductName}}
        Review Title: {{ReviewTitle}}

        View the product at: {{ProductUrl}}

        © 2024 {{StoreName}}
        """;

    private static string GetReviewRejectedHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Review Needs Attention</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">{{StoreName}}</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{CustomerName}},</h2>
                <p>Unfortunately, your review for <strong>{{ProductName}}</strong> could not be published at this time.</p>
                <div style="background: #fef3c7; border-left: 4px solid #f59e0b; padding: 12px 15px; margin: 20px 0; border-radius: 0 8px 8px 0;">
                    <p style="margin: 0; color: #92400e; font-size: 14px;"><strong>Reason:</strong> {{RejectionReason}}</p>
                </div>
                <p>If you believe this decision was made in error or have questions, please contact our support team.</p>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{SupportUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">Contact Support</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{StoreName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetReviewRejectedPlainTextBody() => """
        Review Needs Attention - {{StoreName}}

        Hello {{CustomerName}},

        Unfortunately, your review for {{ProductName}} could not be published at this time.

        Reason: {{RejectionReason}}

        If you have questions, please contact our support team at: {{SupportUrl}}

        © 2024 {{StoreName}}
        """;

    private static string GetCustomerWelcomeHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Welcome!</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">{{StoreName}}</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Welcome, {{CustomerName}}!</h2>
                <p>Thank you for joining <strong>{{StoreName}}</strong>! We're excited to have you as a customer.</p>
                <p>With your new account you can:</p>
                <ul style="color: #374151;">
                    <li>Track your orders in real time</li>
                    <li>Save items to your wishlist</li>
                    <li>Leave reviews for products you've purchased</li>
                    <li>Earn loyalty rewards with every purchase</li>
                </ul>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{ShopUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">Start Shopping</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{StoreName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetCustomerWelcomePlainTextBody() => """
        Welcome to {{StoreName}}!

        Hello {{CustomerName}},

        Thank you for joining {{StoreName}}! We're excited to have you as a customer.

        With your new account you can:
        - Track your orders in real time
        - Save items to your wishlist
        - Leave reviews for products you've purchased
        - Earn loyalty rewards with every purchase

        Start shopping at: {{ShopUrl}}

        If you need any assistance, contact us at: {{SupportUrl}}

        © 2024 {{StoreName}}
        """;

    private static string GetCustomerTierUpgradeHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Loyalty Tier Upgrade</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">{{StoreName}}</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Congratulations, {{CustomerName}}!</h2>
                <p>Your loyalty has been recognized! You have been upgraded from <strong>{{OldTier}}</strong> to <strong>{{NewTier}}</strong>.</p>
                <div style="background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="margin: 5px 0 10px 0;"><strong>Your {{NewTier}} benefits:</strong></p>
                    <p style="margin: 5px 0; white-space: pre-line;">{{TierBenefits}}</p>
                </div>
                <p>Keep shopping to maintain and grow your tier status!</p>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{ShopUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">Shop Now</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{StoreName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetCustomerTierUpgradePlainTextBody() => """
        Loyalty Tier Upgrade - {{StoreName}}

        Congratulations, {{CustomerName}}!

        Your loyalty has been recognized! You have been upgraded from {{OldTier}} to {{NewTier}}.

        Your {{NewTier}} benefits:
        {{TierBenefits}}

        Keep shopping to maintain and grow your tier status!

        Shop now at: {{ShopUrl}}

        © 2024 {{StoreName}}
        """;

    private static string GetCartAbandonedRecoveryHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>You Left Something Behind!</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">{{StoreName}}</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{CustomerName}},</h2>
                <p>You left some items in your cart! Don't let them get away.</p>
                <div style="background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="margin: 5px 0; white-space: pre-line;">{{CartItems}}</p>
                    <p style="margin: 10px 0 5px 0;"><strong>Cart Total:</strong> {{CartTotal}}</p>
                </div>
                <p style="color: #6b7280; font-size: 14px;">Your cart will be saved for <strong>{{ExpiryHours}} hours</strong>.</p>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{RecoveryUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">Complete Your Purchase</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{StoreName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetCartAbandonedRecoveryPlainTextBody() => """
        You Left Something Behind! - {{StoreName}}

        Hello {{CustomerName}},

        You left some items in your cart! Don't let them get away.

        Items in your cart:
        {{CartItems}}

        Cart Total: {{CartTotal}}

        Your cart will be saved for {{ExpiryHours}} hours.

        Complete your purchase at: {{RecoveryUrl}}

        © 2024 {{StoreName}}
        """;

    #endregion
}
