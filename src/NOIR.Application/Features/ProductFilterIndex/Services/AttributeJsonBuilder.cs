namespace NOIR.Application.Features.ProductFilterIndex.Services;

/// <summary>
/// Builds AttributesJson for ProductFilterIndex from product attribute assignments.
/// Shared logic for both incremental sync (handler) and batch reindex (job).
/// </summary>
public class AttributeJsonBuilder : IScopedService
{
    /// <summary>
    /// Builds the AttributesJson string from a list of attribute assignments.
    /// </summary>
    public string BuildAttributesJson(IReadOnlyList<ProductAttributeAssignment> assignments)
    {
        if (assignments == null || assignments.Count == 0)
            return "{}";

        var attributesDict = new Dictionary<string, object>();

        foreach (var assignment in assignments)
        {
            var code = assignment.Attribute.Code;
            var value = GetFilterableValue(assignment);

            if (value != null)
            {
                // For multi-select, accumulate values as array
                if (assignment.Attribute.Type == AttributeType.MultiSelect)
                {
                    if (attributesDict.TryGetValue(code, out var existing) && existing is List<object> list)
                    {
                        if (value is List<object> values)
                            list.AddRange(values);
                        else
                            list.Add(value);
                    }
                    else
                    {
                        attributesDict[code] = value is List<object> v ? v : new List<object> { value };
                    }
                }
                else
                {
                    attributesDict[code] = value;
                }
            }
        }

        return System.Text.Json.JsonSerializer.Serialize(attributesDict);
    }

    private static object? GetFilterableValue(ProductAttributeAssignment assignment)
    {
        return assignment.Attribute.Type switch
        {
            AttributeType.Text => assignment.TextValue,
            AttributeType.Number => assignment.NumberValue,
            AttributeType.Boolean => assignment.BoolValue,
            AttributeType.Date => assignment.DateValue?.ToString("yyyy-MM-dd"),
            AttributeType.DateTime => assignment.DateTimeValue?.ToString("o"),
            AttributeType.Color => assignment.ColorValue,
            AttributeType.Select => assignment.DisplayValue,
            AttributeType.MultiSelect => ParseMultiSelectValues(assignment.DisplayValue),
            AttributeType.Range => assignment.MinRangeValue.HasValue && assignment.MaxRangeValue.HasValue
                ? new { min = assignment.MinRangeValue.Value, max = assignment.MaxRangeValue.Value }
                : null,
            _ => assignment.DisplayValue
        };
    }

    private static List<object>? ParseMultiSelectValues(string? displayValue)
    {
        if (string.IsNullOrEmpty(displayValue))
            return null;

        return displayValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => (object)v.Trim())
            .ToList();
    }
}
