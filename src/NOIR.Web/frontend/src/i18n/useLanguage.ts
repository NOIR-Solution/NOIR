import { useContext } from 'react'
import { LanguageContext, type LanguageContextType } from './LanguageContext'

/**
 * Hook to access language context
 * Must be used within a LanguageProvider
 */
export const useLanguage = (): LanguageContextType => {
  const context = useContext(LanguageContext)
  if (context === undefined) {
    throw new Error('useLanguage must be used within a LanguageProvider')
  }
  return context
}
