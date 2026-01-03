import { createContext } from 'react'
import type { SupportedLanguage } from './index'
import { supportedLanguages } from './index'

export interface LanguageContextType {
  /** Current language code (e.g., 'en', 'vi') */
  currentLanguage: SupportedLanguage
  /** List of supported languages with metadata */
  languages: typeof supportedLanguages
  /** Change the current language */
  changeLanguage: (language: SupportedLanguage) => Promise<void>
  /** Check if a language is currently active */
  isCurrentLanguage: (language: SupportedLanguage) => boolean
}

export const LanguageContext = createContext<LanguageContextType | undefined>(
  undefined
)
