import type { EnterpriseTableSettings } from '@/types/enterprise-table'
import { createDefaultSettings } from '@/types/enterprise-table'

const STORAGE_KEY_PREFIX = 'noir:enterprise-table'
const CURRENT_VERSION = 3

/**
 * Load settings from localStorage with migration
 */
export const loadEnterpriseSettings = (
  tableKey: string,
  columnIds: string[],
  defaultPinLeft: string[] = ['actions', 'select'],
  columnMeta?: Record<string, { defaultHidden?: boolean }>
): EnterpriseTableSettings => {
  try {
    const raw = localStorage.getItem(`${STORAGE_KEY_PREFIX}:${tableKey}`)

    if (!raw) {
      return createDefaultSettings(columnIds, defaultPinLeft, columnMeta)
    }

    const parsed = JSON.parse(raw) as Partial<EnterpriseTableSettings> & { version?: number }

    // Migration from older versions
    const migrated = migrateSettings(parsed, columnIds, defaultPinLeft)

    // Ensure all columns exist in settings
    const withNewColumns = ensureAllColumns(migrated, columnIds, columnMeta)

    return withNewColumns
  } catch {
    return createDefaultSettings(columnIds, defaultPinLeft, columnMeta)
  }
}

/**
 * Save settings to localStorage
 */
export const saveEnterpriseSettings = (
  tableKey: string,
  settings: EnterpriseTableSettings
): void => {
  try {
    localStorage.setItem(
      `${STORAGE_KEY_PREFIX}:${tableKey}`,
      JSON.stringify({ ...settings, version: CURRENT_VERSION })
    )
  } catch {
    // localStorage may be unavailable or quota exceeded
    console.warn('Failed to save table settings')
  }
}

/**
 * Clear settings for a table
 */
export const clearEnterpriseSettings = (tableKey: string): void => {
  try {
    localStorage.removeItem(`${STORAGE_KEY_PREFIX}:${tableKey}`)
  } catch {
    // ignore
  }
}

/**
 * Migrate settings from older versions
 */
const migrateSettings = (
  parsed: Partial<EnterpriseTableSettings> & { version?: number },
  columnIds: string[],
  defaultPinLeft: string[]
): EnterpriseTableSettings => {
  const version = parsed.version || 1
  const defaults = createDefaultSettings(columnIds, defaultPinLeft)

  // Version 1 -> 2: Added columnSizing, columnPinning
  if (version < 2) {
    // Legacy v1 had only visibility and order
    return {
      ...defaults,
      columnVisibility: (parsed as Record<string, unknown>).columnVisibility as Record<string, boolean>
        || defaults.columnVisibility,
      columnOrder: (parsed as Record<string, unknown>).columnOrder as string[]
        || defaults.columnOrder,
    }
  }

  // Version 2 -> 3: Added grouping, expanded, density, showFiltersRow
  if (version < 3) {
    return {
      ...defaults,
      columnVisibility: parsed.columnVisibility || defaults.columnVisibility,
      columnOrder: parsed.columnOrder || defaults.columnOrder,
      columnSizing: parsed.columnSizing || defaults.columnSizing,
      columnPinning: parsed.columnPinning || defaults.columnPinning,
    }
  }

  // Version 3: Current
  return {
    ...defaults,
    ...parsed,
    version: CURRENT_VERSION,
  }
}

/**
 * Ensure all columns exist in settings (handle new columns)
 */
const ensureAllColumns = (
  settings: EnterpriseTableSettings,
  columnIds: string[],
  columnMeta?: Record<string, { defaultHidden?: boolean }>
): EnterpriseTableSettings => {
  const existingIds = new Set(settings.columnOrder)
  const newColumnIds = columnIds.filter(id => !existingIds.has(id))

  if (newColumnIds.length === 0) {
    return settings
  }

  // Add new columns to visibility (respect defaultHidden from column meta)
  const newVisibility = Object.fromEntries(
    newColumnIds.map(id => [id, columnMeta?.[id]?.defaultHidden === true ? false : true])
  )

  // Add new columns to order (at the end, before any right-pinned)
  const rightPinnedIndex = settings.columnOrder.findIndex(
    id => settings.columnPinning.right.includes(id)
  )

  const insertIndex = rightPinnedIndex === -1
    ? settings.columnOrder.length
    : rightPinnedIndex

  const newOrder = [
    ...settings.columnOrder.slice(0, insertIndex),
    ...newColumnIds,
    ...settings.columnOrder.slice(insertIndex),
  ]

  return {
    ...settings,
    columnVisibility: { ...settings.columnVisibility, ...newVisibility },
    columnOrder: newOrder,
  }
}

/**
 * Check if settings have been customized from default
 */
export const checkIsCustomized = (
  settings: EnterpriseTableSettings,
  defaultSettings: EnterpriseTableSettings
): boolean => {
  // Check column order
  if (JSON.stringify(settings.columnOrder) !== JSON.stringify(defaultSettings.columnOrder)) {
    return true
  }

  // Check visibility
  for (const [key, value] of Object.entries(settings.columnVisibility)) {
    if (defaultSettings.columnVisibility[key] !== value) {
      return true
    }
  }

  // Check sizing
  if (Object.keys(settings.columnSizing).length > 0) {
    return true
  }

  // Check pinning (beyond defaults)
  const defaultLeft = new Set(defaultSettings.columnPinning.left)
  const currentLeft = settings.columnPinning.left
  if (currentLeft.some(id => !defaultLeft.has(id))) {
    return true
  }
  if (settings.columnPinning.right.length > 0) {
    return true
  }

  // Check grouping
  if (settings.grouping.length > 0) {
    return true
  }

  if (settings.showFiltersRow) {
    return true
  }

  return false
}
