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

