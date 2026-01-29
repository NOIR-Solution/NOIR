using NOIR.Application.Features.ProductAttributes.Specifications;
using NOIR.Application.Features.Products.Specifications;
using NOIR.Domain.Events.Product;

namespace NOIR.Application.Features.ProductAttributes.Commands.SetProductAttributeValue;

/// <summary>
/// Wolverine handler for setting a product's attribute value.
/// </summary>
public class SetProductAttributeValueCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageBus _messageBus;

    public SetProductAttributeValueCommandHandler(
        IApplicationDbContext dbContext,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductAttribute, Guid> attributeRepository,
        IUnitOfWork unitOfWork,
        IMessageBus messageBus)
    {
        _dbContext = dbContext;
        _productRepository = productRepository;
        _attributeRepository = attributeRepository;
        _unitOfWork = unitOfWork;
        _messageBus = messageBus;
    }

    public async Task<Result<ProductAttributeAssignmentDto>> Handle(
        SetProductAttributeValueCommand command,
        CancellationToken cancellationToken)
    {
        // Verify product exists (with variants loaded if needed for validation)
        var productSpec = new ProductByIdForUpdateSpec(command.ProductId);
        var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);
        if (product == null)
        {
            return Result.Failure<ProductAttributeAssignmentDto>(
                Error.NotFound($"Product with ID '{command.ProductId}' not found.", ErrorCodes.Product.NotFound));
        }

        // Verify attribute exists and include values for Select/MultiSelect validation
        var attributeSpec = new ProductAttributeByIdSpec(command.AttributeId, includeValues: true);
        var attribute = await _attributeRepository.FirstOrDefaultAsync(attributeSpec, cancellationToken);

        if (attribute == null)
        {
            return Result.Failure<ProductAttributeAssignmentDto>(
                Error.NotFound($"Attribute with ID '{command.AttributeId}' not found.", ErrorCodes.Attribute.NotFound));
        }

        // If VariantId is provided, verify it exists on the product
        if (command.VariantId.HasValue)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == command.VariantId);
            if (variant == null)
            {
                return Result.Failure<ProductAttributeAssignmentDto>(
                    Error.NotFound($"Variant with ID '{command.VariantId}' not found for this product.", ErrorCodes.Product.VariantNotFound));
            }
        }

        // Set names for audit
        command.ProductName = product.Name;
        command.AttributeName = attribute.Name;

        // Find or create the assignment
        var assignment = await _dbContext.ProductAttributeAssignments
            .FirstOrDefaultAsync(pa =>
                pa.ProductId == command.ProductId &&
                pa.AttributeId == command.AttributeId &&
                pa.VariantId == command.VariantId,
                cancellationToken);

        var isNew = assignment == null;
        if (isNew)
        {
            assignment = ProductAttributeAssignment.Create(
                command.ProductId,
                command.AttributeId,
                command.VariantId,
                product.TenantId);
            _dbContext.ProductAttributeAssignments.Add(assignment);
        }

        // Set the value based on attribute type
        var setValueResult = SetValueByType(assignment!, attribute, command.Value);
        if (!setValueResult.IsSuccess)
        {
            return Result.Failure<ProductAttributeAssignmentDto>(setValueResult.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish event for filter index sync
        await _messageBus.PublishAsync(new ProductAttributeAssignmentChangedEvent(
            command.ProductId,
            command.VariantId));

        // Create the DTO
        var dto = new ProductAttributeAssignmentDto(
            assignment!.Id,
            assignment.ProductId,
            assignment.AttributeId,
            attribute.Code,
            attribute.Name,
            attribute.Type.ToString(),
            assignment.VariantId,
            assignment.GetTypedValue(),
            assignment.DisplayValue,
            attribute.IsRequired);

        return Result.Success(dto);
    }

    private Result SetValueByType(ProductAttributeAssignment assignment, ProductAttribute attribute, object? value)
    {
        if (value == null)
        {
            // Clear the value - this is valid (setting to empty)
            return Result.Success();
        }

        try
        {
            switch (attribute.Type)
            {
                case AttributeType.Select:
                    {
                        var valueId = ParseGuid(value);
                        if (!valueId.HasValue)
                        {
                            return Result.Failure(Error.Validation("Value", "Invalid value ID format for Select attribute."));
                        }

                        var selectedValue = attribute.Values.FirstOrDefault(v => v.Id == valueId.Value);
                        if (selectedValue == null)
                        {
                            return Result.Failure(Error.Validation("Value", $"Value ID '{valueId}' not found for this attribute."));
                        }

                        assignment.SetSelectValue(valueId.Value, selectedValue.DisplayValue);
                        break;
                    }

                case AttributeType.MultiSelect:
                    {
                        var valueIds = ParseGuidList(value);
                        if (valueIds == null || !valueIds.Any())
                        {
                            return Result.Failure(Error.Validation("Value", "Invalid value IDs format for MultiSelect attribute."));
                        }

                        var displayValues = new List<string>();
                        foreach (var id in valueIds)
                        {
                            var selectedValue = attribute.Values.FirstOrDefault(v => v.Id == id);
                            if (selectedValue == null)
                            {
                                return Result.Failure(Error.Validation("Value", $"Value ID '{id}' not found for this attribute."));
                            }
                            displayValues.Add(selectedValue.DisplayValue);
                        }

                        assignment.SetMultiSelectValue(valueIds, string.Join(", ", displayValues));
                        break;
                    }

                case AttributeType.Text:
                case AttributeType.TextArea:
                case AttributeType.Url:
                    {
                        var textValue = value.ToString();
                        if (attribute.MaxLength.HasValue && textValue?.Length > attribute.MaxLength.Value)
                        {
                            return Result.Failure(Error.Validation("Value", $"Text exceeds maximum length of {attribute.MaxLength}."));
                        }

                        assignment.SetTextValue(textValue ?? string.Empty);
                        break;
                    }

                case AttributeType.Number:
                case AttributeType.Decimal:
                    {
                        if (!TryParseDecimal(value, out var numValue))
                        {
                            return Result.Failure(Error.Validation("Value", "Invalid number format."));
                        }

                        if (attribute.MinValue.HasValue && numValue < attribute.MinValue.Value)
                        {
                            return Result.Failure(Error.Validation("Value", $"Value must be at least {attribute.MinValue}."));
                        }

                        if (attribute.MaxValue.HasValue && numValue > attribute.MaxValue.Value)
                        {
                            return Result.Failure(Error.Validation("Value", $"Value must be at most {attribute.MaxValue}."));
                        }

                        assignment.SetNumberValue(numValue, attribute.Unit);
                        break;
                    }

                case AttributeType.Boolean:
                    {
                        if (!TryParseBool(value, out var boolValue))
                        {
                            return Result.Failure(Error.Validation("Value", "Invalid boolean format."));
                        }

                        assignment.SetBoolValue(boolValue);
                        break;
                    }

                case AttributeType.Date:
                    {
                        if (!TryParseDateTime(value, out var dateValue))
                        {
                            return Result.Failure(Error.Validation("Value", "Invalid date format."));
                        }

                        assignment.SetDateValue(dateValue);
                        break;
                    }

                case AttributeType.DateTime:
                    {
                        if (!TryParseDateTime(value, out var dateTimeValue))
                        {
                            return Result.Failure(Error.Validation("Value", "Invalid datetime format."));
                        }

                        assignment.SetDateTimeValue(dateTimeValue);
                        break;
                    }

                case AttributeType.Color:
                    {
                        var colorValue = value.ToString();
                        if (string.IsNullOrEmpty(colorValue) || !IsValidHexColor(colorValue))
                        {
                            return Result.Failure(Error.Validation("Value", "Invalid color format. Expected hex color (e.g., #FF0000)."));
                        }

                        assignment.SetColorValue(colorValue);
                        break;
                    }

                case AttributeType.Range:
                    {
                        if (!TryParseRange(value, out var min, out var max))
                        {
                            return Result.Failure(Error.Validation("Value", "Invalid range format. Expected object with min and max properties."));
                        }

                        assignment.SetRangeValue(min, max, attribute.Unit);
                        break;
                    }

                case AttributeType.File:
                    {
                        var fileUrl = value.ToString();
                        if (string.IsNullOrEmpty(fileUrl))
                        {
                            return Result.Failure(Error.Validation("Value", "File URL is required."));
                        }

                        assignment.SetFileValue(fileUrl);
                        break;
                    }

                default:
                    return Result.Failure(Error.Validation("Type", $"Unsupported attribute type: {attribute.Type}"));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Validation("Value", $"Error setting value: {ex.Message}"));
        }
    }

    private static Guid? ParseGuid(object? value)
    {
        if (value is Guid guid) return guid;
        if (value is string str && Guid.TryParse(str, out var parsed)) return parsed;
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            if (Guid.TryParse(je.GetString(), out var jsonParsed)) return jsonParsed;
        }
        return null;
    }

    private static List<Guid>? ParseGuidList(object? value)
    {
        if (value is IEnumerable<Guid> guids) return guids.ToList();
        if (value is IEnumerable<string> strings)
        {
            var result = new List<Guid>();
            foreach (var s in strings)
            {
                if (Guid.TryParse(s, out var g)) result.Add(g);
                else return null;
            }
            return result;
        }
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var result = new List<Guid>();
            foreach (var item in je.EnumerateArray())
            {
                if (item.ValueKind == System.Text.Json.JsonValueKind.String && Guid.TryParse(item.GetString(), out var g))
                {
                    result.Add(g);
                }
                else return null;
            }
            return result;
        }
        return null;
    }

    private static bool TryParseDecimal(object? value, out decimal result)
    {
        result = 0;
        if (value is decimal d) { result = d; return true; }
        if (value is int i) { result = i; return true; }
        if (value is long l) { result = l; return true; }
        if (value is double dbl) { result = (decimal)dbl; return true; }
        if (value is float f) { result = (decimal)f; return true; }
        if (value is string s) return decimal.TryParse(s, out result);
        if (value is System.Text.Json.JsonElement je)
        {
            if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                result = je.GetDecimal();
                return true;
            }
            if (je.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return decimal.TryParse(je.GetString(), out result);
            }
        }
        return false;
    }

    private static bool TryParseBool(object? value, out bool result)
    {
        result = false;
        if (value is bool b) { result = b; return true; }
        if (value is string s) return bool.TryParse(s, out result);
        if (value is System.Text.Json.JsonElement je)
        {
            if (je.ValueKind == System.Text.Json.JsonValueKind.True) { result = true; return true; }
            if (je.ValueKind == System.Text.Json.JsonValueKind.False) { result = false; return true; }
            if (je.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return bool.TryParse(je.GetString(), out result);
            }
        }
        return false;
    }

    private static bool TryParseDateTime(object? value, out DateTime result)
    {
        result = DateTime.MinValue;
        if (value is DateTime dt) { result = dt; return true; }
        if (value is DateTimeOffset dto) { result = dto.DateTime; return true; }
        if (value is string s) return DateTime.TryParse(s, out result);
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            return DateTime.TryParse(je.GetString(), out result);
        }
        return false;
    }

    private static bool IsValidHexColor(string color)
    {
        if (string.IsNullOrEmpty(color)) return false;
        if (!color.StartsWith('#')) return false;
        var hex = color.Substring(1);
        return hex.Length is 3 or 6 && hex.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
    }

    private static bool TryParseRange(object? value, out decimal min, out decimal max)
    {
        min = 0;
        max = 0;

        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (je.TryGetProperty("min", out var minProp) && je.TryGetProperty("max", out var maxProp))
            {
                if (minProp.ValueKind == System.Text.Json.JsonValueKind.Number &&
                    maxProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    min = minProp.GetDecimal();
                    max = maxProp.GetDecimal();
                    return true;
                }
            }
        }

        // Try to get min/max from anonymous object via reflection
        var type = value?.GetType();
        if (type != null)
        {
            var minProp = type.GetProperty("Min") ?? type.GetProperty("min");
            var maxProp = type.GetProperty("Max") ?? type.GetProperty("max");
            if (minProp != null && maxProp != null)
            {
                var minVal = minProp.GetValue(value);
                var maxVal = maxProp.GetValue(value);
                if (TryParseDecimal(minVal, out min) && TryParseDecimal(maxVal, out max))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
