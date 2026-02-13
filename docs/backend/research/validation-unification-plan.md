# Validation Unification Plan

> **Goal**: Single source of truth for validation with real-time frontend validation using identical backend rules.

## Executive Summary

This plan describes how to unify validation across the NOIR stack:
- **Backend**: FluentValidation remains the source of truth
- **API Docs**: OpenAPI/Scalar automatically shows validation constraints (min/max, patterns, required)
- **API Runtime**: Exposes validation metadata via a dedicated endpoint for codegen
- **Frontend**: Auto-generated Zod schemas + react-hook-form for real-time validation

**Key Benefits**:
1. Zero duplication - frontend gets exact same rules as backend
2. Real-time validation - instant feedback as users type
3. Type-safe - Zod infers TypeScript types from schemas
4. Maintainable - change rules once in backend, regenerate frontend
5. Self-documenting API - Scalar shows validation rules to API consumers

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     SINGLE SOURCE OF TRUTH                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│                        FluentValidation                                  │
│                     AbstractValidator<T>                                 │
│                              │                                           │
│              ┌───────────────┼───────────────┐                          │
│              │               │               │                          │
│              ▼               ▼               ▼                          │
│       ┌──────────┐    ┌──────────┐    ┌──────────┐                     │
│       │ OpenAPI/ │    │ Metadata │    │ Runtime  │                     │
│       │ Scalar   │    │ Endpoint │    │ Validate │                     │
│       └────┬─────┘    └────┬─────┘    └────┬─────┘                     │
│            │               │               │                            │
│            ▼               ▼               ▼                            │
│       API Docs         Zod Codegen    Server-side                      │
│       (shows rules     (FE schemas)   enforcement                      │
│        to users)                                                        │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                           BUILD PIPELINE                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  1. FluentValidation          2. Metadata              3. Zod Schema    │
│     AbstractValidator<T>  ────> Extraction ────────>   Generation       │
│                                                                          │
│  CreateTenantCommandValidator                          export const     │
│   └─ RuleFor(x => x.Name)     ValidationMetadata       createTenant =   │
│       .NotEmpty()              [                         z.object({     │
│       .MinLength(2)              { field: "Name",          name: z      │
│       .MaxLength(200)              rules: [                 .string()   │
│                                      { type: "notEmpty" },   .min(1)    │
│                                      { type: "minLength",    .min(2)    │
│                                        params: { min: 2 }},  .max(200)  │
│                                      ...                    ...         │
│                                    ]                      })            │
│                                  }                                      │
│                                ]                                        │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                              RUNTIME                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  Frontend Form                                    Backend API            │
│  ┌────────────────────┐                          ┌────────────────────┐ │
│  │ useForm({          │                          │ FluentValidation   │ │
│  │   resolver:        │                          │                    │ │
│  │     zodResolver()  │                          │ Async validations: │ │
│  │   mode: 'onChange' │                          │ - Unique checks    │ │
│  │ })                 │──── onSubmit ──────────> │ - DB lookups       │ │
│  │                    │                          │ - Business rules   │ │
│  │ Real-time:         │ <─── Field errors ────── │                    │ │
│  │ - Required         │     (edge cases only)    │                    │ │
│  │ - Min/Max length   │                          │                    │ │
│  │ - Regex patterns   │                          │                    │ │
│  └────────────────────┘                          └────────────────────┘ │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Implementation Components

### Phase 1: Backend Infrastructure

#### 1.1 OpenAPI/Scalar Integration (FluentValidation → Swagger)

This automatically enriches OpenAPI schema with validation rules from FluentValidation.

**Install Package:**
```bash
cd src/NOIR.Web
dotnet add package MicroElements.Swashbuckle.FluentValidation
```

**Configure in Program.cs:**
```csharp
// src/NOIR.Web/Program.cs (or wherever Swagger is configured)

// Add after services.AddSwaggerGen()
services.AddFluentValidationRulesToSwagger();
```

**What It Does:**

| FluentValidation Rule | OpenAPI Schema |
|----------------------|----------------|
| `.NotEmpty()` | `required: true` |
| `.MinimumLength(2)` | `minLength: 2` |
| `.MaximumLength(100)` | `maxLength: 100` |
| `.Matches("^[a-z]+$")` | `pattern: "^[a-z]+$"` |
| `.EmailAddress()` | `format: "email"` |
| `.GreaterThan(0)` | `minimum: 0, exclusiveMinimum: true` |
| `.LessThanOrEqualTo(100)` | `maximum: 100` |

**Result in Scalar:**

Before:
```yaml
CreateTenantCommand:
  properties:
    identifier:
      type: string
```

After:
```yaml
CreateTenantCommand:
  required: [identifier, name]
  properties:
    identifier:
      type: string
      minLength: 2
      maxLength: 100
      pattern: "^[a-z0-9][a-z0-9-]*[a-z0-9]$"
    name:
      type: string
      minLength: 2
      maxLength: 200
```

---

#### 1.2 Validation Rule Models

```csharp
// src/NOIR.Application/Common/Validation/Models/ValidationRuleMetadata.cs

namespace NOIR.Application.Common.Validation.Models;

public record ValidationFieldMetadata(
    string FieldName,
    string FieldType,
    IReadOnlyList<ValidationRuleInfo> Rules
);

public record ValidationRuleInfo(
    string RuleType,
    Dictionary<string, object>? Parameters = null,
    string? MessageKey = null
);

public record ValidatorMetadata(
    string CommandName,
    string CommandFullName,
    IReadOnlyList<ValidationFieldMetadata> Fields
);
```

#### 1.3 Metadata Extraction Service

```csharp
// src/NOIR.Application/Common/Validation/IValidationMetadataService.cs

namespace NOIR.Application.Common.Validation;

public interface IValidationMetadataService
{
    /// <summary>
    /// Gets metadata for all registered validators
    /// </summary>
    IReadOnlyList<ValidatorMetadata> GetAllValidatorMetadata();

    /// <summary>
    /// Gets metadata for a specific command validator
    /// </summary>
    ValidatorMetadata? GetValidatorMetadata(string commandName);

    /// <summary>
    /// Gets metadata for validators matching a pattern
    /// </summary>
    IReadOnlyList<ValidatorMetadata> GetValidatorMetadata(Func<string, bool> filter);
}
```

#### 1.4 FluentValidation Descriptor Extraction

```csharp
// src/NOIR.Infrastructure/Validation/ValidationMetadataService.cs

namespace NOIR.Infrastructure.Validation;

public class ValidationMetadataService : IValidationMetadataService, IScopedService
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationMetadataService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IReadOnlyList<ValidatorMetadata> GetAllValidatorMetadata()
    {
        var results = new List<ValidatorMetadata>();

        // Get all registered validator types from DI
        var validatorTypes = GetRegisteredValidatorTypes();

        foreach (var validatorType in validatorTypes)
        {
            var metadata = ExtractMetadata(validatorType);
            if (metadata != null)
                results.Add(metadata);
        }

        return results;
    }

    private ValidatorMetadata? ExtractMetadata(Type validatorType)
    {
        // Get the command type from AbstractValidator<TCommand>
        var commandType = validatorType.BaseType?
            .GetGenericArguments()
            .FirstOrDefault();

        if (commandType == null) return null;

        // Create validator instance
        var validator = _serviceProvider.GetService(validatorType) as IValidator;
        if (validator == null) return null;

        // Use FluentValidation's descriptor to extract rules
        var descriptor = validator.CreateDescriptor();
        var fields = new List<ValidationFieldMetadata>();

        foreach (var member in descriptor.GetMembersWithValidators())
        {
            var rules = descriptor.GetRulesForMember(member.Key)
                .SelectMany(ExtractRuleInfo)
                .ToList();

            var propertyType = commandType.GetProperty(member.Key)?.PropertyType;

            fields.Add(new ValidationFieldMetadata(
                member.Key,
                GetTypeScriptType(propertyType),
                rules
            ));
        }

        return new ValidatorMetadata(
            commandType.Name,
            commandType.FullName ?? commandType.Name,
            fields
        );
    }

    private IEnumerable<ValidationRuleInfo> ExtractRuleInfo(IValidationRule rule)
    {
        foreach (var component in rule.Components)
        {
            var ruleInfo = component.Validator switch
            {
                INotEmptyValidator => new ValidationRuleInfo("notEmpty"),
                INotNullValidator => new ValidationRuleInfo("notNull"),
                ILengthValidator length => new ValidationRuleInfo("length", new()
                {
                    ["min"] = length.Min,
                    ["max"] = length.Max
                }),
                IMinimumLengthValidator minLength => new ValidationRuleInfo("minLength", new()
                {
                    ["min"] = minLength.Min
                }),
                IMaximumLengthValidator maxLength => new ValidationRuleInfo("maxLength", new()
                {
                    ["max"] = maxLength.Max
                }),
                IRegularExpressionValidator regex => new ValidationRuleInfo("pattern", new()
                {
                    ["pattern"] = regex.Expression
                }),
                IEmailValidator => new ValidationRuleInfo("email"),
                IComparisonValidator comparison => new ValidationRuleInfo("comparison", new()
                {
                    ["comparison"] = comparison.Comparison.ToString(),
                    ["valueToCompare"] = comparison.ValueToCompare
                }),
                _ => null
            };

            if (ruleInfo != null)
                yield return ruleInfo;
        }
    }

    private static string GetTypeScriptType(Type? type) => type?.Name switch
    {
        "String" => "string",
        "Int32" or "Int64" or "Decimal" or "Double" => "number",
        "Boolean" => "boolean",
        "DateTime" or "DateTimeOffset" or "DateOnly" => "string", // ISO format
        "Guid" => "string",
        _ when type?.IsArray == true => "array",
        _ when type?.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(List<>) => "array",
        _ => "unknown"
    };
}
```

#### 1.5 Validation Metadata Endpoint

```csharp
// src/NOIR.Web/Endpoints/ValidationEndpoints.cs

namespace NOIR.Web.Endpoints;

public class ValidationEndpoints : IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/validation")
            .WithTags("Validation")
            .WithOpenApi();

        group.MapGet("/metadata", GetAllMetadata)
            .WithName("GetValidationMetadata")
            .WithDescription("Get validation metadata for all commands")
            .AllowAnonymous(); // Needed for build-time codegen

        group.MapGet("/metadata/{commandName}", GetMetadataByCommand)
            .WithName("GetValidationMetadataByCommand")
            .AllowAnonymous();
    }

    private static IResult GetAllMetadata(IValidationMetadataService service)
    {
        var metadata = service.GetAllValidatorMetadata();
        return Results.Ok(metadata);
    }

    private static IResult GetMetadataByCommand(
        string commandName,
        IValidationMetadataService service)
    {
        var metadata = service.GetValidatorMetadata(commandName);
        return metadata != null
            ? Results.Ok(metadata)
            : Results.NotFound();
    }
}
```

---

### Phase 2: Frontend Code Generation

#### 2.1 Install Dependencies

```bash
# In frontend directory
pnpm install zod @hookform/resolvers react-hook-form
pnpm install -D tsx
```

#### 2.2 Code Generation Script

```typescript
// src/NOIR.Web/frontend/scripts/generate-validation.ts

import fs from 'fs';
import path from 'path';

interface ValidationRuleInfo {
  ruleType: string;
  parameters?: Record<string, unknown>;
  messageKey?: string;
}

interface ValidationFieldMetadata {
  fieldName: string;
  fieldType: string;
  rules: ValidationRuleInfo[];
}

interface ValidatorMetadata {
  commandName: string;
  commandFullName: string;
  fields: ValidationFieldMetadata[];
}

const API_URL = process.env.VALIDATION_API_URL || 'http://localhost:5000/api/validation/metadata';
const OUTPUT_PATH = path.join(__dirname, '../src/validation/schemas.generated.ts');

async function fetchValidationMetadata(): Promise<ValidatorMetadata[]> {
  const response = await fetch(API_URL);
  if (!response.ok) {
    throw new Error(`Failed to fetch validation metadata: ${response.statusText}`);
  }
  return response.json();
}

function generateZodRule(rule: ValidationRuleInfo): string {
  switch (rule.ruleType) {
    case 'notEmpty':
    case 'notNull':
      return '.min(1, { message: "This field is required" })';
    case 'minLength':
      return `.min(${rule.parameters?.min}, { message: "Minimum ${rule.parameters?.min} characters required" })`;
    case 'maxLength':
      return `.max(${rule.parameters?.max}, { message: "Maximum ${rule.parameters?.max} characters allowed" })`;
    case 'length':
      return `.min(${rule.parameters?.min}).max(${rule.parameters?.max})`;
    case 'pattern':
      return `.regex(/${rule.parameters?.pattern}/, { message: "Invalid format" })`;
    case 'email':
      return '.email({ message: "Invalid email address" })';
    default:
      return ''; // Unknown rules are handled server-side
  }
}

function getBaseZodType(fieldType: string): string {
  switch (fieldType) {
    case 'string':
      return 'z.string()';
    case 'number':
      return 'z.number()';
    case 'boolean':
      return 'z.boolean()';
    case 'array':
      return 'z.array(z.unknown())';
    default:
      return 'z.unknown()';
  }
}

function generateFieldSchema(field: ValidationFieldMetadata): string {
  const baseType = getBaseZodType(field.fieldType);
  const rules = field.rules.map(generateZodRule).filter(Boolean).join('');
  return `${baseType}${rules}`;
}

function generateValidatorSchema(validator: ValidatorMetadata): string {
  const schemaName = validator.commandName.replace('Command', 'Schema');
  const typeName = validator.commandName.replace('Command', 'Input');

  const fields = validator.fields
    .map(field => {
      const camelName = field.fieldName.charAt(0).toLowerCase() + field.fieldName.slice(1);
      return `  ${camelName}: ${generateFieldSchema(field)}`;
    })
    .join(',\n');

  return `
/**
 * Validation schema for ${validator.commandName}
 * Auto-generated from FluentValidation - DO NOT EDIT
 */
export const ${schemaName} = z.object({
${fields}
});

export type ${typeName} = z.infer<typeof ${schemaName}>;
`;
}

async function main() {
  console.log('Fetching validation metadata from API...');
  const metadata = await fetchValidationMetadata();

  console.log(`Found ${metadata.length} validators`);

  const header = `/**
 * AUTO-GENERATED FILE - DO NOT EDIT
 * Generated from FluentValidation metadata
 * Run 'pnpm run generate:validation' to regenerate
 */

import { z } from 'zod';
`;

  const schemas = metadata.map(generateValidatorSchema).join('\n');
  const output = header + schemas;

  // Ensure directory exists
  const dir = path.dirname(OUTPUT_PATH);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }

  fs.writeFileSync(OUTPUT_PATH, output, 'utf-8');
  console.log(`Generated schemas written to ${OUTPUT_PATH}`);
}

main().catch(console.error);
```

#### 2.3 Package.json Scripts

```json
{
  "scripts": {
    "generate:validation": "tsx scripts/generate-validation.ts",
    "prebuild": "pnpm run generate:validation",
    "predev": "pnpm run generate:validation"
  }
}
```

#### 2.4 Generated Output Example

```typescript
// src/NOIR.Web/frontend/src/validation/schemas.generated.ts (auto-generated)

/**
 * AUTO-GENERATED FILE - DO NOT EDIT
 * Generated from FluentValidation metadata
 */

import { z } from 'zod';

/**
 * Validation schema for CreateTenantCommand
 */
export const CreateTenantSchema = z.object({
  identifier: z.string()
    .min(1, { message: "This field is required" })
    .min(2, { message: "Minimum 2 characters required" })
    .max(100, { message: "Maximum 100 characters allowed" })
    .regex(/^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$/, { message: "Invalid format" }),
  name: z.string()
    .min(1, { message: "This field is required" })
    .min(2, { message: "Minimum 2 characters required" })
    .max(200, { message: "Maximum 200 characters allowed" }),
});

export type CreateTenantInput = z.infer<typeof CreateTenantSchema>;

/**
 * Validation schema for RegisterCommand
 */
export const RegisterSchema = z.object({
  email: z.string()
    .min(1, { message: "This field is required" })
    .email({ message: "Invalid email address" }),
  password: z.string()
    .min(1, { message: "This field is required" })
    .min(6, { message: "Minimum 6 characters required" }),
  firstName: z.string()
    .max(100, { message: "Maximum 100 characters allowed" }),
  lastName: z.string()
    .max(100, { message: "Maximum 100 characters allowed" }),
});

export type RegisterInput = z.infer<typeof RegisterSchema>;
```

---

### Phase 3: Frontend Form Integration

#### 3.1 Custom Form Hook

```typescript
// src/NOIR.Web/frontend/src/hooks/useValidatedForm.ts

import { useForm, UseFormProps, UseFormReturn, FieldValues, Path } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { ZodSchema } from 'zod';
import { useState, useCallback } from 'react';
import { ApiError } from '@/services/apiClient';

interface UseValidatedFormOptions<T extends FieldValues> extends Omit<UseFormProps<T>, 'resolver'> {
  schema: ZodSchema<T>;
}

interface UseValidatedFormReturn<T extends FieldValues> extends UseFormReturn<T> {
  /** Server-side errors (for async validations like uniqueness checks) */
  serverErrors: Record<string, string[]>;
  /** Handle API errors and map to form fields */
  handleApiError: (error: unknown) => void;
  /** Clear server errors for a specific field */
  clearServerError: (field: keyof T) => void;
  /** Get combined error message for a field (client + server) */
  getFieldError: (field: Path<T>) => string | undefined;
}

export function useValidatedForm<T extends FieldValues>(
  options: UseValidatedFormOptions<T>
): UseValidatedFormReturn<T> {
  const { schema, ...formOptions } = options;
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});

  const form = useForm<T>({
    ...formOptions,
    resolver: zodResolver(schema),
    mode: 'onChange', // Real-time validation
  });

  const handleApiError = useCallback((error: unknown) => {
    if (error instanceof ApiError && error.hasFieldErrors && error.errors) {
      // Convert PascalCase to camelCase for field names
      const mappedErrors: Record<string, string[]> = {};
      for (const [key, value] of Object.entries(error.errors)) {
        const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
        mappedErrors[camelKey] = value;
      }
      setServerErrors(mappedErrors);
    }
  }, []);

  const clearServerError = useCallback((field: keyof T) => {
    setServerErrors(prev => {
      const next = { ...prev };
      delete next[field as string];
      return next;
    });
  }, []);

  const getFieldError = useCallback((field: Path<T>): string | undefined => {
    // Client-side error takes precedence
    const clientError = form.formState.errors[field]?.message as string | undefined;
    if (clientError) return clientError;

    // Fall back to server error
    const serverError = serverErrors[field as string];
    return serverError?.[0];
  }, [form.formState.errors, serverErrors]);

  return {
    ...form,
    serverErrors,
    handleApiError,
    clearServerError,
    getFieldError,
  };
}
```

#### 3.2 Form Field Component

```typescript
// src/NOIR.Web/frontend/src/components/form/FormField.tsx

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { FieldValues, Path, UseFormReturn } from 'react-hook-form';
import { cn } from '@/lib/utils';

interface FormFieldProps<T extends FieldValues> {
  form: UseFormReturn<T> & {
    getFieldError: (field: Path<T>) => string | undefined;
    clearServerError: (field: keyof T) => void;
  };
  name: Path<T>;
  label: string;
  placeholder?: string;
  type?: string;
  disabled?: boolean;
  hint?: string;
}

export function FormField<T extends FieldValues>({
  form,
  name,
  label,
  placeholder,
  type = 'text',
  disabled,
  hint,
}: FormFieldProps<T>) {
  const error = form.getFieldError(name);
  const hasError = !!error;

  return (
    <div className="space-y-2">
      <Label htmlFor={name}>{label}</Label>
      <Input
        id={name}
        type={type}
        placeholder={placeholder}
        disabled={disabled}
        aria-invalid={hasError}
        className={cn(hasError && 'border-destructive focus-visible:ring-destructive')}
        {...form.register(name, {
          onChange: () => form.clearServerError(name as keyof T),
        })}
      />
      {hasError ? (
        <p className="text-xs text-destructive">{error}</p>
      ) : hint ? (
        <p className="text-xs text-muted-foreground">{hint}</p>
      ) : null}
    </div>
  );
}
```

#### 3.3 Updated TenantForm Example

```typescript
// src/NOIR.Web/frontend/src/pages/portal/admin/tenants/components/TenantForm.tsx (updated)

import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { FormField } from '@/components/form/FormField';
import { useValidatedForm } from '@/hooks/useValidatedForm';
import { CreateTenantSchema, CreateTenantInput } from '@/validation/schemas.generated';
import type { Tenant, CreateTenantRequest } from '@/types';

interface TenantFormProps {
  tenant?: Tenant | null;
  onSubmit: (data: CreateTenantRequest) => Promise<void>;
  onCancel: () => void;
  loading?: boolean;
}

export function TenantForm({ tenant, onSubmit, onCancel, loading }: TenantFormProps) {
  const { t } = useTranslation('common');
  const isEditing = !!tenant;

  const form = useValidatedForm<CreateTenantInput>({
    schema: CreateTenantSchema,
    defaultValues: {
      identifier: tenant?.identifier ?? '',
      name: tenant?.name ?? '',
    },
  });

  const handleSubmit = form.handleSubmit(async (data) => {
    try {
      await onSubmit(data);
    } catch (error) {
      form.handleApiError(error);
    }
  });

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <FormField
        form={form}
        name="identifier"
        label={t('tenants.form.identifier')}
        placeholder={t('tenants.form.identifierPlaceholder')}
        hint={t('tenants.form.identifierHint')}
        disabled={loading}
      />

      <FormField
        form={form}
        name="name"
        label={t('tenants.form.name')}
        placeholder={t('tenants.form.namePlaceholder')}
        disabled={loading}
      />

      <div className="flex justify-end space-x-2 pt-4">
        <Button type="button" variant="outline" onClick={onCancel} disabled={loading}>
          {t('buttons.cancel')}
        </Button>
        <Button type="submit" disabled={loading || !form.formState.isValid}>
          {loading ? t('labels.loading') : isEditing ? t('buttons.update') : t('buttons.create')}
        </Button>
      </div>
    </form>
  );
}
```

---

### Phase 4: Localization Integration

#### 4.1 Generate Message Keys

The codegen script should also output i18n keys that map to the backend localization:

```typescript
// Addition to generate-validation.ts

function generateMessageKey(rule: ValidationRuleInfo, fieldName: string): string {
  // Map to existing backend localization keys
  const baseKey = `validation.${fieldName.toLowerCase()}`;

  switch (rule.ruleType) {
    case 'notEmpty':
    case 'notNull':
      return `${baseKey}.required`;
    case 'minLength':
      return `${baseKey}.minLength`;
    case 'maxLength':
      return `${baseKey}.maxLength`;
    case 'pattern':
      return `${baseKey}.pattern`;
    case 'email':
      return 'validation.email.invalid';
    default:
      return `${baseKey}.${rule.ruleType}`;
  }
}
```

#### 4.2 i18n-Aware Zod Schema

```typescript
// src/NOIR.Web/frontend/src/validation/createLocalizedSchema.ts

import { z } from 'zod';
import i18n from '@/i18n';

export function createLocalizedSchema<T extends z.ZodRawShape>(
  shape: T,
  messageKeys: Record<string, Record<string, string>>
): z.ZodObject<T> {
  // Override error messages with localized versions
  const localizedShape = Object.fromEntries(
    Object.entries(shape).map(([field, schema]) => {
      const fieldKeys = messageKeys[field] || {};
      // Apply custom error map for this field
      return [field, schema];
    })
  ) as T;

  return z.object(localizedShape);
}
```

---

## Implementation Phases

### Phase 1: Backend Infrastructure (2-3 days work)
1. Install & configure `MicroElements.Swashbuckle.FluentValidation` for OpenAPI/Scalar
2. Create validation metadata models
3. Implement `ValidationMetadataService`
4. Add validation metadata endpoint
5. Write unit tests
6. Verify Scalar shows validation rules correctly

### Phase 2: Frontend Codegen (1-2 days work)
1. Install Zod and react-hook-form
2. Create codegen script
3. Add npm scripts for generation
4. Test with dev server running

### Phase 3: Form Migration (3-5 days work)
1. Create `useValidatedForm` hook
2. Create `FormField` component
3. Migrate TenantForm (pilot)
4. Migrate remaining forms one by one

### Phase 4: Localization & Polish (1-2 days work)
1. Connect to existing i18n system
2. Add loading states during validation
3. Add error boundary for schema loading
4. Documentation

---

## Files to Create/Modify

### New Files

| File | Description |
|------|-------------|
| `src/NOIR.Application/Common/Validation/Models/ValidationRuleMetadata.cs` | Rule metadata DTOs |
| `src/NOIR.Application/Common/Validation/IValidationMetadataService.cs` | Service interface |
| `src/NOIR.Infrastructure/Validation/ValidationMetadataService.cs` | Service implementation |
| `src/NOIR.Web/Endpoints/ValidationEndpoints.cs` | API endpoint |
| `frontend/scripts/generate-validation.ts` | Codegen script |
| `frontend/src/validation/schemas.generated.ts` | Auto-generated (gitignored) |
| `frontend/src/hooks/useValidatedForm.ts` | Form hook |
| `frontend/src/components/form/FormField.tsx` | Form field component |

### Modified Files

| File | Changes |
|------|---------|
| `frontend/package.json` | Add dependencies and scripts |
| `frontend/.gitignore` | Add `schemas.generated.ts` |
| `frontend/src/pages/portal/admin/tenants/components/TenantForm.tsx` | Use new form hook |
| (other forms) | Migrate to new pattern |

---

## Edge Cases & Considerations

### 1. Async Validations
Some validations require database lookups (e.g., "identifier must be unique"). These remain server-side only:

```typescript
// Server returns field error for async validation
// { "Identifier": ["A tenant with this identifier already exists"] }

// Form displays it via serverErrors
```

### 2. Conditional Validations
FluentValidation's `When()` conditions need special handling:

```csharp
RuleFor(x => x.EndDate)
    .GreaterThan(x => x.StartDate)
    .When(x => x.EndDate.HasValue);
```

Solution: Generate conditional Zod schemas with `.refine()`:

```typescript
export const DateRangeSchema = z.object({
  startDate: z.string(),
  endDate: z.string().optional(),
}).refine(
  data => !data.endDate || new Date(data.endDate) > new Date(data.startDate),
  { message: 'End date must be after start date', path: ['endDate'] }
);
```

### 3. Build-Time vs Runtime Generation
- **Build-time** (recommended): Generate during `pnpm run build`, commit `schemas.generated.ts`
- **Runtime**: Fetch metadata on app load (adds latency, but always fresh)

Recommendation: Build-time with CI validation that schemas match current API.

### 4. Schema Versioning
Add version hash to detect schema drift:

```typescript
// schemas.generated.ts
export const SCHEMA_VERSION = 'abc123'; // Hash of source validators

// App startup check
if (SCHEMA_VERSION !== await fetchSchemaVersion()) {
  console.warn('Validation schemas may be outdated');
}
```

---

## Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Validation code duplication | High (manual FE rules) | Zero (auto-generated) |
| Time to first validation feedback | ~500ms (API round-trip) | <50ms (local Zod) |
| Forms with real-time validation | ~20% | 100% |
| Rule consistency bugs | Occasional | Zero (single source) |

---

## Questions for Review

1. Do we want runtime fetching as a fallback if generated schemas are missing?
2. Should the codegen run in CI and fail if schemas change unexpectedly?
3. Should we commit `schemas.generated.ts` or regenerate on each build?
