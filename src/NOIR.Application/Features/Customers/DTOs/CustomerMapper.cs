namespace NOIR.Application.Features.Customers.DTOs;

/// <summary>
/// Mapper for Customer-related entities to DTOs.
/// </summary>
public static class CustomerMapper
{
    /// <summary>
    /// Maps a Customer entity to CustomerDto (full detail with addresses).
    /// </summary>
    public static CustomerDto ToDto(Domain.Entities.Customer.Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            UserId = customer.UserId,
            Email = customer.Email,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Phone = customer.Phone,
            Segment = customer.Segment,
            Tier = customer.Tier,
            LastOrderDate = customer.LastOrderDate,
            TotalOrders = customer.TotalOrders,
            TotalSpent = customer.TotalSpent,
            AverageOrderValue = customer.AverageOrderValue,
            LoyaltyPoints = customer.LoyaltyPoints,
            LifetimeLoyaltyPoints = customer.LifetimeLoyaltyPoints,
            Tags = customer.Tags,
            Notes = customer.Notes,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            Addresses = customer.Addresses.Select(ToAddressDto).ToList()
        };
    }

    /// <summary>
    /// Maps a Customer entity to CustomerSummaryDto (list view).
    /// </summary>
    public static CustomerSummaryDto ToSummaryDto(Domain.Entities.Customer.Customer customer)
    {
        return new CustomerSummaryDto
        {
            Id = customer.Id,
            Email = customer.Email,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Phone = customer.Phone,
            Segment = customer.Segment,
            Tier = customer.Tier,
            TotalOrders = customer.TotalOrders,
            TotalSpent = customer.TotalSpent,
            LoyaltyPoints = customer.LoyaltyPoints,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt
        };
    }

    /// <summary>
    /// Maps a CustomerAddress entity to CustomerAddressDto.
    /// </summary>
    public static CustomerAddressDto ToAddressDto(Domain.Entities.Customer.CustomerAddress address)
    {
        return new CustomerAddressDto
        {
            Id = address.Id,
            CustomerId = address.CustomerId,
            AddressType = address.AddressType,
            FullName = address.FullName,
            Phone = address.Phone,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            Ward = address.Ward,
            District = address.District,
            Province = address.Province,
            PostalCode = address.PostalCode,
            IsDefault = address.IsDefault
        };
    }
}
