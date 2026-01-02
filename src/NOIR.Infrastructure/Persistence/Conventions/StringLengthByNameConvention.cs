namespace NOIR.Infrastructure.Persistence.Conventions;

/// <summary>
/// Convention that applies string max lengths based on property name patterns.
/// This ensures consistent field lengths across all entities.
/// </summary>
public class StringLengthByNameConvention : IModelFinalizingConvention
{
    private static readonly Dictionary<string, int> PropertyNameLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        // Identity fields
        { "Name", 200 },
        { "Title", 200 },
        { "Email", 256 },
        { "PhoneNumber", 20 },
        { "Phone", 20 },

        // Content fields
        { "Description", 2000 },
        { "Notes", 4000 },
        { "Comment", 4000 },
        { "Message", 4000 },

        // Reference fields
        { "Code", 50 },
        { "Reference", 100 },
        { "ExternalId", 100 },

        // Address fields
        { "Address", 500 },
        { "Street", 200 },
        { "City", 100 },
        { "State", 100 },
        { "Country", 100 },
        { "PostalCode", 20 },
        { "ZipCode", 20 },

        // URL fields
        { "Url", 2000 },
        { "ImageUrl", 2000 },
        { "Website", 2000 },

        // Network fields
        { "IpAddress", 45 }, // IPv6 max length
        { "UserAgent", 500 },
        { "DeviceName", 200 },

        // Security fields
        { "Token", 500 },
        { "CorrelationId", 100 }
    };

    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties()
                .Where(p => p.ClrType == typeof(string)))
            {
                // Skip if max length is already configured
                if (property.GetMaxLength().HasValue)
                    continue;

                // Check if property name matches any pattern
                foreach (var pattern in PropertyNameLengths)
                {
                    if (property.Name.EndsWith(pattern.Key, StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals(pattern.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        property.Builder.HasMaxLength(pattern.Value);
                        break;
                    }
                }
            }
        }
    }
}
