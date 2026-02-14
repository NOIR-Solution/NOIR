/**
 * Validation Message Translation Utility
 *
 * Translates hardcoded English validation messages from Zod schemas
 * to localized messages using i18next.
 */

/**
 * Translation function type - accepts any key and optional params
 */
type TranslationFn = (key: string, params?: Record<string, unknown>) => string

/**
 * Pattern matchers for validation messages from generated Zod schemas.
 * Each matcher extracts parameters (like numbers) from the message.
 */
const validationPatterns = [
  {
    // "This field is required"
    pattern: /^This field is required$/,
    key: 'validation.required',
  },
  {
    // "Invalid email address"
    pattern: /^Invalid email address$/,
    key: 'validation.invalidEmail',
  },
  {
    // "Invalid format"
    pattern: /^Invalid format$/,
    key: 'validation.invalidFormat',
  },
  {
    // "Minimum X characters required"
    pattern: /^Minimum (\d+) characters required$/,
    key: 'validation.minLength',
    extractParams: (match: RegExpMatchArray) => ({ count: match[1] }),
  },
  {
    // "Maximum X characters allowed"
    pattern: /^Maximum (\d+) characters allowed$/,
    key: 'validation.maxLength',
    extractParams: (match: RegExpMatchArray) => ({ count: match[1] }),
  },
  {
    // "Must be exactly X characters"
    pattern: /^Must be exactly (\d+) characters$/,
    key: 'validation.exactLength',
    extractParams: (match: RegExpMatchArray) => ({ count: match[1] }),
  },
  {
    // "Must be at least X"
    pattern: /^Must be at least (\d+)$/,
    key: 'validation.minValue',
    extractParams: (match: RegExpMatchArray) => ({ value: match[1] }),
  },
  {
    // "Must be at most X"
    pattern: /^Must be at most (\d+)$/,
    key: 'validation.maxValue',
    extractParams: (match: RegExpMatchArray) => ({ value: match[1] }),
  },
  {
    // "Please confirm your new password"
    pattern: /^Please confirm your new password$/,
    key: 'validation.passwordConfirm',
  },
  {
    // "Passwords don't match"
    pattern: /^Passwords don't match$/,
    key: 'validation.passwordsMismatch',
  },
  {
    // "New password must be different from current password"
    pattern: /^New password must be different from current password$/,
    key: 'validation.passwordSameAsCurrent',
  },
  {
    // "New email must be different from current email"
    pattern: /^New email must be different from current email$/,
    key: 'validation.emailSameAsCurrent',
  },
]

/**
 * Translates a validation error message using i18next.
 *
 * @param message - The original validation message (usually in English)
 * @param t - The i18next translation function from useTranslation('common')
 * @returns The translated message, or the original if no translation found
 *
 * @example
 * ```tsx
 * const { t } = useTranslation('common')
 * const translatedError = translateValidationError(error.message, t)
 * ```
 */
export const translateValidationError = (
  message: string | undefined,
  t: TranslationFn
): string => {
  if (!message) return ''

  for (const { pattern, key, extractParams } of validationPatterns) {
    const match = message.match(pattern)
    if (match) {
      const params = extractParams ? extractParams(match) : {}
      return t(key, params)
    }
  }

  // Return original message if no pattern matches
  return message
}

/**
 * Hook-friendly version that creates a translation function
 * bound to the provided t function.
 *
 * @param t - The i18next translation function from useTranslation('common')
 * @returns A function that translates validation messages
 *
 * @example
 * ```tsx
 * const { t } = useTranslation('common')
 * const translateError = createValidationTranslator(t)
 *
 * // In JSX:
 * {error && <p>{translateError(error.message)}</p>}
 * ```
 */
export const createValidationTranslator = (t: TranslationFn) => {
  return (message: string | undefined) => translateValidationError(message, t)
}
