namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Seeds product catalog data: categories, brands, attributes, and products with variants.
/// Order 100 - runs first since other modules may depend on products.
/// </summary>
public class CatalogSeedModule : ISeedDataModule
{
    public int Order => 100;
    public string ModuleName => "Catalog";

    public async Task SeedAsync(SeedDataContext context, CancellationToken ct = default)
    {
        var tenantId = context.CurrentTenant.Id;

        // Idempotency: skip if products already exist for this tenant
        var hasData = await context.DbContext.Set<Product>()
            .IgnoreQueryFilters()
            .TagWith("SeedData:CheckCatalog")
            .AnyAsync(p => p.TenantId == tenantId, ct);

        if (hasData)
        {
            context.Logger.LogInformation("[SeedData] Catalog already seeded for {Tenant}, skipping", tenantId);
            return;
        }

        // 1. Seed categories (parents first, then children)
        var categoryLookup = SeedCategories(context, tenantId);

        // 2. Seed brands
        var brandLookup = SeedBrands(context, tenantId);

        // 3. Seed attributes + values
        SeedAttributes(context, tenantId);

        // Flush categories, brands, and attributes
        await context.DbContext.SaveChangesAsync(ct);

        // 4. Seed products with variants and images
        var (productCount, variantCount, imageCount) = await SeedProductsAsync(
            context, tenantId, categoryLookup, brandLookup, ct);

        await context.DbContext.SaveChangesAsync(ct);

        context.Logger.LogInformation(
            "[SeedData] Catalog: {Products} products, {Variants} variants, {Images} images",
            productCount, variantCount, imageCount);
    }

    private static Dictionary<string, Guid> SeedCategories(
        SeedDataContext context, string tenantId)
    {
        var categoryLookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var categories = CatalogData.GetCategories();

        // Parents first (ParentSlug == null)
        foreach (var def in categories.Where(c => c.ParentSlug == null))
        {
            var id = SeedDataConstants.TenantGuid(tenantId, $"cat:{def.Slug}");
            var cat = ProductCategory.Create(def.Name, def.Slug, null, tenantId);
            SeedDataConstants.SetEntityId(cat, id);

            if (def.Description != null)
            {
                cat.UpdateDetails(def.Name, def.Slug, def.Description, null);
            }

            context.DbContext.Set<ProductCategory>().Add(cat);
            categoryLookup[def.Slug] = id;
        }

        // Then children (ParentSlug != null)
        foreach (var def in categories.Where(c => c.ParentSlug != null))
        {
            var id = SeedDataConstants.TenantGuid(tenantId, $"cat:{def.Slug}");

            if (!categoryLookup.TryGetValue(def.ParentSlug!, out var parentId))
            {
                context.Logger.LogWarning("[SeedData] Parent category '{Parent}' not found for '{Child}', skipping",
                    def.ParentSlug, def.Slug);
                continue;
            }

            var cat = ProductCategory.Create(def.Name, def.Slug, parentId, tenantId);
            SeedDataConstants.SetEntityId(cat, id);

            if (def.Description != null)
            {
                cat.UpdateDetails(def.Name, def.Slug, def.Description, null);
            }

            context.DbContext.Set<ProductCategory>().Add(cat);
            categoryLookup[def.Slug] = id;
        }

        return categoryLookup;
    }

    private static Dictionary<string, Guid> SeedBrands(
        SeedDataContext context, string tenantId)
    {
        var brandLookup = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in CatalogData.GetBrands())
        {
            var id = SeedDataConstants.TenantGuid(tenantId, $"brand:{def.Slug}");
            var brand = Brand.Create(def.Name, def.Slug, tenantId);
            SeedDataConstants.SetEntityId(brand, id);

            if (def.Description != null)
            {
                brand.UpdateDetails(def.Name, def.Slug, def.Description, null);
            }

            context.DbContext.Set<Brand>().Add(brand);
            brandLookup[def.Slug] = id;
        }

        return brandLookup;
    }

    private static void SeedAttributes(
        SeedDataContext context, string tenantId)
    {
        foreach (var def in CatalogData.GetAttributes())
        {
            var attr = ProductAttribute.Create(def.Code, def.Name, def.Type, tenantId);

            if (def.IsFilterable)
            {
                attr.SetBehaviorFlags(true, true, false, false);
            }

            if (def.Values != null)
            {
                var sortOrder = 0;
                foreach (var v in def.Values)
                {
                    var attrValue = attr.AddValue(v.Value, v.DisplayValue, sortOrder++);
                    if (v.ColorCode != null)
                    {
                        attrValue.SetVisualDisplay(v.ColorCode, null, null);
                    }
                }
            }

            context.DbContext.Set<ProductAttribute>().Add(attr);
        }
    }

    private static async Task<(int Products, int Variants, int Images)> SeedProductsAsync(
        SeedDataContext context,
        string tenantId,
        Dictionary<string, Guid> categoryLookup,
        Dictionary<string, Guid> brandLookup,
        CancellationToken ct)
    {
        var imageProcessor = context.ServiceProvider.GetService<IImageProcessor>();
        var productCount = 0;
        var variantCount = 0;
        var imageCount = 0;

        foreach (var def in CatalogData.GetProducts())
        {
            var productId = SeedDataConstants.TenantGuid(tenantId, $"product:{def.Slug}");
            var product = Product.Create(def.Name, def.Slug, def.BasePrice, "VND", tenantId);
            SeedDataConstants.SetEntityId(product, productId);

            // Set category
            if (categoryLookup.TryGetValue(def.CategorySlug, out var catId))
            {
                product.SetCategory(catId);
            }

            // Set brand
            if (brandLookup.TryGetValue(def.BrandSlug, out var brandId))
            {
                product.SetBrandId(brandId);
            }

            // Set description
            product.UpdateBasicInfo(def.Name, def.Slug, def.ShortDescription, null, def.DescriptionHtml);

            // Add variants
            foreach (var v in def.Variants)
            {
                var variant = product.AddVariant(v.Name, v.Price, v.Sku, v.Options);
                variant.SetStock(v.Stock);
                variantCount++;
            }

            // Generate and process placeholder image
            var imgResult = await SeedImageHelper.GenerateAndProcessAsync(
                imageProcessor, 800, 600, def.ImageColor, def.Name,
                def.Slug, $"images/products/{productId}", context.Logger, ct);

            if (imgResult != null)
            {
                var primaryUrl = SeedImageHelper.GetPrimaryUrl(imgResult);
                if (primaryUrl != null)
                {
                    product.AddImage(primaryUrl, def.Name, true);
                    imageCount++;
                }
            }

            // Publish ~70% of products (indices 0-6 out of every 10)
            if (productCount % 10 < 7)
            {
                product.Publish();
            }

            context.DbContext.Set<Product>().Add(product);
            productCount++;
        }

        return (productCount, variantCount, imageCount);
    }

}
