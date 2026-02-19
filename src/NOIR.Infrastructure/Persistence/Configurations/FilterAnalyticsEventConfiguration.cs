namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for FilterAnalyticsEvent entity.
/// Optimized for time-series analytics queries.
/// </summary>
public class FilterAnalyticsEventConfiguration : TenantEntityConfiguration<FilterAnalyticsEvent>
{
    public override void Configure(EntityTypeBuilder<FilterAnalyticsEvent> builder)
    {
        base.Configure(builder);

        builder.ToTable("FilterAnalyticsEvents");

        #region Core Properties

        builder.Property(e => e.SessionId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasMaxLength(450); // Match ASP.NET Identity user ID length

        builder.Property(e => e.EventType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.CategorySlug)
            .HasMaxLength(200);

        builder.Property(e => e.FilterCode)
            .HasMaxLength(100);

        builder.Property(e => e.FilterValue)
            .HasMaxLength(500);

        builder.Property(e => e.SearchQuery)
            .HasMaxLength(500);

        #endregion

        #region Indexes for Analytics Queries

        // Primary time-series index for date-range queries
        builder.HasIndex(e => new { e.TenantId, e.CreatedAt })
            .HasDatabaseName("IX_FilterAnalyticsEvents_TenantId_CreatedAt");

        // Index for event type filtering
        builder.HasIndex(e => new { e.TenantId, e.EventType, e.CreatedAt })
            .HasDatabaseName("IX_FilterAnalyticsEvents_TenantId_EventType_CreatedAt");

        // Index for category-specific analytics
        builder.HasIndex(e => new { e.TenantId, e.CategorySlug, e.CreatedAt })
            .HasDatabaseName("IX_FilterAnalyticsEvents_TenantId_Category_CreatedAt");

        // Index for filter code analytics (popular filters)
        builder.HasIndex(e => new { e.TenantId, e.FilterCode, e.FilterValue })
            .HasDatabaseName("IX_FilterAnalyticsEvents_TenantId_FilterCode_FilterValue");

        // Index for session tracking
        builder.HasIndex(e => new { e.TenantId, e.SessionId })
            .HasDatabaseName("IX_FilterAnalyticsEvents_TenantId_SessionId");

        // Index for user behavior analysis
        builder.HasIndex(e => new { e.TenantId, e.UserId, e.CreatedAt })
            .HasDatabaseName("IX_FilterAnalyticsEvents_TenantId_UserId_CreatedAt");

        #endregion
    }
}
