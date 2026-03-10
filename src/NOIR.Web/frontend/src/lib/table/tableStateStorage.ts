import type { TableColumnPreferences } from '@/types/table'

const STORAGE_KEY_PREFIX = 'noir-table-state'
/**
 * Bump this when the stored schema becomes incompatible.
 * Old values with a lower version are silently discarded.
 */
const STORAGE_VERSION = 1

/**
 * Persist column visibility, order, and sizing preferences to localStorage.
 *
 * NEVER persist: sorting, pagination, filters, row selection — these are ephemeral.
 */
export const saveTableColumnPreferences = (
  pageKey: string,
  prefs: Omit<TableColumnPreferences, '_v'>,
): void => {
  try {
    const value: TableColumnPreferences = { ...prefs, _v: STORAGE_VERSION }
    localStorage.setItem(`${STORAGE_KEY_PREFIX}:${pageKey}`, JSON.stringify(value))
  } catch {
    // localStorage may be unavailable (private mode, quota exceeded)
  }
}

export const loadTableColumnPreferences = (
  pageKey: string,
): Omit<TableColumnPreferences, '_v'> | null => {
  try {
    const raw = localStorage.getItem(`${STORAGE_KEY_PREFIX}:${pageKey}`)
    if (!raw) return null
    const parsed: TableColumnPreferences = JSON.parse(raw)
    if (parsed._v !== STORAGE_VERSION) return null
    const { _v: _ignored, ...rest } = parsed
    return rest
  } catch {
    return null
  }
}

export const clearTableColumnPreferences = (pageKey: string): void => {
  try {
    localStorage.removeItem(`${STORAGE_KEY_PREFIX}:${pageKey}`)
  } catch {
    // ignore
  }
}
