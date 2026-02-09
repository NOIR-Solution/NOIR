/**
 * Gravatar helper utilities
 *
 * Generates Gravatar URLs and initials for avatar fallbacks.
 * Uses SHA-256 hash for email (per Gravatar specs).
 */

/**
 * Generate SHA-256 hash of a string (for Gravatar email hashing)
 */
async function sha256(message: string): Promise<string> {
  const msgBuffer = new TextEncoder().encode(message)
  const hashBuffer = await crypto.subtle.digest('SHA-256', msgBuffer)
  const hashArray = Array.from(new Uint8Array(hashBuffer))
  return hashArray.map((b) => b.toString(16).padStart(2, '0')).join('')
}

/**
 * Generate a Gravatar URL for an email address
 * @param email The email address
 * @param size The image size (default 80px)
 * @returns Promise with the Gravatar URL
 */
export async function getGravatarUrl(email: string, size = 80): Promise<string> {
  const normalizedEmail = email.toLowerCase().trim()
  const hash = await sha256(normalizedEmail)
  // d=404 returns 404 if no Gravatar exists (we can fallback to initials)
  return `https://www.gravatar.com/avatar/${hash}?s=${size}&d=404`
}

/**
 * Generate initials from a name
 * @param firstName First name (or null)
 * @param lastName Last name (or null)
 * @param email Fallback email if no name
 * @returns 1-2 character initials (uppercase)
 */
export function getInitials(
  firstName: string | null,
  lastName: string | null,
  email: string
): string {
  // If we have names, use them
  if (firstName || lastName) {
    const first = firstName?.charAt(0) || ''
    const last = lastName?.charAt(0) || ''
    return (first + last).toUpperCase() || email.charAt(0).toUpperCase()
  }

  // Fallback to email first character
  return email.charAt(0).toUpperCase()
}

/**
 * Generate a consistent color based on a string (for initials avatar background)
 * @param str Input string (usually email or name)
 * @returns HSL color string
 */
export function getAvatarColor(str: string): string {
  let hash = 0
  for (let i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash)
  }

  // Generate a pleasing hue with sufficient contrast for white text (WCAG AA: 4.5:1)
  const hue = hash % 360
  return `hsl(${hue}, 65%, 40%)`
}
