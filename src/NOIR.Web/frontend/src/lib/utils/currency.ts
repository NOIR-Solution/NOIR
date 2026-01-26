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
 * @param amount - The numeric amount to format
 * @param currency - ISO 4217 currency code (default: 'VND')
 * @returns Formatted currency string
 */
export function formatCurrency(amount: number, currency: string = 'VND'): string {
  const locale = currencyLocaleMap[currency] || 'en-US'

  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: currency,
  }).format(amount)
}

/**
 * Format VND currency (shorthand for Vietnam Dong)
 * @param amount - The numeric amount to format
 * @returns Formatted VND string
 */
export function formatVND(amount: number): string {
  return formatCurrency(amount, 'VND')
}

/**
 * Format USD currency (shorthand for US Dollar)
 * @param amount - The numeric amount to format
 * @returns Formatted USD string
 */
export function formatUSD(amount: number): string {
  return formatCurrency(amount, 'USD')
}
