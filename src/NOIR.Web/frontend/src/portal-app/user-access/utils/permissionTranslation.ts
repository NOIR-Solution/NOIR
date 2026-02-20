import type { TFunction } from 'i18next'

/**
 * Translate a permission category name using locale keys.
 * Falls back to the raw category string if no translation exists.
 */
export const translatePermissionCategory = (t: TFunction, category: string): string => {
  const categoryKey = category.toLowerCase().replace(/\s+/g, '')
  return t(`permissions.categories.${categoryKey}`, category)
}

/**
 * Translate a permission displayName using locale keys.
 * Key format: permissions.displayNames.{resource}.{action}
 * Permission name format: "resource:action" (e.g., "users:read")
 */
export const translatePermissionDisplayName = (t: TFunction, permName: string, fallback: string): string => {
  const key = permName.replace(':', '.')
  return t(`permissions.displayNames.${key}`, fallback)
}

/**
 * Translate a permission description using locale keys.
 * Returns undefined if the original description is falsy.
 */
export const translatePermissionDescription = (t: TFunction, permName: string, description?: string | null): string | undefined => {
  if (!description) return undefined
  const key = permName.replace(':', '.')
  return t(`permissions.descriptions.${key}`, description)
}
