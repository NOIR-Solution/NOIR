import { createContext, useCallback, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { supportedLanguages, type SupportedLanguage, LANGUAGE_COOKIE_NAME } from './index'

// Context type definition
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

// Create the context
export const LanguageContext = createContext<LanguageContextType | undefined>(
  undefined
)

interface LanguageProviderProps {
  children: ReactNode
}

/**
 * Provider component for language context
 * Wraps the application to provide language switching functionality
 */
export function LanguageProvider({ children }: LanguageProviderProps) {
  const { i18n } = useTranslation()

  const currentLanguage = (i18n.resolvedLanguage ||
    i18n.language ||
    'en') as SupportedLanguage

  const changeLanguage = useCallback(
    async (language: SupportedLanguage) => {
      // Validate language is supported
      if (!(language in supportedLanguages)) {
        console.warn(`Language "${language}" is not supported`)
        return
      }

      // Change language in i18next (also saves to localStorage and cookie via detection config)
      await i18n.changeLanguage(language)

      // Explicitly set cookie for backend synchronization (belt and suspenders)
      // Cookie expires in 1 year
      const expiryDate = new Date()
      expiryDate.setFullYear(expiryDate.getFullYear() + 1)
      document.cookie = `${LANGUAGE_COOKIE_NAME}=${language};expires=${expiryDate.toUTCString()};path=/;SameSite=Lax`

      // Update HTML lang attribute for accessibility
      document.documentElement.lang = language

      // Update text direction if needed (for RTL languages in the future)
      document.documentElement.dir = supportedLanguages[language].dir
    },
    [i18n]
  )

  const isCurrentLanguage = useCallback(
    (language: SupportedLanguage) => currentLanguage === language,
    [currentLanguage]
  )

  const value: LanguageContextType = {
    currentLanguage,
    languages: supportedLanguages,
    changeLanguage,
    isCurrentLanguage,
  }

  return (
    <LanguageContext.Provider value={value}>
      {children}
    </LanguageContext.Provider>
  )
}
