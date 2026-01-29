namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductFilterIndex entity.
/// Optimized for high-performance filtering queries.
/// </summary>
public class ProductFilterIndexConfiguration : IEntityTypeConfiguration<ProductFilterIndex>
{
    public void Configure(EntityTypeBuilder<ProductFilterIndex> builder)
    {
        builder.ToTable("ProductFilterIndexes");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // 1:1 relationship with Product
        builder.HasIndex(e => new { e.TenantId, e.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_ProductFilterIndexes_TenantId_ProductId");

        builder.HasOne(e => e.Product)
            .WithOne()
            .HasForeignKey<ProductFilterIndex>(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade); // Delete index when product is deleted

        #region Product Info

        builder.Property(e => e.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ProductSlug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<int>();

        builder.Property(e => e.Sku)
            .HasMaxLength(50);

        #endregion

        #region Category

        builder.Property(e => e.CategoryPath)
            .HasMaxLength(500);

        builder.Property(e => e.CategoryName)
            .HasMaxLength(200);

        builder.Property(e => e.CategorySlug)
            .HasMaxLength(200);

        // Index for category filtering (includes TenantId for multi-tenancy)
        builder.HasIndex(e => new { e.TenantId, e.CategoryId })
            .HasDatabaseName("IX_ProductFilterIndexes_TenantId_CategoryId");

        // Index for category hierarchy queries (LIKE on CategoryPath)
        builder.HasIndex(e => new { e.TenantId, e.CategoryPath })
            .HasDatabaseName("IX_ProductFilterIndexes_TenantId_CategoryPath");

        #endregion

        #region Brand

        builder.Property(e => e.BrandName)
            .HasMaxLength(100);

        builder.Property(e => e.BrandSlug)
            .HasMaxLength(100);

        // Index for brand filtering
        builder.HasIndex(e => new { e.TenantId, e.BrandId })
            .HasDatabaseName("IX_ProductFilterIndexes_TenantId_BrandId");

        #endregion

        #region Pricing

        builder.Property(e => e.MinPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.MaxPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("VND");

        // Index for price range filtering
        builder.HasIndex(e => new { e.TenantId, e.MinPrice, e.MaxPrice })
            .HasDatabaseName("IX_ProductFilterIndexes_TenantId_Price");

        #endregion

        #region Inventory

        // Index for in-stock filtering (most common filter)
        builder.HasIndex(e => new { e.TenantId, e.InStock })
            .HasDatabaseName("IX_ProductFilterIndexes_TenantId_InStock");

        #endregion

        #region Reviews

        builder.Property(e => e.AverageRating)
            .HasPrecision(3, 2);

        // Index for rating filtering
        builder.HasIndex(e => new { e.TenantId, e.AverageRating })
            .HasDatabaseName("IX_ProductFilterIndexes_TenantId_Rating");

        #endregion

        #region Attributes (JSONB)

        // AttributesJson for flexible attribute filtering
        // Format: {"color": ["red", "blue"], "size": ["m", "l"], "screen_size": 6.7}
        builder.Property(e => e.AttributesJson)
            .HasColumnType("nvarchar(max)"); // Use nvarchar(max) for SQL Server, jsonb for PostgreSQL

        // Note: For PostgreSQL, you would create a GIN index on the JSONB column via raw SQL:
        // CREATE INDEX IX_ProductFilterIndexes_Attributes_GIN ON ProductFilterIndexes USING GIN (AttributesJson jsonb_path_ops);
        // This is done in the migration or via HasAnnotation

        #endregion

        #region Search

        builder.Property(e => e.SearchText)
            .HasMaxLength(4000); // Concatenated search text

        // Full-text search index is typically created via raw SQL in the migration
        // SQL Server: CREATE FULLTEXT INDEX ON ProductFilterIndexes(SearchText) ...
        // PostgreSQL: CREATE INDEX ON ProductFilterIndexes USING GIN(to_tsvector('english', SearchText))

        #endregion

        #region Display

        builder.Property(e => e.PrimaryImageUrl)
            .HasMaxLength(500);

        #endregion

        #region Composite Indexes for Common Queries

        // Index for listing (status + sort)
        builder.HasIndex(e => new { e.TenantId, e.Status, e.SortOrder })
            .HasDatabaseName("IX_ProductFilterIndexes_TenantId_Status_Sort");

        // Index for category listing (category + status + sort)
        builder.HasIndex(e => new { e.TenantId, e.CategoryId, e.Status, e.SortOrder })
            .HasDatabaseName("IX_ProductFilterIndexes_TenantId_Category_Status_Sort");

        // Index for stale detection (sync monitoring)
        builder.HasIndex(e => new { e.TenantId, e.LastSyncedAt })
            .HasDatabaseName("IX_ProductFilterIndexes_TenantId_LastSynced");

        #endregion

        #region Tenant

        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        #endregion

        // Note: No CreatedBy/ModifiedBy - this is an auto-synced index table
        // that follows the product's lifecycle via domain events.
    }
}
