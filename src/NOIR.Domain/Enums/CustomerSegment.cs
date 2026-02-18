namespace NOIR.Domain.Enums;

/// <summary>
/// Customer segmentation based on RFM (Recency, Frequency, Monetary) analysis.
/// </summary>
public enum CustomerSegment
{
    /// <summary>Customer with 0-1 orders.</summary>
    New = 0,

    /// <summary>Customer who ordered in the last 30 days.</summary>
    Active = 1,

    /// <summary>Customer whose last order was 31-90 days ago.</summary>
    AtRisk = 2,

    /// <summary>Customer whose last order was 91-180 days ago.</summary>
    Dormant = 3,

    /// <summary>Customer whose last order was more than 180 days ago or never ordered.</summary>
    Lost = 4,

    /// <summary>High-value customer with 20+ orders and 10,000,000+ VND spent.</summary>
    VIP = 5
}
