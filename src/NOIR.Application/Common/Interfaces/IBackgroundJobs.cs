namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for scheduling and managing background jobs.
/// </summary>
public interface IBackgroundJobs
{
    /// <summary>
    /// Enqueues a job to be executed immediately.
    /// </summary>
    string Enqueue(Expression<Action> methodCall);

    /// <summary>
    /// Enqueues a job to be executed immediately.
    /// </summary>
    string Enqueue(Expression<Func<Task>> methodCall);

    /// <summary>
    /// Enqueues a job to be executed immediately with a specific type.
    /// </summary>
    string Enqueue<T>(Expression<Action<T>> methodCall);

    /// <summary>
    /// Enqueues a job to be executed immediately with a specific type.
    /// </summary>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);

    /// <summary>
    /// Schedules a job to be executed at a specific time.
    /// </summary>
    string Schedule(Expression<Action> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules a job to be executed at a specific time.
    /// </summary>
    string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);

    /// <summary>
    /// Schedules a job to be executed at a specific time.
    /// </summary>
    string Schedule(Expression<Action> methodCall, DateTimeOffset enqueueAt);

    /// <summary>
    /// Schedules a job to be executed at a specific time.
    /// </summary>
    string Schedule(Expression<Func<Task>> methodCall, DateTimeOffset enqueueAt);

    /// <summary>
    /// Creates or updates a recurring job with a CRON expression.
    /// </summary>
    void RecurringJob(string jobId, Expression<Action> methodCall, string cronExpression);

    /// <summary>
    /// Creates or updates a recurring job with a CRON expression.
    /// </summary>
    void RecurringJob(string jobId, Expression<Func<Task>> methodCall, string cronExpression);

    /// <summary>
    /// Creates or updates a recurring job with a CRON expression.
    /// </summary>
    void RecurringJob<T>(string jobId, Expression<Action<T>> methodCall, string cronExpression);

    /// <summary>
    /// Creates or updates a recurring job with a CRON expression.
    /// </summary>
    void RecurringJob<T>(string jobId, Expression<Func<T, Task>> methodCall, string cronExpression);

    /// <summary>
    /// Removes a recurring job.
    /// </summary>
    void RemoveRecurringJob(string jobId);

    /// <summary>
    /// Continues with another job after the specified job completes.
    /// </summary>
    string ContinueWith(string parentJobId, Expression<Action> methodCall);

    /// <summary>
    /// Continues with another job after the specified job completes.
    /// </summary>
    string ContinueWith(string parentJobId, Expression<Func<Task>> methodCall);
}
