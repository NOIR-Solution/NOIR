using NOIR.Application.Features.EmailTemplates.Commands.RevertToPlatformDefault;
using NOIR.Application.Features.EmailTemplates.Commands.ToggleEmailTemplateActive;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Email template management API endpoints.
/// Admin-only endpoints for viewing and editing email templates.
/// </summary>
public static class EmailTemplateEndpoints
{
    public static void MapEmailTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/email-templates")
            .WithTags("Email Templates")
            .RequireAuthorization();

        // Get all email templates
        group.MapGet("/", async (
            [FromQuery] string? search,
            IMessageBus bus) =>
        {
            var query = new GetEmailTemplatesQuery(search);
            var result = await bus.InvokeAsync<Result<List<EmailTemplateListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.EmailTemplatesRead)
        .WithName("GetEmailTemplates")
        .WithSummary("Get list of email templates")
        .WithDescription("Returns all email templates with optional search filtering.")
        .Produces<List<EmailTemplateListDto>>(StatusCodes.Status200OK);

        // Get single email template by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetEmailTemplateQuery(id);
            var result = await bus.InvokeAsync<Result<EmailTemplateDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.EmailTemplatesRead)
        .WithName("GetEmailTemplate")
        .WithSummary("Get email template by ID")
        .WithDescription("Returns full email template details including HTML body.")
        .Produces<EmailTemplateDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update email template
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateEmailTemplateRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateEmailTemplateCommand(
                id,
                request.Subject,
                request.HtmlBody,
                request.PlainTextBody,
                request.Description)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<EmailTemplateDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.EmailTemplatesUpdate)
        .WithName("UpdateEmailTemplate")
        .WithSummary("Update email template content")
        .WithDescription("Updates the subject, HTML body, plain text body, and description of an email template.")
        .Produces<EmailTemplateDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Send test email
        group.MapPost("/{id:guid}/test", async (
            Guid id,
            SendTestEmailRequest request,
            IMessageBus bus) =>
        {
            var command = new SendTestEmailCommand(id, request.RecipientEmail, request.SampleData);
            var result = await bus.InvokeAsync<Result<EmailPreviewResponse>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.EmailTemplatesUpdate)
        .WithName("SendTestEmail")
        .WithSummary("Send a test email")
        .WithDescription("Sends a test email using the template with provided sample data.")
        .Produces<EmailPreviewResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Preview email template with sample data
        group.MapPost("/{id:guid}/preview", async (
            Guid id,
            PreviewEmailTemplateRequest request,
            IMessageBus bus) =>
        {
            // Get the template
            var queryResult = await bus.InvokeAsync<Result<EmailTemplateDto>>(new GetEmailTemplateQuery(id));
            if (!queryResult.IsSuccess)
            {
                return queryResult.ToHttpResult();
            }

            var template = queryResult.Value!;

            // Replace variables
            var subject = ReplaceVariables(template.Subject, request.SampleData);
            var htmlBody = ReplaceVariables(template.HtmlBody, request.SampleData);
            var plainTextBody = template.PlainTextBody is not null
                ? ReplaceVariables(template.PlainTextBody, request.SampleData)
                : null;

            var preview = new EmailPreviewResponse(subject, htmlBody, plainTextBody);
            return Results.Ok(preview);
        })
        .RequireAuthorization(Permissions.EmailTemplatesRead)
        .WithName("PreviewEmailTemplate")
        .WithSummary("Preview email template")
        .WithDescription("Returns the rendered email template with sample data applied.")
        .Produces<EmailPreviewResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Revert to platform default
        group.MapDelete("/{id:guid}/revert", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RevertToPlatformDefaultCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<EmailTemplateDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.EmailTemplatesUpdate)
        .WithName("RevertToPlatformDefault")
        .WithSummary("Revert to platform default template")
        .WithDescription("Deletes the tenant's customized version and reverts to the platform default template. Only available for tenant users.")
        .Produces<EmailTemplateDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Toggle email template active/inactive
        group.MapPatch("/{id:guid}/toggle-active", async (
            Guid id,
            ToggleActiveRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ToggleEmailTemplateActiveCommand(id, request.IsActive)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<EmailTemplateDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.EmailTemplatesUpdate)
        .WithName("ToggleEmailTemplateActive")
        .WithSummary("Toggle email template active status")
        .WithDescription("Activates or deactivates an email template. Uses Copy-on-Write for platform templates.")
        .Produces<EmailTemplateDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static string ReplaceVariables(string content, Dictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
        {
            // Support both {{Variable}} and {Variable} formats
            content = content.Replace($"{{{{{key}}}}}", value);
            content = content.Replace($"{{{key}}}", value);
        }
        return content;
    }
}
