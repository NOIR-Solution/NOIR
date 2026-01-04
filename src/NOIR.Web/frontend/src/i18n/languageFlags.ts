import type { SupportedLanguage } from './index'

/**
 * Language flags for visual identification in language switcher components.
 * Centralized here to avoid duplication across components.
 */
export const languageFlags: Record<SupportedLanguage, string> = {
  en: '🇺🇸',
  vi: '🇻🇳',
}
