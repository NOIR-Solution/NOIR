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
export const generateSKU = (
  productName: string,
  categoryCode?: string
): string => {
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

