namespace NOIR.Domain.Entities.Customer;

/// <summary>
/// Represents a customer in the e-commerce platform.
/// Tracks RFM metrics, loyalty points, segments, and tiers.
/// </summary>
public class Customer : TenantAggregateRoot<Guid>
{
    private Customer() : base() { }
    private Customer(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Reference to the ApplicationUser (optional - customers can exist without user accounts).
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// Customer email address.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Customer first name.
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// Customer last name.
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Customer phone number.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Current customer segment based on RFM analysis.
    /// </summary>
    public CustomerSegment Segment { get; private set; } = CustomerSegment.New;

    /// <summary>
    /// Current customer loyalty tier.
    /// </summary>
    public CustomerTier Tier { get; private set; } = CustomerTier.Standard;

    // RFM (Recency, Frequency, Monetary) metrics
    /// <summary>
    /// Date of the customer's most recent order.
    /// </summary>
    public DateTimeOffset? LastOrderDate { get; private set; }

    /// <summary>
    /// Total number of orders placed.
    /// </summary>
    public int TotalOrders { get; private set; }

    /// <summary>
    /// Total amount spent across all orders (VND).
    /// </summary>
    public decimal TotalSpent { get; private set; }

    /// <summary>
    /// Average order value (VND).
    /// </summary>
    public decimal AverageOrderValue { get; private set; }

    // Loyalty
    /// <summary>
    /// Current available loyalty points.
    /// </summary>
    public int LoyaltyPoints { get; private set; }

    /// <summary>
    /// Total loyalty points earned over lifetime (never decreases).
    /// </summary>
    public int LifetimeLoyaltyPoints { get; private set; }

    /// <summary>
    /// Comma-separated tags for flexible categorization.
    /// </summary>
    public string? Tags { get; private set; }

    /// <summary>
    /// Internal notes about the customer.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Whether the customer is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    // Navigation
    public virtual ICollection<CustomerAddress> Addresses { get; private set; } = new List<CustomerAddress>();

    private readonly List<CustomerGroupMembership> _groupMemberships = new();
    public IReadOnlyCollection<CustomerGroupMembership> GroupMemberships => _groupMemberships.AsReadOnly();

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    public static Customer Create(
        string? userId,
        string email,
        string firstName,
        string lastName,
        string? phone = null,
        string? tenantId = null)
    {
        return new Customer(Guid.NewGuid(), tenantId)
        {
            UserId = userId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            Segment = CustomerSegment.New,
            Tier = CustomerTier.Standard,
            IsActive = true
        };
    }

    /// <summary>
    /// Updates basic profile information.
    /// </summary>
    public void UpdateProfile(string firstName, string lastName, string email, string? phone)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
    }

    /// <summary>
    /// Updates RFM metrics from order data.
    /// </summary>
    public void UpdateRfmMetrics(DateTimeOffset lastOrderDate, int totalOrders, decimal totalSpent)
    {
        LastOrderDate = lastOrderDate;
        TotalOrders = totalOrders;
        TotalSpent = totalSpent;
        AverageOrderValue = totalOrders > 0 ? totalSpent / totalOrders : 0;
    }

    /// <summary>
    /// Recalculates the customer segment based on RFM data.
    /// </summary>
    public void RecalculateSegment()
    {
        // VIP: high frequency + high monetary
        if (TotalOrders >= 20 && TotalSpent >= 10_000_000m)
        {
            Segment = CustomerSegment.VIP;
            return;
        }

        // New customer
        if (TotalOrders <= 1)
        {
            Segment = CustomerSegment.New;
            return;
        }

        // Recency-based segments
        if (LastOrderDate is null)
        {
            Segment = CustomerSegment.Lost;
            return;
        }

        var daysSinceLastOrder = (DateTimeOffset.UtcNow - LastOrderDate.Value).TotalDays;

        Segment = daysSinceLastOrder switch
        {
            <= 30 => CustomerSegment.Active,
            <= 90 => CustomerSegment.AtRisk,
            <= 180 => CustomerSegment.Dormant,
            _ => CustomerSegment.Lost
        };
    }

    /// <summary>
    /// Updates the customer loyalty tier based on lifetime points.
    /// </summary>
    public void UpdateTier()
    {
        Tier = LifetimeLoyaltyPoints switch
        {
            >= 50000 => CustomerTier.Diamond,
            >= 20000 => CustomerTier.Platinum,
            >= 10000 => CustomerTier.Gold,
            >= 5000 => CustomerTier.Silver,
            _ => CustomerTier.Standard
        };
    }

    /// <summary>
    /// Adds loyalty points to the customer.
    /// </summary>
    public void AddLoyaltyPoints(int points)
    {
        if (points <= 0)
            throw new InvalidOperationException("Points must be positive.");

        LoyaltyPoints += points;
        LifetimeLoyaltyPoints += points;
        UpdateTier();
    }

    /// <summary>
    /// Redeems loyalty points from the customer.
    /// </summary>
    public void RedeemLoyaltyPoints(int points)
    {
        if (points <= 0)
            throw new InvalidOperationException("Points must be positive.");

        if (points > LoyaltyPoints)
            throw new InvalidOperationException($"Insufficient loyalty points. Available: {LoyaltyPoints}, Requested: {points}");

        LoyaltyPoints -= points;
    }

    /// <summary>
    /// Adds a tag to the customer.
    /// </summary>
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        var tagList = string.IsNullOrEmpty(Tags)
            ? new List<string>()
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        if (!tagList.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            tagList.Add(tag.Trim());
            Tags = string.Join(",", tagList);
        }
    }

    /// <summary>
    /// Removes a tag from the customer.
    /// </summary>
    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrEmpty(Tags)) return;

        var tagList = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        tagList.RemoveAll(t => string.Equals(t, tag.Trim(), StringComparison.OrdinalIgnoreCase));
        Tags = tagList.Count > 0 ? string.Join(",", tagList) : null;
    }

    /// <summary>
    /// Adds or appends a note.
    /// </summary>
    public void AddNote(string note)
    {
        if (string.IsNullOrWhiteSpace(note)) return;

        Notes = string.IsNullOrEmpty(Notes)
            ? note
            : $"{Notes}\n---\n{note}";
    }

    /// <summary>
    /// Sets the customer segment manually (admin override).
    /// </summary>
    public void SetSegment(CustomerSegment segment)
    {
        Segment = segment;
    }

    /// <summary>
    /// Deactivates the customer.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activates the customer.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}
