namespace NOIR.Application.Features.Shipping.DTOs;

/// <summary>
/// Address information for shipping operations.
/// </summary>
public record ShippingAddressDto(
    string FullName,
    string Phone,
    string? Email,
    string AddressLine1,
    string? AddressLine2,
    string Ward,
    string WardCode,
    string District,
    string DistrictCode,
    string Province,
    string ProvinceCode,
    string? PostalCode = null,
    string CountryCode = "VN");

/// <summary>
/// Contact information for sender/recipient.
/// </summary>
public record ShippingContactDto(
    string FullName,
    string Phone,
    string? Email = null);
