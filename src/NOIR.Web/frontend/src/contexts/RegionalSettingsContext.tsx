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
import { createContext, useContext, useEffect, useCallback, useRef, type ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useAuthContext } from './AuthContext'
import { getRegionalSettings, type RegionalSettingsDto } from '@/services/tenantSettings'
import { useLanguage } from '@/i18n/useLanguage'
import { i18n, type SupportedLanguage, supportedLanguages } from '@/i18n'
import { tenantSettingsKeys } from '@/portal-app/settings/queries/queryKeys'

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
// Match Vietnamese locale: seeder + backend handler use same defaults
const DEFAULT_TIMEZONE = 'Asia/Ho_Chi_Minh'
const DEFAULT_DATE_FORMAT = 'DD/MM/YYYY'
const DEFAULT_LANGUAGE = 'vi'

/**
 * Convert date format pattern to Intl.DateTimeFormat options
 */
const getDateFormatOptions = (pattern: string): Intl.DateTimeFormatOptions => {
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
export const getLocaleForFormat = (pattern: string): string => {
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

export const RegionalSettingsProvider = ({ children }: RegionalSettingsProviderProps) => {
  const { isAuthenticated, user } = useAuthContext()
  const { changeLanguage } = useLanguage()
  // Track if we've already applied the tenant language default to avoid loops
  const appliedTenantLanguageRef = useRef(false)
  // Use refs for callback values
  const changeLanguageRef = useRef(changeLanguage)

  // Keep refs in sync
  useEffect(() => { changeLanguageRef.current = changeLanguage }, [changeLanguage])

  // Platform Admin has tenantId = null — skip tenant-scoped API calls
  const tenantId = user?.tenantId

  const { data: regionalData, isLoading: loading, refetch } = useQuery({
    queryKey: tenantSettingsKeys.regional(),
    queryFn: () => getRegionalSettings(),
    enabled: isAuthenticated && !!tenantId,
    staleTime: 5 * 60_000,
  })

  // Apply language side effects when data changes
  useEffect(() => {
    if (!regionalData) return

    // Language override logic:
    // When tenant admin changes Default Language, override ALL users' current settings.
    // After that, if user changes via profile menu, respect it until next admin change.
    // We track the last applied tenant language to detect admin changes.
    const lastAppliedTenantLanguage = localStorage.getItem(TENANT_LANGUAGE_KEY)
    const tenantLanguageChanged = lastAppliedTenantLanguage !== regionalData.language

    if (tenantLanguageChanged && !appliedTenantLanguageRef.current) {
      // Tenant language differs from what we last applied → admin changed it
      // Override user's current setting
      if (regionalData.language in supportedLanguages) {
        changeLanguageRef.current(regionalData.language as SupportedLanguage)
        localStorage.setItem(TENANT_LANGUAGE_KEY, regionalData.language)
        appliedTenantLanguageRef.current = true
      }
    }
  }, [regionalData])

  // Reset language tracking when auth/tenant changes
  useEffect(() => {
    if (!isAuthenticated || !tenantId) {
      appliedTenantLanguageRef.current = false
    }
  }, [isAuthenticated, tenantId])

  const reloadRegional = useCallback(async () => {
    // Reset the ref to allow language re-application on intentional reload (e.g., after admin save)
    appliedTenantLanguageRef.current = false
    await refetch()
  }, [refetch])

  const regional = regionalData ?? null

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
    const locale = getLocaleForFormat(dateFormat)

    try {
      return d.toLocaleTimeString(locale, {
        hour: '2-digit',
        minute: '2-digit',
        timeZone: timezone,
      })
    } catch {
      return d.toLocaleTimeString(locale, {
        hour: '2-digit',
        minute: '2-digit',
      })
    }
  }, [timezone, dateFormat])

  const formatRelativeTime = useCallback((date: Date | string): string => {
    const d = typeof date === 'string' ? new Date(date) : date
    const now = new Date()
    const diffMs = now.getTime() - d.getTime()
    const diffSeconds = Math.floor(diffMs / 1000)
    const diffMinutes = Math.floor(diffSeconds / 60)
    const diffHours = Math.floor(diffMinutes / 60)
    const diffDays = Math.floor(diffHours / 24)

    if (diffSeconds < 60) {
      return i18n.t('time.now', { ns: 'common', defaultValue: 'Just now' })
    } else if (diffMinutes < 60) {
      return i18n.t('time.minutesAgoShort', { ns: 'common', count: diffMinutes, defaultValue: '{{count}}m ago' })
    } else if (diffHours < 24) {
      return i18n.t('time.hoursAgoShort', { ns: 'common', count: diffHours, defaultValue: '{{count}}h ago' })
    } else if (diffDays === 1) {
      return i18n.t('time.yesterday', { ns: 'common', defaultValue: 'Yesterday' })
    } else if (diffDays < 7) {
      return i18n.t('time.daysAgoShort', { ns: 'common', count: diffDays, defaultValue: '{{count}}d ago' })
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

export const useRegionalSettings = () => {
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
export const useRegionalSettingsOptional = () => {
  return useContext(RegionalSettingsContext)
}
