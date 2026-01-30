using System.Text.RegularExpressions;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.Features.Products.Commands.BulkImportProducts;

/// <summary>
/// Wolverine handler for bulk importing products.
/// Supports variants (multiple rows per product), images (pipe-separated), and attributes (dynamic columns).
/// </summary>
public partial class BulkImportProductsCommandHandler
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryMovementLogger _movementLogger;
    private readonly ILogger<BulkImportProductsCommandHandler> _logger;

    public BulkImportProductsCommandHandler(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IRepository<ProductAttribute, Guid> attributeRepository,
        IUnitOfWork unitOfWork,
        IInventoryMovementLogger movementLogger,
        ILogger<BulkImportProductsCommandHandler> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _attributeRepository = attributeRepository;
        _unitOfWork = unitOfWork;
        _movementLogger = movementLogger;
        _logger = logger;
    }

    public async Task<Result<BulkImportResultDto>> Handle(
        BulkImportProductsCommand command,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        var errors = new List<ImportErrorDto>();
        var importedProducts = new List<Product>();

        // Pre-load categories for lookup (single query)
        var categoriesSpec = new AllProductCategoriesSpec();
        var categories = await _categoryRepository.ListAsync(categoriesSpec, cancellationToken);
        var categoryLookup = categories.ToDictionary(
            c => c.Name.ToLowerInvariant(),
            c => c.Id);

        // Pre-load attributes by codes (if any attributes in import data)
        var attributeCodes = command.Products
            .Where(p => p.Attributes != null)
            .SelectMany(p => p.Attributes!.Keys)
            .Distinct()
            .ToList();

        var attributeLookup = new Dictionary<string, ProductAttribute>(StringComparer.OrdinalIgnoreCase);
        if (attributeCodes.Any())
        {
            var attributesSpec = new ProductAttributesByCodesSpec(attributeCodes, includeValues: true);
            var attributes = await _attributeRepository.ListAsync(attributesSpec, cancellationToken);
            attributeLookup = attributes.ToDictionary(a => a.Code, a => a, StringComparer.OrdinalIgnoreCase);
        }

        // Group products by slug to handle variants
        var productGroups = GroupProductsBySlug(command.Products);

        // Pre-load existing slugs in a single query
        var proposedSlugs = productGroups.Keys.ToList();
        var slugsSpec = new ProductsBySlugsSpec(proposedSlugs);
        var existingProducts = await _productRepository.ListAsync(slugsSpec, cancellationToken);
        var existingSlugs = new HashSet<string>(existingProducts.Select(p => p.Slug), StringComparer.OrdinalIgnoreCase);

        // Track slugs used during import to avoid duplicates within the batch
        var usedSlugs = new HashSet<string>(existingSlugs, StringComparer.OrdinalIgnoreCase);

        foreach (var (slug, productRows) in productGroups)
        {
            var firstRow = productRows.First();
            var rowNumber = firstRow.RowNumber;

            try
            {
                // Generate unique slug if needed
                var finalSlug = slug;
                while (usedSlugs.Contains(finalSlug))
                {
                    finalSlug = $"{finalSlug}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}";
                }
                usedSlugs.Add(finalSlug);

                // Lookup category by name
                Guid? categoryId = null;
                if (!string.IsNullOrEmpty(firstRow.Import.CategoryName))
                {
                    if (categoryLookup.TryGetValue(firstRow.Import.CategoryName.ToLowerInvariant(), out var catId))
                    {
                        categoryId = catId;
                    }
                }

                // Create product from first row
                var product = Product.Create(
                    firstRow.Import.Name,
                    finalSlug,
                    firstRow.Import.BasePrice,
                    firstRow.Import.Currency ?? "VND");

                product.UpdateBasicInfo(
                    firstRow.Import.Name,
                    finalSlug,
                    firstRow.Import.ShortDescription,
                    null,
                    null);

                product.SetCategory(categoryId);
                product.SetBrand(firstRow.Import.Brand);
                product.UpdateIdentification(firstRow.Import.Sku, firstRow.Import.Barcode);

                // Process product images from first row (product-level images)
                if (!string.IsNullOrEmpty(firstRow.Import.Images))
                {
                    var imageUrls = firstRow.Import.Images.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    for (var i = 0; i < imageUrls.Length; i++)
                    {
                        var imageUrl = imageUrls[i].Trim();
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            // First image is primary, others are not
                            product.AddImage(imageUrl, null, i == 0);
                        }
                    }
                }

                // Process variants - each row becomes a variant
                var variantSortOrder = 0;
                foreach (var productRow in productRows)
                {
                    var variantName = productRow.Import.VariantName ?? "Default";
                    var variantPrice = productRow.Import.VariantPrice ?? productRow.Import.BasePrice;
                    var variantSku = productRow.Import.Sku;

                    var variant = product.AddVariant(
                        variantName,
                        variantPrice,
                        variantSku);

                    variant.SetSortOrder(variantSortOrder++);

                    if (productRow.Import.CompareAtPrice.HasValue)
                    {
                        variant.SetCompareAtPrice(productRow.Import.CompareAtPrice.Value);
                    }

                    if (productRow.Import.Stock.HasValue)
                    {
                        variant.SetStock(productRow.Import.Stock.Value);
                    }
                }

                // Process attributes from first row - add to product's collection
                // EF Core will cascade the insert when product is added
                var attributeWarnings = new List<string>();
                if (firstRow.Import.Attributes != null && firstRow.Import.Attributes.Any())
                {
                    foreach (var (attrCode, attrValue) in firstRow.Import.Attributes)
                    {
                        if (string.IsNullOrEmpty(attrValue)) continue;

                        if (!attributeLookup.TryGetValue(attrCode, out var attribute))
                        {
                            attributeWarnings.Add($"Attribute '{attrCode}' not found");
                            _logger.LogWarning("Attribute code '{Code}' not found, skipping", attrCode);
                            continue;
                        }

                        try
                        {
                            var assignment = CreateAttributeAssignment(product, attribute, attrValue);
                            if (assignment != null)
                            {
                                // Add to product's collection - EF Core cascades on AddAsync
                                product.AttributeAssignments.Add(assignment);
                            }
                            else
                            {
                                attributeWarnings.Add($"Could not parse value '{attrValue}' for attribute '{attrCode}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            attributeWarnings.Add($"Failed to set attribute '{attrCode}': {ex.Message}");
                            _logger.LogWarning(ex, "Failed to set attribute '{Code}' value '{Value}' for product at row {Row}",
                                attrCode, attrValue, rowNumber);
                        }
                    }
                }

                await _productRepository.AddAsync(product, cancellationToken);
                importedProducts.Add(product);
                successCount++;

                // Report attribute warnings (product was imported but some attributes failed)
                if (attributeWarnings.Any())
                {
                    errors.Add(new ImportErrorDto(rowNumber, $"Product imported with warnings: {string.Join("; ", attributeWarnings)}"));
                }
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new ImportErrorDto(rowNumber, ex.Message));
            }
            catch (ArgumentException ex)
            {
                errors.Add(new ImportErrorDto(rowNumber, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error importing product at row {Row}: {ProductName}",
                    rowNumber, firstRow.Import.Name);
                errors.Add(new ImportErrorDto(rowNumber, "Failed to import product due to unexpected error"));
            }
        }

        // Save all at once
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log initial stock as StockIn for all imported variants with stock > 0
        foreach (var product in importedProducts)
        {
            foreach (var variant in product.Variants.Where(v => v.StockQuantity > 0))
            {
                await _movementLogger.LogMovementAsync(
                    variant,
                    InventoryMovementType.StockIn,
                    quantityBefore: 0,
                    quantityMoved: variant.StockQuantity,
                    reference: $"SKU: {variant.Sku}",
                    notes: "Initial stock on bulk import",
                    userId: null,
                    cancellationToken: cancellationToken);
            }
        }

        _logger.LogInformation(
            "Completed bulk import: {SuccessCount} products imported, {ErrorCount} errors",
            successCount, errors.Count);

        return Result.Success(new BulkImportResultDto(
            successCount,
            errors.Count,
            errors));
    }

    /// <summary>
    /// Groups import rows by product slug, tracking original row numbers.
    /// </summary>
    private static Dictionary<string, List<(ImportProductDto Import, int RowNumber)>> GroupProductsBySlug(
        List<ImportProductDto> products)
    {
        var groups = new Dictionary<string, List<(ImportProductDto, int)>>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < products.Count; i++)
        {
            var row = i + 2; // Row number (1-indexed, +1 for header)
            var product = products[i];
            var slug = product.Slug ?? GenerateSlug(product.Name);

            if (!groups.ContainsKey(slug))
            {
                groups[slug] = new List<(ImportProductDto, int)>();
            }
            groups[slug].Add((product, row));
        }

        return groups;
    }

    /// <summary>
    /// Creates an attribute assignment based on attribute type and value.
    /// Returns the assignment for explicit persistence, or null if value is invalid/unparseable.
    /// </summary>
    private static ProductAttributeAssignment? CreateAttributeAssignment(Product product, ProductAttribute attribute, string value)
    {
        var assignment = ProductAttributeAssignment.Create(product.Id, attribute.Id, null, product.TenantId);
        var valueSet = false;

        switch (attribute.Type)
        {
            case AttributeType.Select:
                // Look up value by display name
                var selectValue = attribute.Values.FirstOrDefault(v =>
                    v.DisplayValue.Equals(value, StringComparison.OrdinalIgnoreCase));
                if (selectValue != null)
                {
                    assignment.SetSelectValue(selectValue.Id, selectValue.DisplayValue);
                    valueSet = true;
                }
                break;

            case AttributeType.MultiSelect:
                // Values separated by semicolon
                var multiValues = value.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .ToList();
                var matchedValues = attribute.Values
                    .Where(v => multiValues.Any(mv => mv.Equals(v.DisplayValue, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (matchedValues.Any())
                {
                    assignment.SetMultiSelectValue(
                        matchedValues.Select(v => v.Id).ToList(),
                        string.Join(", ", matchedValues.Select(v => v.DisplayValue)));
                    valueSet = true;
                }
                break;

            case AttributeType.Text:
            case AttributeType.TextArea:
            case AttributeType.Url:
                assignment.SetTextValue(value);
                valueSet = true;
                break;

            case AttributeType.Number:
                if (int.TryParse(value, out var intValue))
                {
                    assignment.SetNumberValue(intValue, attribute.Unit);
                    valueSet = true;
                }
                break;

            case AttributeType.Decimal:
                if (decimal.TryParse(value, out var decValue))
                {
                    assignment.SetNumberValue(decValue, attribute.Unit);
                    valueSet = true;
                }
                break;

            case AttributeType.Boolean:
                var boolValue = value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                               value.Equals("1", StringComparison.OrdinalIgnoreCase);
                assignment.SetBoolValue(boolValue);
                valueSet = true;
                break;

            case AttributeType.Date:
                if (DateTime.TryParse(value, out var dateValue))
                {
                    assignment.SetDateValue(dateValue);
                    valueSet = true;
                }
                break;

            case AttributeType.DateTime:
                if (DateTime.TryParse(value, out var dateTimeValue))
                {
                    assignment.SetDateTimeValue(dateTimeValue);
                    valueSet = true;
                }
                break;

            case AttributeType.Color:
                assignment.SetColorValue(value);
                valueSet = true;
                break;

            case AttributeType.Range:
                // Format: "min-max" e.g., "10-50"
                var rangeParts = value.Split('-');
                if (rangeParts.Length == 2 &&
                    decimal.TryParse(rangeParts[0], out var minValue) &&
                    decimal.TryParse(rangeParts[1], out var maxValue))
                {
                    assignment.SetRangeValue(minValue, maxValue, attribute.Unit);
                    valueSet = true;
                }
                break;

            case AttributeType.File:
                assignment.SetFileValue(value);
                valueSet = true;
                break;
        }

        return valueSet ? assignment : null;
    }

    /// <summary>
    /// Generates a URL-friendly slug from a product name.
    /// </summary>
    private static string GenerateSlug(string name)
    {
        // Convert to lowercase
        var slug = name.ToLowerInvariant();

        // Remove accents/diacritics
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
