import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  type ReactNode,
} from 'react'

type Theme = 'light' | 'dark' | 'system' | 'high-contrast'
type ResolvedTheme = 'light' | 'dark' | 'high-contrast'

interface ThemeContextType {
  /** Current theme setting (light, dark, system, or high-contrast) */
  theme: Theme
  /** Actual applied theme after resolving system preference */
  resolvedTheme: ResolvedTheme
  /** Set theme preference */
  setTheme: (theme: Theme) => void
  /** Toggle between light and dark (sets explicit preference) */
  toggleTheme: () => void
  /** Whether the user has explicitly set a theme preference */
  hasUserPreference: boolean
  /** Whether high contrast mode is active */
  isHighContrast: boolean
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined)

const STORAGE_KEY = 'noir-theme'

/**
 * Get system color scheme preference, including high contrast detection
 */
function getSystemTheme(): ResolvedTheme {
  if (typeof window === 'undefined') return 'light'

  // Check high contrast preference first
  if (window.matchMedia('(prefers-contrast: more)').matches) {
    return 'high-contrast'
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches
    ? 'dark'
    : 'light'
}

/**
 * Apply theme class to document
 */
function applyTheme(resolvedTheme: ResolvedTheme) {
  const root = document.documentElement
  root.classList.remove('dark', 'high-contrast')

  if (resolvedTheme === 'dark') {
    root.classList.add('dark')
  } else if (resolvedTheme === 'high-contrast') {
    root.classList.add('high-contrast')
  }
}

interface ThemeProviderProps {
  children: ReactNode
  /** Default theme if no preference is stored */
  defaultTheme?: Theme
}

export function ThemeProvider({
  children,
  defaultTheme = 'system',
}: ThemeProviderProps) {
  const [hasUserPreference, setHasUserPreference] = useState<boolean>(() => {
    if (typeof window === 'undefined') return false
    return localStorage.getItem(STORAGE_KEY) !== null
  })

  const [theme, setThemeState] = useState<Theme>(() => {
    if (typeof window === 'undefined') return defaultTheme
    const stored = localStorage.getItem(STORAGE_KEY) as Theme | null
    return stored || defaultTheme
  })

  const [resolvedTheme, setResolvedTheme] = useState<ResolvedTheme>(() => {
    if (typeof window === 'undefined') return 'light'
    const stored = localStorage.getItem(STORAGE_KEY) as Theme | null
    const currentTheme = stored || defaultTheme
    if (currentTheme === 'system') {
      return getSystemTheme()
    }
    if (currentTheme === 'high-contrast') {
      return 'high-contrast'
    }
    return currentTheme as ResolvedTheme
  })

  // Apply theme on mount and when theme changes
  useEffect(() => {
    let resolved: ResolvedTheme
    if (theme === 'system') {
      resolved = getSystemTheme()
    } else if (theme === 'high-contrast') {
      resolved = 'high-contrast'
    } else {
      resolved = theme
    }
    setResolvedTheme(resolved)
    applyTheme(resolved)
  }, [theme])

  // Listen for system preference changes when theme is 'system'
  useEffect(() => {
    if (theme !== 'system') return

    const darkModeQuery = window.matchMedia('(prefers-color-scheme: dark)')
    const contrastQuery = window.matchMedia('(prefers-contrast: more)')

    const handleChange = () => {
      const newResolved = getSystemTheme()
      setResolvedTheme(newResolved)
      applyTheme(newResolved)
    }

    darkModeQuery.addEventListener('change', handleChange)
    contrastQuery.addEventListener('change', handleChange)

    return () => {
      darkModeQuery.removeEventListener('change', handleChange)
      contrastQuery.removeEventListener('change', handleChange)
    }
  }, [theme])

  const setTheme = useCallback((newTheme: Theme) => {
    setThemeState(newTheme)
    localStorage.setItem(STORAGE_KEY, newTheme)
    setHasUserPreference(true)
  }, [])

  const toggleTheme = useCallback(() => {
    // Cycle: light -> dark -> high-contrast -> light
    // Or simple toggle between light and dark if not using high contrast
    if (resolvedTheme === 'light') {
      setTheme('dark')
    } else if (resolvedTheme === 'dark') {
      setTheme('light')
    } else {
      // From high-contrast, go to light
      setTheme('light')
    }
  }, [resolvedTheme, setTheme])

  const isHighContrast = resolvedTheme === 'high-contrast'

  return (
    <ThemeContext.Provider
      value={{
        theme,
        resolvedTheme,
        setTheme,
        toggleTheme,
        hasUserPreference,
        isHighContrast,
      }}
    >
      {children}
    </ThemeContext.Provider>
  )
}

export function useTheme() {
  const context = useContext(ThemeContext)
  if (context === undefined) {
    throw new Error('useTheme must be used within a ThemeProvider')
  }
  return context
}
