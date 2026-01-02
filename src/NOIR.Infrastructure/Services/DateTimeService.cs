namespace NOIR.Infrastructure.Services;

/// <summary>
/// Implementation of IDateTime that returns actual system time.
/// Uses DateTimeOffset for timezone-aware timestamps.
/// </summary>
public class DateTimeService : IDateTime, IScopedService
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
