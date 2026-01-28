/**
 * Generate a SKU (Stock Keeping Unit) from a product name
 *
 * Format: {CATEGORY}-{NAME_PREFIX}-{RANDOM}
 * Example: "GEN-BLUE-A1B2"
 *
 * @param productName - The product name to base the SKU on
 * @param categoryCode - Optional category code (defaults to "GEN")
 * @returns Generated SKU string
 */
export function generateSKU(
  productName: string,
  categoryCode?: string
): string {
  // Extract first 4 alphanumeric characters from product name
  const namePrefix = productName
    .toUpperCase()
    .replace(/[^A-Z0-9]/g, '')
    .slice(0, 4)
    .padEnd(4, 'X') // Pad with X if name is too short

  // Generate random suffix (4 characters)
  const randomSuffix = Math.random()
    .toString(36)
    .substring(2, 6)
    .toUpperCase()

  // Use category code or default to "GEN" (general)
  const categoryPrefix = categoryCode
    ? categoryCode.slice(0, 3).toUpperCase().padEnd(3, 'X')
    : 'GEN'

  return `${categoryPrefix}-${namePrefix}-${randomSuffix}`
}

/**
 * Validate a SKU format
 *
 * Expected format: XXX-XXXX-XXXX (3 letters, 4 letters, 4 alphanumeric)
 */
export function isValidSKU(sku: string): boolean {
  return /^[A-Z]{3}-[A-Z0-9]{4}-[A-Z0-9]{4}$/.test(sku.toUpperCase())
}

/**
 * Generate a unique SKU by checking against existing SKUs
 *
 * @param productName - The product name to base the SKU on
 * @param existingSKUs - Array of existing SKUs to avoid duplicates
 * @param categoryCode - Optional category code
 * @param maxAttempts - Maximum attempts to generate unique SKU (default: 10)
 * @returns Unique SKU string or null if unable to generate
 */
export function generateUniqueSKU(
  productName: string,
  existingSKUs: string[],
  categoryCode?: string,
  maxAttempts = 10
): string | null {
  const existingSet = new Set(existingSKUs.map((s) => s.toUpperCase()))

  for (let i = 0; i < maxAttempts; i++) {
    const sku = generateSKU(productName, categoryCode)
    if (!existingSet.has(sku.toUpperCase())) {
      return sku
    }
  }

  return null
}
