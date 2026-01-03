import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import HttpApi from 'i18next-http-backend';

/**
 * Supported languages configuration
 * Add new languages here when expanding localization
 */
export const supportedLanguages = {
  en: { name: 'English', nativeName: 'English', dir: 'ltr' },
  vi: { name: 'Vietnamese', nativeName: 'Tiếng Việt', dir: 'ltr' },
} as const;

export type SupportedLanguage = keyof typeof supportedLanguages;

export const defaultLanguage: SupportedLanguage = 'en';

/**
 * Available namespaces for translation files
 * Each namespace corresponds to a JSON file in /locales/{lang}/{namespace}.json
 */
export const namespaces = ['common', 'auth', 'errors', 'nav'] as const;
export type Namespace = (typeof namespaces)[number];

/**
 * LocalStorage key for persisting language preference
 */
export const LANGUAGE_STORAGE_KEY = 'noir-language';

/**
 * Cookie name for backend synchronization
 * Must match the backend constant in JsonLocalizationService
 */
export const LANGUAGE_COOKIE_NAME = 'noir-language';

/**
 * Initialize i18next with:
 * - HTTP backend for loading JSON translation files
 * - Browser language detection with localStorage persistence
 * - React integration
 */
i18n
  // Load translations from /locales/{lang}/{namespace}.json
  .use(HttpApi)
  // Detect user language from browser/localStorage
  .use(LanguageDetector)
  // Pass i18n instance to react-i18next
  .use(initReactI18next)
  .init({
    // Fallback language when translation not found
    fallbackLng: defaultLanguage,

    // Supported languages (prevents loading unsupported languages)
    supportedLngs: Object.keys(supportedLanguages),

    // Namespaces configuration
    ns: namespaces as unknown as string[],
    defaultNS: 'common',

    // Backend configuration for loading JSON files
    backend: {
      loadPath: '/locales/{{lng}}/{{ns}}.json',
    },

    // Language detection configuration
    detection: {
      // Detection order: cookie, localStorage, then browser language
      order: ['cookie', 'localStorage', 'navigator', 'htmlTag'],
      // LocalStorage key for saving language preference
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
      // Cookie name for backend sync
      lookupCookie: LANGUAGE_COOKIE_NAME,
      // Cache language in both cookie and localStorage
      caches: ['cookie', 'localStorage'],
      // Cookie options
      cookieMinutes: 525600, // 1 year
      cookieDomain: window.location.hostname,
    },

    // React specific options
    react: {
      // Use Suspense for async loading
      useSuspense: true,
    },

    // Interpolation options
    interpolation: {
      // React already escapes values, no need for i18next to do it
      escapeValue: false,
    },

    // Debug mode (disable in production)
    debug: import.meta.env.DEV,
  });

export default i18n;
