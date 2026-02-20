namespace NOIR.Domain.Entities.Common;

/// <summary>
/// Atomic sequence counter for generating unique sequential numbers.
/// Used for order numbers, transaction numbers, etc.
/// Each row represents a unique prefix + tenant combination.
/// </summary>
public class SequenceCounter
{
    public Guid Id { get; private set; }
    public string? TenantId { get; private set; }
    public string Prefix { get; private set; } = string.Empty;
    public int CurrentValue { get; private set; }

    private SequenceCounter() { }

    public static SequenceCounter Create(string prefix, string? tenantId)
    {
        return new SequenceCounter
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Prefix = prefix,
            CurrentValue = 0
        };
    }
}
