import { useTranslation } from 'react-i18next'
import { Check } from 'lucide-react'
import { useLanguage } from './useLanguage'
import type { SupportedLanguage } from './index'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

// Flag emojis for visual language identification
const languageFlags: Record<SupportedLanguage, string> = {
  en: '🇺🇸',
  vi: '🇻🇳',
}

interface LanguageSwitcherProps {
  /** Show native language names instead of English names */
  showNativeName?: boolean
  /** Additional CSS classes */
  className?: string
  /** Render as dropdown or buttons */
  variant?: 'dropdown' | 'buttons'
}

/**
 * Professional Language Switcher Component
 * Uses Radix UI dropdown for smooth animations and accessibility
 * Inspired by shadcn.io language selector pattern
 */
export function LanguageSwitcher({
  showNativeName = true,
  className = '',
  variant = 'dropdown',
}: LanguageSwitcherProps) {
  const { t } = useTranslation('common')
  const { currentLanguage, languages, changeLanguage } = useLanguage()

  const handleChange = (language: SupportedLanguage) => {
    changeLanguage(language)
  }

  if (variant === 'buttons') {
    return (
      <div className={cn('flex gap-2', className)}>
        {(Object.entries(languages) as [SupportedLanguage, typeof languages[SupportedLanguage]][]).map(
          ([code, lang]) => (
            <button
              key={code}
              onClick={() => handleChange(code)}
              className={cn(
                'flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-medium transition-all duration-200',
                currentLanguage === code
                  ? 'bg-gray-900 text-white shadow-sm'
                  : 'bg-gray-100 hover:bg-gray-200 text-gray-700'
              )}
              aria-pressed={currentLanguage === code}
              aria-label={t('labels.switchToLanguage', { language: lang.name })}
            >
              <span>{languageFlags[code]}</span>
              {showNativeName ? lang.nativeName : lang.name}
            </button>
          )
        )}
      </div>
    )
  }

  // Dropdown variant using Radix UI
  const currentLang = languages[currentLanguage]

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className={cn(
            'flex items-center gap-2 text-gray-600 hover:text-gray-900 hover:bg-gray-100',
            className
          )}
          aria-label={t('labels.selectLanguage')}
        >
          <span className="inline-flex items-center gap-1.5">
            <span>{languageFlags[currentLanguage]}</span>
            <span className="text-sm font-medium">{currentLang.nativeName}</span>
          </span>
        </Button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-40">
        {(Object.entries(languages) as [SupportedLanguage, typeof languages[SupportedLanguage]][]).map(
          ([code, lang]) => (
            <DropdownMenuItem
              key={code}
              onClick={() => handleChange(code)}
              className={cn(
                'flex items-center justify-between cursor-pointer',
                currentLanguage === code && 'bg-gray-50'
              )}
            >
              <div className="flex items-center gap-2">
                <span>{languageFlags[code]}</span>
                <span className="text-sm">{lang.nativeName}</span>
              </div>
              {currentLanguage === code && (
                <Check className="h-4 w-4 text-gray-600" />
              )}
            </DropdownMenuItem>
          )
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

