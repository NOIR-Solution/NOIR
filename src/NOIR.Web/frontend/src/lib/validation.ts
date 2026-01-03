/**
 * Validation utilities for form inputs
 */

/**
 * Email validation regex
 * Matches basic email format: user@domain.tld
 */
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/**
 * Validates email address format
 * @param email Email address to validate
 * @returns true if valid email format
 */
export function isValidEmail(email: string): boolean {
  return EMAIL_REGEX.test(email)
}

/**
 * Validates that a string is not empty after trimming
 * @param value String to check
 * @returns true if string has content
 */
export function isNotEmpty(value: string): boolean {
  return value.trim().length > 0
}
