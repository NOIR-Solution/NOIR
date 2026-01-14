using FluentValidation.Validators;
using NOIR.Application.Common.Validation;

namespace NOIR.Infrastructure.Validation;

/// <summary>
/// Extracts validation metadata from FluentValidation validators.
/// Used by:
/// - OpenAPI schema transformer (Scalar docs)
/// - Validation metadata endpoint (frontend codegen)
/// </summary>
public class ValidationMetadataService : IValidationMetadataService, ISingletonService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationMetadataService> _logger;
    private readonly Lazy<IReadOnlyList<ValidatorMetadata>> _allMetadata;

    public ValidationMetadataService(
        IServiceProvider serviceProvider,
        ILogger<ValidationMetadataService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        // Cache metadata on first access (validators don't change at runtime)
        _allMetadata = new Lazy<IReadOnlyList<ValidatorMetadata>>(ExtractAllMetadata);
    }

    public IReadOnlyList<ValidatorMetadata> GetAllValidatorMetadata() => _allMetadata.Value;

    public ValidatorMetadata? GetValidatorMetadata(string commandName)
    {
        return _allMetadata.Value.FirstOrDefault(v =>
            v.CommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase) ||
            v.CommandFullName.Equals(commandName, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<ValidatorMetadata> GetValidatorMetadata(Func<string, bool> filter)
    {
        return _allMetadata.Value.Where(v => filter(v.CommandName)).ToList();
    }

    public ValidatorMetadata? GetValidatorMetadataForType(Type commandType)
    {
        return _allMetadata.Value.FirstOrDefault(v =>
            v.CommandFullName == commandType.FullName);
    }

    private IReadOnlyList<ValidatorMetadata> ExtractAllMetadata()
    {
        var results = new List<ValidatorMetadata>();

        // Find all IValidator<T> registrations
        // FluentValidation.DependencyInjectionExtensions registers validators as IValidator<T>
        var validatorInterfaceType = typeof(IValidator<>);

        // Scan Application assembly for validator types
        var applicationAssembly = typeof(NOIR.Application.DependencyInjection).Assembly;
        var validatorTypes = applicationAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterfaceType))
            .ToList();

        _logger.LogDebug("Found {Count} validator types in Application assembly", validatorTypes.Count);

        foreach (var validatorType in validatorTypes)
        {
            try
            {
                var metadata = ExtractMetadata(validatorType);
                if (metadata != null)
                {
                    results.Add(metadata);
                    _logger.LogDebug("Extracted metadata for {CommandName}", metadata.CommandName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract metadata from validator {ValidatorType}", validatorType.Name);
            }
        }

        _logger.LogInformation("Extracted validation metadata for {Count} commands", results.Count);
        return results;
    }

    private ValidatorMetadata? ExtractMetadata(Type validatorType)
    {
        // Get the command type from IValidator<TCommand>
        var validatorInterface = validatorType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>));

        if (validatorInterface == null) return null;

        var commandType = validatorInterface.GetGenericArguments().FirstOrDefault();
        if (commandType == null) return null;

        // Create validator instance using DI
        using var scope = _serviceProvider.CreateScope();
        var validator = scope.ServiceProvider.GetService(validatorType) as IValidator;

        if (validator == null)
        {
            // Try to resolve by interface
            validator = scope.ServiceProvider.GetService(validatorInterface) as IValidator;
        }

        if (validator == null)
        {
            _logger.LogDebug("Could not resolve validator {ValidatorType} from DI", validatorType.Name);
            return null;
        }

        // Use FluentValidation's descriptor to extract rules
        var descriptor = validator.CreateDescriptor();
        var fields = new List<ValidationFieldMetadata>();

        foreach (var member in descriptor.GetMembersWithValidators())
        {
            var rules = descriptor.GetRulesForMember(member.Key)
                .SelectMany(ExtractRuleInfo)
                .ToList();

            var propertyInfo = commandType.GetProperty(member.Key);
            var propertyType = propertyInfo?.PropertyType;
            var isRequired = rules.Any(r => r.RuleType is "notEmpty" or "notNull");

            fields.Add(new ValidationFieldMetadata(
                member.Key,
                GetTypeScriptType(propertyType),
                isRequired,
                rules
            ));
        }

        return new ValidatorMetadata(
            commandType.Name,
            commandType.FullName ?? commandType.Name,
            fields
        );
    }

    private static IEnumerable<ValidationRuleInfo> ExtractRuleInfo(IValidationRule rule)
    {
        foreach (var component in rule.Components)
        {
            var ruleInfo = ExtractFromValidator(component.Validator);
            if (ruleInfo != null)
                yield return ruleInfo;
        }
    }

    private static ValidationRuleInfo? ExtractFromValidator(IPropertyValidator validator)
    {
        return validator switch
        {
            INotEmptyValidator => new ValidationRuleInfo("notEmpty"),
            INotNullValidator => new ValidationRuleInfo("notNull"),

            // Length validators - specific types FIRST (they inherit from ILengthValidator)
            IMinimumLengthValidator minLength => new ValidationRuleInfo("minLength", new Dictionary<string, object>
            {
                ["min"] = minLength.Min
            }),
            IMaximumLengthValidator maxLength => new ValidationRuleInfo("maxLength", new Dictionary<string, object>
            {
                ["max"] = maxLength.Max
            }),
            IExactLengthValidator exactLength => new ValidationRuleInfo("exactLength", new Dictionary<string, object>
            {
                ["length"] = exactLength.Max
            }),
            // ILengthValidator as fallback for any other length validators
            ILengthValidator length => new ValidationRuleInfo("length", new Dictionary<string, object>
            {
                ["min"] = length.Min,
                ["max"] = length.Max
            }),

            // Pattern validators
            IRegularExpressionValidator regex => new ValidationRuleInfo("pattern", new Dictionary<string, object>
            {
                ["pattern"] = regex.Expression
            }),
            IEmailValidator => new ValidationRuleInfo("email"),

            // Comparison validators
            IComparisonValidator comparison => new ValidationRuleInfo("comparison", new Dictionary<string, object>
            {
                ["comparison"] = comparison.Comparison.ToString(),
                ["valueToCompare"] = comparison.ValueToCompare ?? new object()
            }),
            IBetweenValidator between => new ValidationRuleInfo("between", new Dictionary<string, object>
            {
                ["from"] = between.From,
                ["to"] = between.To
            }),

            // Other common validators
            ICreditCardValidator => new ValidationRuleInfo("creditCard"),
            IEnumValidator => new ValidationRuleInfo("enum"),

            // Unknown validators - skip (will be validated server-side only)
            _ => null
        };
    }

    private static string GetTypeScriptType(Type? type)
    {
        if (type == null) return "unknown";

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return GetTypeScriptType(underlyingType);
        }

        return type.Name switch
        {
            "String" => "string",
            "Int16" or "Int32" or "Int64" or "UInt16" or "UInt32" or "UInt64" => "number",
            "Single" or "Double" or "Decimal" => "number",
            "Boolean" => "boolean",
            "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly" => "string",
            "Guid" => "string",
            "Byte[]" => "string", // Base64 encoded
            _ when type.IsArray => "array",
            _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) => "array",
            _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>) => "array",
            _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>) => "array",
            _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) => "array",
            _ when type.IsEnum => "string", // Enums serialized as strings
            _ => "object"
        };
    }
}
