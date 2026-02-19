namespace NOIR.Domain.Entities.Order;

/// <summary>
/// Represents an internal staff note on an order.
/// Used for team communication about order handling.
/// </summary>
public class OrderNote : TenantEntity<Guid>
{
    private OrderNote() : base() { }
    private OrderNote(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Parent order ID.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// Note content.
    /// </summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// User ID of the note author.
    /// </summary>
    public string CreatedByUserId { get; private set; } = string.Empty;

    /// <summary>
    /// Display name of the note author.
    /// </summary>
    public string CreatedByUserName { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the note is internal (staff-only). All notes are internal for now.
    /// </summary>
    public bool IsInternal { get; private set; } = true;

    /// <summary>
    /// Navigation to parent order.
    /// </summary>
    public virtual Order? Order { get; private set; }

    /// <summary>
    /// Creates a new order note.
    /// </summary>
    public static OrderNote Create(
        Guid orderId,
        string content,
        string userId,
        string userName,
        string? tenantId = null)
    {
        return new OrderNote(Guid.NewGuid(), tenantId)
        {
            OrderId = orderId,
            Content = content,
            CreatedByUserId = userId,
            CreatedByUserName = userName,
            IsInternal = true
        };
    }
}
