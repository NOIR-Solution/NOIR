import { Moon, Sun } from 'lucide-react'
import { motion } from 'framer-motion'
import { useTranslation } from 'react-i18next'
import { cn } from '@/lib/utils'
import { useTheme } from '@/contexts/ThemeContext'

interface ThemeToggleProps {
  className?: string
}

/**
 * Segmented theme toggle with animated sliding indicator
 * Design from 21st.dev
 */
export const ThemeToggle = ({ className }: ThemeToggleProps) => {
  const { t } = useTranslation('common')
  const { resolvedTheme, setTheme } = useTheme()

  return (
    <div
      className={cn(
        'relative flex items-center rounded-lg border border-border bg-muted p-1',
        className
      )}
    >
      {/* Light button */}
      <button
        onClick={() => setTheme('light')}
        className={cn(
          'relative z-10 flex flex-1 items-center justify-center gap-2 px-3 py-1.5 rounded-md text-sm font-medium transition-colors cursor-pointer',
          resolvedTheme === 'light'
            ? 'text-foreground'
            : 'text-muted-foreground hover:text-foreground'
        )}
        aria-label={t('labels.lightMode', 'Light mode')}
      >
        {resolvedTheme === 'light' && (
          <motion.div
            layoutId="theme-indicator"
            className="absolute inset-0 rounded-md bg-background shadow-sm"
            transition={{
              type: 'spring',
              stiffness: 300,
              damping: 30,
            }}
          />
        )}
        <Sun className="relative z-10 h-4 w-4" />
        <span className="relative z-10">Light</span>
      </button>

      {/* Dark button */}
      <button
        onClick={() => setTheme('dark')}
        className={cn(
          'relative z-10 flex flex-1 items-center justify-center gap-2 px-3 py-1.5 rounded-md text-sm font-medium transition-colors cursor-pointer',
          resolvedTheme === 'dark'
            ? 'text-foreground'
            : 'text-muted-foreground hover:text-foreground'
        )}
        aria-label={t('labels.darkMode', 'Dark mode')}
      >
        {resolvedTheme === 'dark' && (
          <motion.div
            layoutId="theme-indicator"
            className="absolute inset-0 rounded-md bg-background shadow-sm"
            transition={{
              type: 'spring',
              stiffness: 300,
              damping: 30,
            }}
          />
        )}
        <span className="relative z-10">Dark</span>
        <Moon className="relative z-10 h-4 w-4" />
      </button>
    </div>
  )
}

/**
 * Compact icon button for theme toggle (for collapsed sidebar)
 */
export const ThemeToggleCompact = ({ className }: ThemeToggleProps) => {
  const { resolvedTheme, toggleTheme } = useTheme()

  return (
    <button
      onClick={toggleTheme}
      className={cn(
        'relative flex items-center justify-center h-9 w-9 rounded-md border border-input bg-transparent hover:bg-accent hover:text-accent-foreground transition-colors cursor-pointer',
        className
      )}
      aria-label={`Switch to ${resolvedTheme === 'dark' ? 'light' : 'dark'} mode`}
    >
      <Sun
        className={cn(
          'h-4 w-4 transition-all',
          resolvedTheme === 'dark' ? 'scale-0 opacity-0' : 'scale-100 opacity-100'
        )}
      />
      <Moon
        className={cn(
          'absolute h-4 w-4 transition-all',
          resolvedTheme === 'dark' ? 'scale-100 opacity-100' : 'scale-0 opacity-0'
        )}
      />
    </button>
  )
}
