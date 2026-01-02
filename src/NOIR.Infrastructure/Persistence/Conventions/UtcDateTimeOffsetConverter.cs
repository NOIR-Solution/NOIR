namespace NOIR.Infrastructure.Persistence.Conventions;

/// <summary>
/// Value converter for DateTimeOffset that ensures UTC storage.
/// </summary>
public class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(
            v => v.ToUniversalTime(), // To database
            v => v.ToUniversalTime()) // From database
    {
    }
}

/// <summary>
/// Value converter for nullable DateTimeOffset that ensures UTC storage.
/// </summary>
public class NullableUtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset?, DateTimeOffset?>
{
    public NullableUtcDateTimeOffsetConverter()
        : base(
            v => v.HasValue ? v.Value.ToUniversalTime() : null, // To database
            v => v.HasValue ? v.Value.ToUniversalTime() : null) // From database
    {
    }
}
