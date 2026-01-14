using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using NOIR.Application.Common.Validation;

namespace NOIR.Web.OpenApi;

/// <summary>
/// OpenAPI schema transformer that enriches schemas with FluentValidation rules.
/// This makes validation constraints visible in Scalar API documentation.
/// </summary>
public sealed class FluentValidationSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Get the validation metadata service from DI
        var metadataService = context.ApplicationServices.GetService<IValidationMetadataService>();
        if (metadataService == null) return Task.CompletedTask;

        // Get the type being documented
        var type = context.JsonTypeInfo.Type;
        if (type == null) return Task.CompletedTask;

        // Look for validator metadata for this type
        var validatorMetadata = metadataService.GetValidatorMetadataForType(type);
        if (validatorMetadata == null) return Task.CompletedTask;

        // Apply validation rules to schema properties
        if (schema.Properties == null) return Task.CompletedTask;

        foreach (var field in validatorMetadata.Fields)
        {
            // Try to find the property in the schema (handle PascalCase to camelCase)
            var propertyName = ToCamelCase(field.FieldName);
            if (!schema.Properties.TryGetValue(propertyName, out var propertySchema))
            {
                // Try original casing
                if (!schema.Properties.TryGetValue(field.FieldName, out propertySchema))
                    continue;
            }

            // Mark required fields
            if (field.IsRequired)
            {
                schema.Required ??= new HashSet<string>();
                schema.Required.Add(propertyName);
            }

            // Apply rules to property schema (cast from IOpenApiSchema)
            if (propertySchema is OpenApiSchema concreteSchema)
            {
                ApplyRulesToSchema(concreteSchema, field.Rules);
            }
        }

        return Task.CompletedTask;
    }

    private static void ApplyRulesToSchema(OpenApiSchema schema, IReadOnlyList<ValidationRuleInfo> rules)
    {
        foreach (var rule in rules)
        {
            switch (rule.RuleType)
            {
                case "minLength":
                    if (rule.Parameters?.TryGetValue("min", out var minVal) == true)
                        schema.MinLength = Convert.ToInt32(minVal);
                    break;

                case "maxLength":
                    if (rule.Parameters?.TryGetValue("max", out var maxVal) == true)
                        schema.MaxLength = Convert.ToInt32(maxVal);
                    break;

                case "length":
                    if (rule.Parameters?.TryGetValue("min", out var lengthMin) == true)
                        schema.MinLength = Convert.ToInt32(lengthMin);
                    if (rule.Parameters?.TryGetValue("max", out var lengthMax) == true)
                        schema.MaxLength = Convert.ToInt32(lengthMax);
                    break;

                case "exactLength":
                    if (rule.Parameters?.TryGetValue("length", out var exact) == true)
                    {
                        var exactVal = Convert.ToInt32(exact);
                        schema.MinLength = exactVal;
                        schema.MaxLength = exactVal;
                    }
                    break;

                case "pattern":
                    if (rule.Parameters?.TryGetValue("pattern", out var pattern) == true)
                        schema.Pattern = pattern.ToString();
                    break;

                case "email":
                    schema.Format = "email";
                    break;

                case "comparison":
                    if (rule.Parameters?.TryGetValue("comparison", out var comparison) == true &&
                        rule.Parameters?.TryGetValue("valueToCompare", out var compareVal) == true)
                    {
                        var comparisonType = comparison.ToString();
                        var value = Convert.ToDecimal(compareVal);

                        // In JSON Schema 2020-12 / OpenAPI 3.1, exclusiveMinimum/Maximum are the boundary values
                        // not boolean flags. We use Minimum/Maximum for inclusive boundaries.
                        switch (comparisonType)
                        {
                            case "GreaterThan":
                                schema.ExclusiveMinimum = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                break;
                            case "GreaterThanOrEqual":
                                schema.Minimum = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                break;
                            case "LessThan":
                                schema.ExclusiveMaximum = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                break;
                            case "LessThanOrEqual":
                                schema.Maximum = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                break;
                        }
                    }
                    break;

                case "between":
                    if (rule.Parameters?.TryGetValue("from", out var from) == true)
                        schema.Minimum = Convert.ToDecimal(from).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (rule.Parameters?.TryGetValue("to", out var to) == true)
                        schema.Maximum = Convert.ToDecimal(to).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;

                case "creditCard":
                    schema.Format = "credit-card";
                    break;
            }
        }
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str[1..];
    }
}
