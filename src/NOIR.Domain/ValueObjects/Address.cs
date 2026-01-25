namespace NOIR.Domain.ValueObjects;

/// <summary>
/// Address value object for shipping and billing.
/// Follows Vietnam address format (Ward/District/Province).
/// </summary>
public record Address
{
    /// <summary>
    /// Full name of recipient.
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Contact phone number.
    /// </summary>
    public string Phone { get; init; } = string.Empty;

    /// <summary>
    /// Primary street address.
    /// </summary>
    public string AddressLine1 { get; init; } = string.Empty;

    /// <summary>
    /// Secondary address (apartment, suite, etc.).
    /// </summary>
    public string? AddressLine2 { get; init; }

    /// <summary>
    /// Ward (Phuong/Xa in Vietnam).
    /// </summary>
    public string Ward { get; init; } = string.Empty;

    /// <summary>
    /// District (Quan/Huyen in Vietnam).
    /// </summary>
    public string District { get; init; } = string.Empty;

    /// <summary>
    /// Province/City (Tinh/Thanh pho in Vietnam).
    /// </summary>
    public string Province { get; init; } = string.Empty;

    /// <summary>
    /// Country name.
    /// </summary>
    public string Country { get; init; } = "Vietnam";

    /// <summary>
    /// Postal/ZIP code (optional in Vietnam).
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// Whether this is the default address for the user.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Returns formatted single-line address.
    /// </summary>
    public string ToSingleLine() =>
        string.Join(", ",
            new[] { AddressLine1, AddressLine2, Ward, District, Province, Country }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>
    /// Creates a copy with IsDefault set.
    /// </summary>
    public Address WithDefault(bool isDefault) => this with { IsDefault = isDefault };
}
