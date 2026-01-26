/**
 * Slug generation utilities
 * Shared across content management pages
 */

/**
 * Generate URL-friendly slug from text
 * Handles Vietnamese diacritics and special characters
 *
 * @param text - The text to convert to a slug
 * @returns URL-friendly slug string
 *
 * @example
 * generateSlug('Hello World') // 'hello-world'
 * generateSlug('Sản phẩm mới') // 'san-pham-moi'
 * generateSlug('Product #1 (New!)') // 'product-1-new'
 */
export function generateSlug(text: string): string {
  return text
    .toLowerCase()
    .normalize('NFD') // Decompose combined characters (e.g., ă → a + ̆)
    .replace(/[\u0300-\u036f]/g, '') // Remove diacritical marks
    .replace(/đ/g, 'd') // Handle Vietnamese đ specifically
    .replace(/[^a-z0-9\s-]/g, '') // Remove non-alphanumeric except spaces and hyphens
    .trim()
    .replace(/\s+/g, '-') // Replace spaces with hyphens
    .replace(/-+/g, '-') // Replace multiple hyphens with single hyphen
}

/**
 * Validate if a string is a valid slug format
 *
 * @param slug - The slug to validate
 * @returns true if valid, false otherwise
 */
export function isValidSlug(slug: string): boolean {
  const slugRegex = /^[a-z0-9]+(?:-[a-z0-9]+)*$/
  return slugRegex.test(slug)
}
