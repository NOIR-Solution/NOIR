namespace NOIR.Infrastructure.Services;

/// <summary>
/// Hangfire implementation of background job service.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Thin wrapper around Hangfire static methods - tested via integration")]
public class BackgroundJobsService : IBackgroundJobs, IScopedService
{
    public string Enqueue(Expression<Action> methodCall)
        => BackgroundJob.Enqueue(methodCall);

    public string Enqueue(Expression<Func<Task>> methodCall)
        => BackgroundJob.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Action<T>> methodCall)
        => BackgroundJob.Enqueue(methodCall);

    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
        => BackgroundJob.Enqueue(methodCall);

    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
        => BackgroundJob.Schedule(methodCall, delay);

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
        => BackgroundJob.Schedule(methodCall, delay);

    public string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt)
        => BackgroundJob.Schedule(methodCall, enqueueAt);

    public string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt)
        => BackgroundJob.Schedule(methodCall, enqueueAt);

    public void RecurringJob(string jobId, Expression<Action> methodCall, string cronExpression)
        => Hangfire.RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);

    public void RecurringJob(string jobId, Expression<Func<Task>> methodCall, string cronExpression)
        => Hangfire.RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);

    public void RecurringJob<T>(string jobId, Expression<Action<T>> methodCall, string cronExpression)
        => Hangfire.RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);

    public void RecurringJob<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression)
        => Hangfire.RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);

    public void RemoveRecurringJob(string jobId)
        => Hangfire.RecurringJob.RemoveIfExists(jobId);

    public string ContinueWith(string parentJobId, Expression<Action> methodCall)
        => BackgroundJob.ContinueJobWith(parentJobId, methodCall);

    public string ContinueWith(string parentJobId, Expression<Func<Task>> methodCall)
        => BackgroundJob.ContinueJobWith(parentJobId, methodCall);
}
