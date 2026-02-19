/**
 * Currency formatting utilities
 * Shared across e-commerce pages
 */

/**
 * Map currency codes to appropriate locales
 */
const currencyLocaleMap: Record<string, string> = {
  VND: 'vi-VN',
  USD: 'en-US',
  EUR: 'de-DE',
  GBP: 'en-GB',
  JPY: 'ja-JP',
  KRW: 'ko-KR',
  CNY: 'zh-CN',
  THB: 'th-TH',
  SGD: 'en-SG',
  MYR: 'ms-MY',
}

/**
 * Format currency amount based on locale and currency code
 * Full format with currency symbol (e.g., 1.500.000 ₫)
 */
export const formatCurrency = (amount: number, currency: string = 'VND'): string => {
  const locale = currencyLocaleMap[currency] || 'en-US'

  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 2,
  }).format(amount)
}

/**
 * Abbreviated VND format for KPI cards and compact displays
 * Examples: 1.5B₫, 2.3M₫, 500K₫, 999₫
 */
export const formatVndAbbreviated = (amount: number): string => {
  const abs = Math.abs(amount)
  const sign = amount < 0 ? '-' : ''

  if (abs >= 1_000_000_000) return `${sign}${(abs / 1_000_000_000).toFixed(1)}B₫`
  if (abs >= 1_000_000) return `${sign}${(abs / 1_000_000).toFixed(1)}M₫`
  if (abs >= 1_000) return `${sign}${(abs / 1_000).toFixed(0)}K₫`
  return `${sign}${abs}₫`
}

/**
 * Compact VND format for tables — number only, no currency symbol
 * Uses vi-VN grouping (e.g., 1.500.000)
 */
export const formatVndCompact = (amount: number): string =>
  new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 0 }).format(amount)
