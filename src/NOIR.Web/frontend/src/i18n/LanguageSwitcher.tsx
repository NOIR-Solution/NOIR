import { Fragment } from 'react';
import { useLanguage } from './LanguageContext';
import type { SupportedLanguage } from './index';

interface LanguageSwitcherProps {
  /** Show native language names instead of English names */
  showNativeName?: boolean;
  /** Additional CSS classes */
  className?: string;
  /** Render as dropdown or buttons */
  variant?: 'dropdown' | 'buttons';
}

/**
 * Language switcher component
 * Allows users to change the application language
 */
export function LanguageSwitcher({
  showNativeName = true,
  className = '',
  variant = 'dropdown',
}: LanguageSwitcherProps) {
  const { currentLanguage, languages, changeLanguage } = useLanguage();

  const handleChange = (language: SupportedLanguage) => {
    changeLanguage(language);
  };

  if (variant === 'buttons') {
    return (
      <div className={`flex gap-2 ${className}`}>
        {(Object.entries(languages) as [SupportedLanguage, typeof languages[SupportedLanguage]][]).map(
          ([code, lang]) => (
            <button
              key={code}
              onClick={() => handleChange(code)}
              className={`px-3 py-1 rounded-md text-sm font-medium transition-colors ${
                currentLanguage === code
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted hover:bg-muted/80 text-muted-foreground'
              }`}
              aria-pressed={currentLanguage === code}
              aria-label={`Switch to ${lang.name}`}
            >
              {showNativeName ? lang.nativeName : lang.name}
            </button>
          )
        )}
      </div>
    );
  }

  return (
    <select
      value={currentLanguage}
      onChange={(e) => handleChange(e.target.value as SupportedLanguage)}
      className={`px-3 py-2 rounded-md border border-input bg-background text-sm focus:outline-none focus:ring-2 focus:ring-ring ${className}`}
      aria-label="Select language"
    >
      {(Object.entries(languages) as [SupportedLanguage, typeof languages[SupportedLanguage]][]).map(
        ([code, lang]) => (
          <option key={code} value={code}>
            {showNativeName ? lang.nativeName : lang.name}
          </option>
        )
      )}
    </select>
  );
}

/**
 * Compact language switcher showing only language codes
 * Useful for headers or tight spaces
 */
export function LanguageSwitcherCompact({ className = '' }: { className?: string }) {
  const { currentLanguage, languages, changeLanguage } = useLanguage();

  const handleChange = (language: SupportedLanguage) => {
    changeLanguage(language);
  };

  return (
    <div className={`flex items-center gap-1 ${className}`}>
      {(Object.keys(languages) as SupportedLanguage[]).map((code, index) => (
        <Fragment key={code}>
          {index > 0 && <span className="text-muted-foreground">|</span>}
          <button
            onClick={() => handleChange(code)}
            className={`px-1 text-sm uppercase transition-colors ${
              currentLanguage === code
                ? 'font-bold text-foreground'
                : 'text-muted-foreground hover:text-foreground'
            }`}
            aria-pressed={currentLanguage === code}
            aria-label={`Switch to ${languages[code].name}`}
          >
            {code}
          </button>
        </Fragment>
      ))}
    </div>
  );
}
