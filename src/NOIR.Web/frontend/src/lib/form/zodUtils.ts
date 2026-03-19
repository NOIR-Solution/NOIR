import { z } from 'zod'

/**
 * Extracts the set of required field names from a Zod object schema.
 *
 * Compatible with Zod v3 and v4.
 * Uses instanceof checks (not internal string typenames) for resilience.
 *
 * A field is "required" if it is NOT wrapped in:
 *   - z.optional() / .optional()
 *   - z.default() / .default()
 *   - .or(z.literal(''))  — URL/string fields that accept empty string
 *
 * Note: .nullable() alone does NOT make a field optional.
 */
export const getRequiredFields = (schema: unknown): Set<string> => {
  if (!schema || typeof schema !== 'object') return new Set()

  const shape = extractShape(schema as z.ZodTypeAny)
  if (!shape) return new Set()

  const required = new Set<string>()
  for (const [key, field] of Object.entries(shape)) {
    if (!isOptionalField(field as z.ZodTypeAny)) {
      required.add(key)
    }
  }
  return required
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Extracts .shape from a ZodObject, unwrapping effects/pipes if needed.
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const extractShape = (schema: z.ZodTypeAny): Record<string, z.ZodTypeAny> | null => {
  if (!schema) return null

  // Direct ZodObject — has .shape property
  if (schema instanceof z.ZodObject) {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return (schema as z.ZodObject<any>).shape as Record<string, z.ZodTypeAny>
  }

  // ZodEffects (z.ZodTransform in v4 / ZodEffects in v3): unwrap .schema
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const def = (schema as any)?._def
  if (!def) return null

  // Try common wrapper patterns
  const inner = def.schema ?? def.in ?? def.type
  if (inner && typeof inner === 'object') {
    return extractShape(inner as z.ZodTypeAny)
  }

  return null
}

/**
 * Returns true if the field can be left empty/unset by the user.
 */
const isOptionalField = (schema: z.ZodTypeAny): boolean => {
  if (!schema) return false

  // ZodOptional — explicit .optional()
  if (schema instanceof z.ZodOptional) return true

  // ZodDefault — has .default() → user doesn't have to fill it
  if (schema instanceof z.ZodDefault) return true

  // ZodNullable — unwrap and check inner (nullable alone ≠ optional)
  if (schema instanceof z.ZodNullable) {
    return isOptionalField(schema.unwrap() as z.ZodTypeAny)
  }

  // ZodUnion — if any branch is optional/literal-empty → optional
  // Handles: z.string().url().or(z.literal('')) patterns
  if (schema instanceof z.ZodUnion) {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const options = (schema._def as any).options as z.ZodTypeAny[]
    for (const opt of options) {
      if (opt instanceof z.ZodLiteral) {
        // .or(z.literal('')) means user can leave field blank
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        if ((opt._def as any).value === '') return true
      }
      if (isOptionalField(opt)) return true
    }
  }

  // ZodPipe (Zod v4 replacement for some ZodEffects) — check input schema
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const def = (schema as any)?._def
  if (def?.typeName === 'ZodPipe' && def?.in) {
    return isOptionalField(def.in as z.ZodTypeAny)
  }

  return false
}
