import { useTranslation } from 'react-i18next'
import { Check } from 'lucide-react'
import { useLanguage } from './useLanguage'
import { languageFlags } from './languageFlags'
import type { SupportedLanguage } from './index'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

interface LanguageSwitcherProps {
  /** Show native language names instead of English names */
  showNativeName?: boolean
  /** Additional CSS classes */
  className?: string
  /** Render as dropdown or buttons */
  variant?: 'dropdown' | 'buttons'
}

/**
 * Language Switcher - 21st.dev inspired design
 * Features: Glassmorphism dropdown, smooth animations, accessible
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
                'flex items-center gap-2 px-3 py-1.5 rounded-xl text-sm font-medium',
                'transition-all duration-200',
                currentLanguage === code
                  ? 'bg-gradient-to-r from-blue-600 to-cyan-600 text-white shadow-md'
                  : 'bg-accent hover:bg-accent/80 text-foreground'
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
            'flex items-center gap-2 rounded-xl',
            'text-muted-foreground hover:text-foreground hover:bg-accent',
            'transition-all duration-200',
            className
          )}
          aria-label={t('labels.selectLanguage')}
        >
          <span className="text-base">{languageFlags[currentLanguage]}</span>
          <span className="text-sm font-medium hidden sm:inline">{currentLang.nativeName}</span>
        </Button>
      </DropdownMenuTrigger>

      <DropdownMenuContent
        align="end"
        className="w-44 backdrop-blur-xl bg-background/95 border-border/50"
      >
        {(Object.entries(languages) as [SupportedLanguage, typeof languages[SupportedLanguage]][]).map(
          ([code, lang]) => (
            <DropdownMenuItem
              key={code}
              onClick={() => handleChange(code)}
              className={cn(
                'flex items-center justify-between rounded-lg',
                'transition-all duration-200',
                currentLanguage === code && 'bg-accent'
              )}
            >
              <div className="flex items-center gap-2">
                <span className="text-base">{languageFlags[code]}</span>
                <span className="text-sm font-medium">{lang.nativeName}</span>
              </div>
              {currentLanguage === code && (
                <Check className="h-4 w-4 text-blue-600" />
              )}
            </DropdownMenuItem>
          )
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
