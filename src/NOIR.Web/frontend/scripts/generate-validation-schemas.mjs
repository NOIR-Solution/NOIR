#!/usr/bin/env node
/**
 * Generates Zod validation schemas from FluentValidation metadata.
 * This creates type-safe client-side validation that matches the server.
 *
 * Usage:
 *   node scripts/generate-validation-schemas.mjs
 *   npm run generate:validation
 */

import { writeFileSync } from "node:fs"
import { resolve, dirname } from "node:path"
import { fileURLToPath } from "node:url"

const __filename = fileURLToPath(import.meta.url)
const __dirname = dirname(__filename)
const frontendDir = resolve(__dirname, "..")
const outputFile = resolve(frontendDir, "src/validation/schemas.generated.ts")

const BACKEND_URL = "http://localhost:4000/api/validation/metadata"

/**
 * @typedef {Object} ValidationRuleInfo
 * @property {string} ruleType
 * @property {Object<string, any>} [parameters]
 * @property {string} [messageKey]
 */

/**
 * @typedef {Object} ValidationFieldMetadata
 * @property {string} fieldName
 * @property {string} fieldType
 * @property {boolean} isRequired
 * @property {ValidationRuleInfo[]} rules
 */

/**
 * @typedef {Object} ValidatorMetadata
 * @property {string} commandName
 * @property {string} commandFullName
 * @property {ValidationFieldMetadata[]} fields
 */

/**
 * Convert PascalCase command name to a schema name
 * e.g., "CreateTenantCommand" -> "createTenantSchema"
 * @param {string} commandName
 * @returns {string}
 */
function toSchemaName(commandName) {
  // Remove "Command" suffix and convert to camelCase + "Schema"
  const baseName = commandName.replace(/Command$/, "")
  const camelCase = baseName.charAt(0).toLowerCase() + baseName.slice(1)
  return `${camelCase}Schema`
}

/**
 * Convert PascalCase to camelCase
 * @param {string} str
 * @returns {string}
 */
function toCamelCase(str) {
  if (!str || str.charAt(0) === str.charAt(0).toLowerCase()) return str
  return str.charAt(0).toLowerCase() + str.slice(1)
}

/**
 * Generate Zod schema code for a field based on its type and validation rules
 * @param {ValidationFieldMetadata} field
 * @returns {string}
 */
function generateFieldSchema(field) {
  const rules = field.rules || []
  const parts = []

  // Start with base type
  switch (field.fieldType) {
    case "string":
      parts.push("z.string()")
      break
    case "number":
      parts.push("z.number()")
      break
    case "boolean":
      parts.push("z.boolean()")
      break
    case "array":
      parts.push("z.array(z.unknown())")
      break
    case "object":
      parts.push("z.record(z.string(), z.unknown())")
      break
    default:
      parts.push("z.unknown()")
  }

  // Apply validation rules
  for (const rule of rules) {
    switch (rule.ruleType) {
      case "notEmpty":
        if (field.fieldType === "string") {
          parts.push(".min(1, { message: 'This field is required' })")
        }
        break

      case "minLength":
        if (rule.parameters?.min != null) {
          parts.push(`.min(${rule.parameters.min}, { message: 'Minimum ${rule.parameters.min} characters required' })`)
        }
        break

      case "maxLength":
        if (rule.parameters?.max != null) {
          parts.push(`.max(${rule.parameters.max}, { message: 'Maximum ${rule.parameters.max} characters allowed' })`)
        }
        break

      case "length":
        if (rule.parameters?.min != null && rule.parameters?.max != null) {
          parts.push(`.min(${rule.parameters.min}, { message: 'Minimum ${rule.parameters.min} characters required' })`)
          parts.push(`.max(${rule.parameters.max}, { message: 'Maximum ${rule.parameters.max} characters allowed' })`)
        }
        break

      case "exactLength":
        if (rule.parameters?.length != null) {
          parts.push(`.length(${rule.parameters.length}, { message: 'Must be exactly ${rule.parameters.length} characters' })`)
        }
        break

      case "pattern":
        if (rule.parameters?.pattern) {
          // Escape backslashes for JavaScript regex
          const pattern = rule.parameters.pattern.replace(/\\/g, "\\\\")
          parts.push(`.regex(/${pattern}/, { message: 'Invalid format' })`)
        }
        break

      case "email":
        parts.push(".email({ message: 'Invalid email address' })")
        break

      case "comparison":
        // Only apply numeric comparisons if valueToCompare is a primitive number
        // Skip if it's an object (e.g., comparison to another property - handled server-side only)
        if (rule.parameters?.comparison && rule.parameters?.valueToCompare != null) {
          const value = rule.parameters.valueToCompare
          const isNumericValue = typeof value === "number" || (typeof value === "string" && !isNaN(Number(value)))
          
          if (isNumericValue) {
            const numValue = Number(value)
            switch (rule.parameters.comparison) {
              case "GreaterThan":
                parts.push(`.gt(${numValue}, { message: 'Must be greater than ${numValue}' })`)
                break
              case "GreaterThanOrEqual":
                parts.push(`.gte(${numValue}, { message: 'Must be at least ${numValue}' })`)
                break
              case "LessThan":
                parts.push(`.lt(${numValue}, { message: 'Must be less than ${numValue}' })`)
                break
              case "LessThanOrEqual":
                parts.push(`.lte(${numValue}, { message: 'Must be at most ${numValue}' })`)
                break
            }
          }
          // Skip NotEqual and other comparisons to non-numeric values (handled server-side)
        }
        break

      case "between":
        if (rule.parameters?.from != null && rule.parameters?.to != null) {
          parts.push(`.gte(${rule.parameters.from}, { message: 'Must be at least ${rule.parameters.from}' })`)
          parts.push(`.lte(${rule.parameters.to}, { message: 'Must be at most ${rule.parameters.to}' })`)
        }
        break

      // Skip notNull - handled by required
      // Skip creditCard, enum - can be added later
    }
  }

  // Handle optional fields (not required)
  if (!field.isRequired) {
    // For strings, allow empty strings or undefined
    if (field.fieldType === "string") {
      parts.push(".optional().or(z.literal(''))")
    } else {
      parts.push(".optional()")
    }
  }

  return parts.join("")
}

/**
 * Generate complete Zod schema for a command validator
 * @param {ValidatorMetadata} validator
 * @returns {string}
 */
function generateValidatorSchema(validator) {
  const schemaName = toSchemaName(validator.commandName)
  const lines = []

  lines.push(`/**`)
  lines.push(` * Validation schema for ${validator.commandName}`)
  lines.push(` * @generated from FluentValidation - DO NOT EDIT`)
  lines.push(` */`)
  lines.push(`export const ${schemaName} = z.object({`)

  for (const field of validator.fields) {
    const fieldName = toCamelCase(field.fieldName)
    const fieldSchema = generateFieldSchema(field)
    lines.push(`  ${fieldName}: ${fieldSchema},`)
  }

  lines.push(`})`)
  lines.push(``)
  lines.push(`export type ${schemaName.charAt(0).toUpperCase() + schemaName.slice(1, -6)}Input = z.infer<typeof ${schemaName}>`)
  lines.push(``)

  return lines.join("\n")
}

/**
 * Generate the complete TypeScript file with all schemas
 * @param {ValidatorMetadata[]} validators
 * @returns {string}
 */
function generateFile(validators) {
  const header = `/**
 * Zod Validation Schemas
 * 
 * Auto-generated from FluentValidation rules.
 * DO NOT EDIT - run 'npm run generate:validation' to regenerate.
 * 
 * @generated ${new Date().toISOString()}
 */

import { z } from "zod"

`

  const schemas = validators.map(generateValidatorSchema).join("\n")

  // Generate a map for easy lookup
  const schemaMapEntries = validators
    .map(v => `  "${v.commandName}": ${toSchemaName(v.commandName)},`)
    .join("\n")

  const schemaMap = `
/**
 * Map of command names to their validation schemas
 */
export const validationSchemas = {
${schemaMapEntries}
} as const

export type ValidatedCommandName = keyof typeof validationSchemas
`

  return header + schemas + schemaMap
}

async function main() {
  console.log(`Fetching validation metadata from ${BACKEND_URL}...`)
  console.log("Make sure the backend is running: dotnet run --project src/NOIR.Web\n")

  try {
    const response = await fetch(BACKEND_URL)

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`)
    }

    /** @type {ValidatorMetadata[]} */
    const validators = await response.json()

    console.log(`Found ${validators.length} validators`)

    // Generate the file content
    const content = generateFile(validators)

    // Write to file
    writeFileSync(outputFile, content, "utf-8")

    console.log(`\n✓ Generated: ${outputFile}`)
    console.log(`  - ${validators.length} schemas generated`)
  } catch (error) {
    console.error(`\n✗ Failed to generate schemas: ${error.message}`)

    const msg = error.message.toLowerCase()
    if (msg.includes("econnrefused") || msg.includes("fetch failed")) {
      console.error("\nTroubleshooting:")
      console.error("  1. Ensure backend is running: dotnet run --project src/NOIR.Web")
      console.error(`  2. Verify the URL is accessible: ${BACKEND_URL}`)
    }

    process.exit(1)
  }
}

main()
