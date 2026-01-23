/**
 * Regional Settings Context
 *
 * Provides tenant regional settings (timezone, language, date format)
 * and applies them globally for date/time formatting.
 *
 * Features:
 * - Fetches regional settings when user is authenticated
 * - Provides timezone for date/time display conversion
 * - Provides date format pattern for consistent date display
 * - Applies tenant language to all users when admin changes it
 * - Respects individual user language changes until next admin update
 */
import { createContext, useContext, useEffect, useState, useCallback, useRef, type ReactNode } from 'react'
import { useAuthContext } from './AuthContext'
import { getRegionalSettings, type RegionalSettingsDto } from '@/services/tenantSettings'
import { useLanguage } from '@/i18n/useLanguage'
import { LANGUAGE_STORAGE_KEY, type SupportedLanguage, supportedLanguages } from '@/i18n'

/**
 * LocalStorage key for tracking the last tenant language applied.
 * Used to detect when tenant admin changes the default language.
 * When the stored value differs from the current tenant language,
 * we know admin changed it and need to override all users.
 */
const TENANT_LANGUAGE_KEY = 'noir-tenant-language'

interface RegionalSettingsContextType {
  /** Current regional settings */
  regional: RegionalSettingsDto | null
  /** Whether regional settings are being loaded */
  loading: boolean
  /** Tenant's timezone (e.g., 'Asia/Ho_Chi_Minh') */
  timezone: string
  /** Tenant's date format (e.g., 'YYYY-MM-DD') */
  dateFormat: string
  /** Tenant's default language */
  defaultLanguage: string
  /** Reload regional settings (call after save) */
  reloadRegional: () => Promise<void>
  /** Format a date according to tenant settings */
  formatDate: (date: Date | string) => string
  /** Format a date and time according to tenant settings */
  formatDateTime: (date: Date | string) => string
  /** Format time only according to tenant settings */
  formatTime: (date: Date | string) => string
  /** Format a relative time (e.g., "2 hours ago") */
  formatRelativeTime: (date: Date | string) => string
}

const RegionalSettingsContext = createContext<RegionalSettingsContextType | undefined>(undefined)

// Default values when not authenticated or settings not loaded
const DEFAULT_TIMEZONE = 'UTC'
const DEFAULT_DATE_FORMAT = 'YYYY-MM-DD'
const DEFAULT_LANGUAGE = 'en'

/**
 * Convert date format pattern to Intl.DateTimeFormat options
 */
function getDateFormatOptions(pattern: string): Intl.DateTimeFormatOptions {
  switch (pattern) {
    case 'MM/DD/YYYY': // US format
      return { month: '2-digit', day: '2-digit', year: 'numeric' }
    case 'DD/MM/YYYY': // EU format
      return { day: '2-digit', month: '2-digit', year: 'numeric' }
    case 'DD.MM.YYYY': // German format
      return { day: '2-digit', month: '2-digit', year: 'numeric' }
    case 'YYYY-MM-DD': // ISO format
    default:
      return { year: 'numeric', month: '2-digit', day: '2-digit' }
  }
}

/**
 * Get locale for date format pattern.
 * Exported so other components can use consistent locale mapping
 * for specialized date/time formatting (e.g., with seconds).
 */
export function getLocaleForFormat(pattern: string): string {
  switch (pattern) {
    case 'MM/DD/YYYY':
      return 'en-US'
    case 'DD/MM/YYYY':
      return 'en-GB'
    case 'DD.MM.YYYY':
      return 'de-DE'
    case 'YYYY-MM-DD':
    default:
      return 'sv-SE' // Swedish locale outputs YYYY-MM-DD
  }
}

interface RegionalSettingsProviderProps {
  children: ReactNode
}

export function RegionalSettingsProvider({ children }: RegionalSettingsProviderProps) {
  const { isAuthenticated, user } = useAuthContext()
  const { changeLanguage } = useLanguage()
  const [regional, setRegional] = useState<RegionalSettingsDto | null>(null)
  const [loading, setLoading] = useState(false)
  // Track if we've already applied the tenant language default to avoid loops
  const appliedTenantLanguageRef = useRef(false)
  // Use refs for callback values
  const changeLanguageRef = useRef(changeLanguage)

  // Keep refs in sync
  useEffect(() => { changeLanguageRef.current = changeLanguage }, [changeLanguage])

  const loadRegional = useCallback(async () => {
    if (!isAuthenticated) {
      setRegional(null)
      appliedTenantLanguageRef.current = false
      return
    }

    setLoading(true)
    try {
      const settings = await getRegionalSettings()
      setRegional(settings)

      // Language override logic:
      // When tenant admin changes Default Language, override ALL users' current settings.
      // After that, if user changes via profile menu, respect it until next admin change.
      // We track the last applied tenant language to detect admin changes.
      const lastAppliedTenantLanguage = localStorage.getItem(TENANT_LANGUAGE_KEY)
      const tenantLanguageChanged = lastAppliedTenantLanguage !== settings.language

      if (tenantLanguageChanged && !appliedTenantLanguageRef.current) {
        // Tenant language differs from what we last applied → admin changed it
        // Override user's current setting
        if (settings.language in supportedLanguages) {
          await changeLanguageRef.current(settings.language as SupportedLanguage)
          localStorage.setItem(TENANT_LANGUAGE_KEY, settings.language)
          appliedTenantLanguageRef.current = true
        }
      }
    } catch (error) {
      console.error('Failed to load regional settings:', error)
      setRegional(null)
    } finally {
      setLoading(false)
    }
  }, [isAuthenticated])

  // Load regional settings when authentication state or tenant changes
  const tenantId = user?.tenantId
  useEffect(() => {
    loadRegional()
  }, [loadRegional, tenantId])

  const reloadRegional = useCallback(async () => {
    // Reset the ref to allow language re-application on intentional reload (e.g., after admin save)
    appliedTenantLanguageRef.current = false
    await loadRegional()
  }, [loadRegional])

  // Extract settings with defaults
  const timezone = regional?.timezone ?? DEFAULT_TIMEZONE
  const dateFormat = regional?.dateFormat ?? DEFAULT_DATE_FORMAT
  const defaultLanguage = regional?.language ?? DEFAULT_LANGUAGE

  // Date formatting functions
  const formatDate = useCallback((date: Date | string): string => {
    const d = typeof date === 'string' ? new Date(date) : date
    const locale = getLocaleForFormat(dateFormat)
    const options = getDateFormatOptions(dateFormat)

    try {
      return d.toLocaleDateString(locale, { ...options, timeZone: timezone })
    } catch {
      // Fallback if timezone is invalid
      return d.toLocaleDateString(locale, options)
    }
  }, [timezone, dateFormat])

  const formatDateTime = useCallback((date: Date | string): string => {
    const d = typeof date === 'string' ? new Date(date) : date
    const locale = getLocaleForFormat(dateFormat)
    const dateOptions = getDateFormatOptions(dateFormat)

    try {
      return d.toLocaleString(locale, {
        ...dateOptions,
        hour: '2-digit',
        minute: '2-digit',
        timeZone: timezone,
      })
    } catch {
      return d.toLocaleString(locale, {
        ...dateOptions,
        hour: '2-digit',
        minute: '2-digit',
      })
    }
  }, [timezone, dateFormat])

  const formatTime = useCallback((date: Date | string): string => {
    const d = typeof date === 'string' ? new Date(date) : date

    try {
      return d.toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit',
        timeZone: timezone,
      })
    } catch {
      return d.toLocaleTimeString('en-US', {
        hour: '2-digit',
        minute: '2-digit',
      })
    }
  }, [timezone])

  const formatRelativeTime = useCallback((date: Date | string): string => {
    const d = typeof date === 'string' ? new Date(date) : date
    const now = new Date()
    const diffMs = now.getTime() - d.getTime()
    const diffSeconds = Math.floor(diffMs / 1000)
    const diffMinutes = Math.floor(diffSeconds / 60)
    const diffHours = Math.floor(diffMinutes / 60)
    const diffDays = Math.floor(diffHours / 24)

    if (diffSeconds < 60) {
      return 'Just now'
    } else if (diffMinutes < 60) {
      return `${diffMinutes}m ago`
    } else if (diffHours < 24) {
      return `${diffHours}h ago`
    } else if (diffDays === 1) {
      return 'Yesterday'
    } else if (diffDays < 7) {
      return `${diffDays}d ago`
    } else {
      return formatDate(d)
    }
  }, [formatDate])

  return (
    <RegionalSettingsContext.Provider
      value={{
        regional,
        loading,
        timezone,
        dateFormat,
        defaultLanguage,
        reloadRegional,
        formatDate,
        formatDateTime,
        formatTime,
        formatRelativeTime,
      }}
    >
      {children}
    </RegionalSettingsContext.Provider>
  )
}

export function useRegionalSettings() {
  const context = useContext(RegionalSettingsContext)
  if (context === undefined) {
    throw new Error('useRegionalSettings must be used within a RegionalSettingsProvider')
  }
  return context
}

/**
 * Optional hook that doesn't throw if used outside provider
 * Useful for components that may or may not have regional context
 */
export function useRegionalSettingsOptional() {
  return useContext(RegionalSettingsContext)
}
