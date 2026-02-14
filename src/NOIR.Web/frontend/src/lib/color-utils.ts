/**
 * Color utility functions for safe color handling
 */

/**
 * Sanitizes a color code to prevent XSS attacks.
 * Only allows valid hex colors (#RRGGBB format).
 * @param colorCode The color code to sanitize
 * @param fallback The fallback color if invalid (default: #e5e5e5)
 * @returns A safe hex color code
 */
export const sanitizeColorCode = (
  colorCode: string | undefined | null,
  fallback = '#e5e5e5'
): string => {
  if (!colorCode) return fallback

  // Only allow valid hex colors
  const hexRegex = /^#[0-9A-Fa-f]{6}$/
  if (!hexRegex.test(colorCode)) {
    console.warn(`Invalid color code: ${colorCode}, using fallback`)
    return fallback
  }

  return colorCode
}

/**
 * Determines if a color is light or dark using WCAG luminance calculation.
 * Uses proper gamma correction for accurate contrast determination.
 * @param hexColor The hex color to evaluate
 * @returns 'light' if the text should be dark, 'dark' if text should be light
 */
export const getContrastMode = (hexColor: string): 'light' | 'dark' => {
  const hex = hexColor.replace('#', '')
  const r = parseInt(hex.substring(0, 2), 16) / 255
  const g = parseInt(hex.substring(2, 4), 16) / 255
  const b = parseInt(hex.substring(4, 6), 16) / 255

  // Apply gamma correction (WCAG formula)
  const rsRGB = r <= 0.03928 ? r / 12.92 : Math.pow((r + 0.055) / 1.055, 2.4)
  const gsRGB = g <= 0.03928 ? g / 12.92 : Math.pow((g + 0.055) / 1.055, 2.4)
  const bsRGB = b <= 0.03928 ? b / 12.92 : Math.pow((b + 0.055) / 1.055, 2.4)

  const luminance = 0.2126 * rsRGB + 0.7152 * gsRGB + 0.0722 * bsRGB

  return luminance > 0.179 ? 'light' : 'dark'
}

/**
 * Legacy helper for backward compatibility.
 * @deprecated Use getContrastMode instead
 */
export const isLightColor = (hexColor: string): boolean => {
  return getContrastMode(hexColor) === 'light'
}
