using NOIR.Infrastructure.Persistence;

namespace NOIR.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire filter that logs job failures and can send notifications.
/// Applied globally to all Hangfire jobs for improved observability.
/// Uses IServiceProvider to resolve scoped IFluentEmail service properly.
/// </summary>
public class JobFailureNotificationFilter : JobFilterAttribute, IElectStateFilter
{
    private readonly ILogger<JobFailureNotificationFilter> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<JobNotificationSettings> _settings;

    /// <summary>
    /// Creates a new instance of the filter.
    /// Note: This constructor is used when the filter is instantiated via DI.
    /// IServiceProvider is used to resolve scoped services (like IFluentEmail) at runtime.
    /// </summary>
    public JobFailureNotificationFilter(
        ILogger<JobFailureNotificationFilter> logger,
        IOptions<JobNotificationSettings> settings,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _settings = settings;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Called when a job's state is about to change.
    /// We intercept transitions to FailedState to log, create audit entry, and optionally notify.
    /// </summary>
    public void OnStateElection(ElectStateContext context)
    {
        // Only act on transitions to FailedState
        if (context.CandidateState is not FailedState failedState)
            return;

        var jobId = context.BackgroundJob.Id;
        var jobType = context.BackgroundJob.Job?.Type?.Name ?? "Unknown";
        var jobMethod = context.BackgroundJob.Job?.Method?.Name ?? "Unknown";
        var exception = failedState.Exception;

        // Always log the failure with structured logging
        _logger.LogError(
            exception,
            "Hangfire job failed - JobId: {JobId}, Type: {JobType}, Method: {JobMethod}, Reason: {Reason}",
            jobId,
            jobType,
            jobMethod,
            failedState.Reason);

        // Create audit log entry for the failure so it appears in Activity Timeline
        CreateBackgroundJobAuditLog(jobId, jobType, jobMethod, exception);

        // Send email notification if enabled
        if (_settings.Value.SendEmailOnFailure)
        {
            // Use Task.Run with proper error handling for fire-and-forget async
            // This ensures email sending doesn't block job state transitions
            Task.Run(async () =>
            {
                try
                {
                    await SendEmailNotificationAsync(jobId, jobType, jobMethod, exception);
                }
                catch (Exception ex)
                {
                    // Safety net - should never hit this since SendEmailNotificationAsync has try-catch
                    _logger.LogError(ex, "Unexpected error in background email notification for JobId: {JobId}", jobId);
                }
            });
        }
    }

    /// <summary>
    /// Creates an audit log entry for the background job failure.
    /// This makes job failures visible in the Activity Timeline.
    /// </summary>
    private void CreateBackgroundJobAuditLog(
        string jobId,
        string jobType,
        string jobMethod,
        Exception? exception)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var correlationId = $"hangfire-{jobId}";
            var errorMessage = exception is not null
                ? $"{exception.GetType().Name}: {exception.Message}"
                : "Job failed";

            // Truncate error message to prevent database issues
            if (errorMessage.Length > 2000)
            {
                errorMessage = errorMessage[..2000] + "... [TRUNCATED]";
            }

            var auditLog = HandlerAuditLog.Create(
                correlationId: correlationId,
                handlerName: $"{jobType}.{jobMethod}",
                operationType: AuditOperationType.Update, // Background jobs are typically data processing
                tenantId: null, // Background jobs may not have tenant context
                httpRequestAuditLogId: null, // No HTTP request for background jobs
                pageContext: "Background Jobs");

            auditLog.Complete(isSuccess: false, errorMessage: errorMessage);
            auditLog.SetActivityContext(
                displayName: $"Background Job {jobId}",
                actionDescription: $"Background job '{jobType}.{jobMethod}' failed");

            dbContext.HandlerAuditLogs.Add(auditLog);
            dbContext.SaveChanges(); // Sync since we're in Hangfire filter context
        }
        catch (Exception ex)
        {
            // Don't let audit failures break the job pipeline
            _logger.LogWarning(ex, "Failed to create audit log for job failure: {JobId}", jobId);
        }
    }

    private async Task SendEmailNotificationAsync(
        string jobId,
        string jobType,
        string jobMethod,
        Exception? exception)
    {
        if (string.IsNullOrEmpty(_settings.Value.NotificationEmail))
        {
            _logger.LogWarning("Job failure notification email not configured");
            return;
        }

        try
        {
            // Create a scope to resolve scoped services (IFluentEmail is registered as Scoped)
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetService<IFluentEmail>();

            if (emailService == null)
            {
                _logger.LogWarning("IFluentEmail service not available for job failure notification");
                return;
            }

            await emailService
                .To(_settings.Value.NotificationEmail)
                .Subject($"[NOIR] Background Job Failed: {jobType}.{jobMethod}")
                .Body($"""
                    A background job has failed in the NOIR application.

                    Job Details:
                    - Job ID: {jobId}
                    - Type: {jobType}
                    - Method: {jobMethod}

                    Error Details:
                    - Exception Type: {exception?.GetType().Name ?? "Unknown"}
                    - Message: {exception?.Message ?? "No message"}

                    Stack Trace:
                    {exception?.StackTrace ?? "No stack trace available"}

                    This job may be retried automatically. Check the Hangfire dashboard for current status.
                    """)
                .SendAsync();

            _logger.LogInformation("Job failure notification email sent for JobId: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            // Don't let email failures break the job pipeline
            _logger.LogWarning(ex, "Failed to send job failure notification email for JobId: {JobId}", jobId);
        }
    }
}

/// <summary>
/// Configuration settings for job failure notifications.
/// </summary>
public class JobNotificationSettings
{
    /// <summary>
    /// Configuration section name for binding.
    /// </summary>
    public const string SectionName = "JobNotifications";

    /// <summary>
    /// Whether to send email notifications on job failures.
    /// Default: false
    /// </summary>
    public bool SendEmailOnFailure { get; set; }

    /// <summary>
    /// Email address to send failure notifications to.
    /// </summary>
    public string? NotificationEmail { get; set; }
}
