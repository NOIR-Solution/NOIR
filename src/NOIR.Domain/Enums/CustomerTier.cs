namespace NOIR.Domain.Enums;

/// <summary>
/// Customer loyalty tier based on lifetime loyalty points.
/// </summary>
public enum CustomerTier
{
    /// <summary>Below 5,000 lifetime points.</summary>
    Standard = 0,

    /// <summary>5,000+ lifetime points.</summary>
    Silver = 1,

    /// <summary>10,000+ lifetime points.</summary>
    Gold = 2,

    /// <summary>20,000+ lifetime points.</summary>
    Platinum = 3,

    /// <summary>50,000+ lifetime points.</summary>
    Diamond = 4
}
