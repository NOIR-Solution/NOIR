namespace NOIR.Application.Features.Customers.Queries.GetCustomerById;

/// <summary>
/// Query to get a customer by ID with full details.
/// </summary>
public sealed record GetCustomerByIdQuery(Guid Id);
