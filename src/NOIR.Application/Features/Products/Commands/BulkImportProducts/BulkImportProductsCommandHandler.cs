using System.Text.RegularExpressions;

namespace NOIR.Application.Features.Products.Commands.BulkImportProducts;

/// <summary>
/// Wolverine handler for bulk importing products.
/// Processes each product individually, collecting errors.
/// </summary>
public partial class BulkImportProductsCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkImportProductsCommandHandler> _logger;

    public BulkImportProductsCommandHandler(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<BulkImportProductsCommandHandler> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<BulkImportResultDto>> Handle(
        BulkImportProductsCommand command,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        var errors = new List<ImportErrorDto>();

        // Pre-load categories for lookup (single query)
        var categoriesSpec = new AllProductCategoriesSpec();
        var categories = await _categoryRepository.ListAsync(categoriesSpec, cancellationToken);
        var categoryLookup = categories.ToDictionary(
            c => c.Name.ToLowerInvariant(),
            c => c.Id);

        // Generate all proposed slugs upfront
        var proposedSlugs = command.Products
            .Select(p => p.Slug ?? GenerateSlug(p.Name))
            .ToList();

        // Pre-load existing slugs in a single query (O(1) instead of O(n))
        var slugsSpec = new ProductsBySlugsSpec(proposedSlugs);
        var existingProducts = await _productRepository.ListAsync(slugsSpec, cancellationToken);
        var existingSlugs = new HashSet<string>(existingProducts.Select(p => p.Slug), StringComparer.OrdinalIgnoreCase);

        // Track slugs used during import to avoid duplicates within the batch
        var usedSlugs = new HashSet<string>(existingSlugs, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < command.Products.Count; i++)
        {
            var row = i + 2; // Row number (1-indexed, +1 for header)
            var importProduct = command.Products[i];

            try
            {
                // Generate slug if not provided
                var slug = proposedSlugs[i];

                // Make unique if exists in DB or already used in this batch
                while (usedSlugs.Contains(slug))
                {
                    slug = $"{slug}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}";
                }
                usedSlugs.Add(slug);

                // Lookup category by name
                Guid? categoryId = null;
                if (!string.IsNullOrEmpty(importProduct.CategoryName))
                {
                    if (categoryLookup.TryGetValue(importProduct.CategoryName.ToLowerInvariant(), out var catId))
                    {
                        categoryId = catId;
                    }
                }

                // Create product
                var product = Product.Create(
                    importProduct.Name,
                    slug,
                    importProduct.BasePrice,
                    importProduct.Currency ?? "VND");

                // Set optional fields
                product.UpdateBasicInfo(
                    importProduct.Name,
                    slug,
                    importProduct.ShortDescription,
                    null,
                    null);

                product.SetCategory(categoryId);
                product.SetBrand(importProduct.Brand);
                product.UpdateIdentification(importProduct.Sku, importProduct.Barcode);

                // Add default variant with stock if provided
                if (importProduct.Stock.HasValue)
                {
                    var variant = product.AddVariant(
                        "Default",
                        importProduct.BasePrice,
                        importProduct.Sku);
                    variant.SetStock(importProduct.Stock.Value);
                }

                await _productRepository.AddAsync(product, cancellationToken);
                successCount++;
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new ImportErrorDto(row, ex.Message));
            }
            catch (ArgumentException ex)
            {
                errors.Add(new ImportErrorDto(row, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error importing product at row {Row}: {ProductName}",
                    row, importProduct.Name);
                errors.Add(new ImportErrorDto(row, "Failed to import product due to unexpected error"));
            }
        }

        // Save all at once
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new BulkImportResultDto(
            successCount,
            errors.Count,
            errors));
    }

    /// <summary>
    /// Generates a URL-friendly slug from a product name.
    /// </summary>
    private static string GenerateSlug(string name)
    {
        // Convert to lowercase
        var slug = name.ToLowerInvariant();

        // Remove accents/diacritics (simplified - for Vietnamese would need more comprehensive handling)
        slug = RemoveAccents(slug);

        // Replace spaces with hyphens
        slug = slug.Replace(" ", "-");

        // Remove invalid characters
        slug = SlugRegex().Replace(slug, "");

        // Remove duplicate hyphens
        slug = DuplicateHyphenRegex().Replace(slug, "-");

        // Trim hyphens from ends
        slug = slug.Trim('-');

        return slug;
    }

    private static string RemoveAccents(string text)
    {
        // Simple accent removal - for production would use proper normalization
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    [GeneratedRegex("[^a-z0-9-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex("-+")]
    private static partial Regex DuplicateHyphenRegex();
}
