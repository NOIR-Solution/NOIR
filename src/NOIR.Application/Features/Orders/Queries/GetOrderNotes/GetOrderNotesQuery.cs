namespace NOIR.Application.Features.Orders.Queries.GetOrderNotes;

/// <summary>
/// Query to get notes for an order.
/// </summary>
public sealed record GetOrderNotesQuery(Guid OrderId);
