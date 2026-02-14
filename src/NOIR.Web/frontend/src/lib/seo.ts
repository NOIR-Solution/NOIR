/**
 * SEO utilities for meta title/description handling
 * Based on research: docs/backend/research/seo-meta-and-hint-text-best-practices.md
 */

// Character limits based on industry best practices
export const SEO_LIMITS = {
  title: {
    min: 30,
    optimal: 50,
    max: 60,
  },
  description: {
    min: 70,
    optimal: 120,
    max: 160,
  },
} as const

/**
 * Get the appropriate color class for character counter based on count
 * - Empty: neutral gray
 * - Too short (<50% of optimal): warning amber
 * - Optimal range: success green
 * - Near limit (between optimal and max): warning amber
 * - Over limit: error red
 */
export const getCharCountColor = (
  current: number,
  optimal: number,
  max: number
): string => {
  if (current === 0) return 'text-muted-foreground'
  if (current < optimal * 0.5) return 'text-amber-500'
  if (current <= optimal) return 'text-green-600 dark:text-green-500'
  if (current <= max) return 'text-amber-500'
  return 'text-destructive'
}

/**
 * Generate effective meta title from post title and site name
 * Used when metaTitle field is empty
 */
export const generateMetaTitle = (
  postTitle: string,
  siteName: string = 'NOIR'
): string => {
  if (!postTitle) return ''

  const separator = ' | '
  const maxTitleLength = SEO_LIMITS.title.max - siteName.length - separator.length

  const truncatedTitle = postTitle.length > maxTitleLength
    ? postTitle.substring(0, maxTitleLength - 1).trimEnd() + '…'
    : postTitle

  return `${truncatedTitle}${separator}${siteName}`
}

/**
 * Generate effective meta description from excerpt or content
 * Used when metaDescription field is empty
 */
export const generateMetaDescription = (
  excerpt?: string | null,
  content?: string | null
): string => {
  // Priority 1: Use excerpt if available
  if (excerpt && excerpt.trim()) {
    return truncateToWords(excerpt.trim(), SEO_LIMITS.description.max)
  }

  // Priority 2: Use first part of content (strip HTML)
  if (content && content.trim()) {
    const plainText = stripHtml(content)
    return truncateToWords(plainText, SEO_LIMITS.description.max)
  }

  return ''
}

/**
 * Strip HTML tags from content
 */
const stripHtml = (html: string): string => {
  // Create a temporary element to parse HTML
  const temp = document.createElement('div')
  temp.innerHTML = html
  return temp.textContent || temp.innerText || ''
}

/**
 * Truncate text to approximate character limit, ending at word boundary
 */
const truncateToWords = (text: string, maxLength: number): string => {
  if (text.length <= maxLength) return text

  // Find the last space before maxLength
  const truncated = text.substring(0, maxLength)
  const lastSpace = truncated.lastIndexOf(' ')

  if (lastSpace > maxLength * 0.7) {
    return truncated.substring(0, lastSpace) + '…'
  }

  return truncated.trimEnd() + '…'
}

/**
 * Get the status label for character count
 */
export const getCharCountStatus = (
  current: number,
  optimal: number,
  max: number
): string | null => {
  if (current === 0) return null
  if (current < optimal * 0.5) return 'Too short'
  if (current <= optimal) return 'Good'
  if (current <= max) return 'Near limit'
  return 'Too long'
}
