import { useEffect, useRef, useCallback, useState } from 'react'
import { type UseFormReturn, type FieldValues } from 'react-hook-form'
import { toast } from '@/lib/toast'
import { i18n } from '@/i18n'

interface DraftData<T> {
  data: T
  savedAt: number
}

interface UseAutoSaveOptions<T extends FieldValues> {
  /** React Hook Form instance */
  form: UseFormReturn<T>
  /** Unique key for localStorage (e.g., 'blog-post-draft', 'product-draft-{id}') */
  storageKey: string
  /** Debounce delay in milliseconds (default: 2000ms) */
  debounceMs?: number
  /** Whether auto-save is enabled (default: true) */
  enabled?: boolean
  /** Maximum age of draft in milliseconds (default: 24 hours) */
  maxAge?: number
  /** Callback when draft is restored */
  onRestore?: (data: Partial<T>) => void
  /** Custom message for draft restored toast */
  restoreMessage?: string
}

interface UseAutoSaveReturn {
  /** Clear the saved draft */
  clearDraft: () => void
  /** Whether a draft exists */
  hasDraft: boolean
  /** When the draft was last saved */
  lastSavedAt: Date | null
  /** Manually save draft now */
  saveDraft: () => void
}

const DEFAULT_MAX_AGE = 24 * 60 * 60 * 1000 // 24 hours

/**
 * Hook for auto-saving form drafts to localStorage
 *
 * Features:
 * - Debounced auto-save on form changes
 * - Draft restoration on mount
 * - Automatic expiration of old drafts
 * - Toast notification with discard option
 *
 * @example
 * const { clearDraft, hasDraft } = useAutoSave({
 *   form,
 *   storageKey: `blog-post-draft-${postId || 'new'}`,
 *   onRestore: (data) => form.reset(data),
 * })
 *
 * // Clear draft on successful save
 * onSubmit: async (data) => {
 *   await savePost(data)
 *   clearDraft()
 * }
 */
export const useAutoSave = <T extends FieldValues>({
  form,
  storageKey,
  debounceMs = 2000,
  enabled = true,
  maxAge = DEFAULT_MAX_AGE,
  onRestore,
  restoreMessage = i18n.t('autoSave.draftRestored', { ns: 'common', defaultValue: 'Draft restored' }),
}: UseAutoSaveOptions<T>): UseAutoSaveReturn => {
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const lastSavedRef = useRef<string>('')
  const [hasDraft, setHasDraft] = useState(false)
  const [lastSavedAt, setLastSavedAt] = useState<Date | null>(null)

  // Restore draft on mount
  useEffect(() => {
    if (!enabled) return

    try {
      const saved = localStorage.getItem(storageKey)
      if (saved) {
        const parsed: DraftData<T> = JSON.parse(saved)
        const { data, savedAt } = parsed

        // Check if draft is still valid (not too old)
        const age = Date.now() - savedAt
        if (age < maxAge) {
          setHasDraft(true)
          setLastSavedAt(new Date(savedAt))

          // Show restoration toast
          toast.info(restoreMessage, {
            description: i18n.t('autoSave.draftFrom', { ns: 'common', date: new Date(savedAt).toLocaleString(), defaultValue: 'From {{date}}' }),
            action: {
              label: i18n.t('autoSave.discard', { ns: 'common', defaultValue: 'Discard' }),
              onClick: () => {
                localStorage.removeItem(storageKey)
                setHasDraft(false)
                setLastSavedAt(null)
                form.reset()
              },
            },
            duration: 10000, // 10 seconds to decide
          })

          // Restore the data
          onRestore?.(data)
        } else {
          // Draft is too old, remove it
          localStorage.removeItem(storageKey)
        }
      }
    } catch {
      // Invalid stored data, remove it
      localStorage.removeItem(storageKey)
    }
    // Only run on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [storageKey, enabled])

  // Watch for changes and auto-save
  const formValues = form.watch()
  const isDirty = form.formState.isDirty

  useEffect(() => {
    if (!enabled || !isDirty) return

    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current)
    }

    timeoutRef.current = setTimeout(() => {
      const serialized = JSON.stringify(formValues)

      // Only save if data actually changed
      if (serialized !== lastSavedRef.current) {
        const draftData: DraftData<T> = {
          data: formValues as T,
          savedAt: Date.now(),
        }
        localStorage.setItem(storageKey, JSON.stringify(draftData))
        lastSavedRef.current = serialized
        setHasDraft(true)
        setLastSavedAt(new Date())
      }
    }, debounceMs)

    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current)
      }
    }
  }, [formValues, storageKey, debounceMs, enabled, isDirty])

  // Clear draft (call on successful save)
  const clearDraft = useCallback(() => {
    localStorage.removeItem(storageKey)
    lastSavedRef.current = ''
    setHasDraft(false)
    setLastSavedAt(null)
  }, [storageKey])

  // Manual save
  const saveDraft = useCallback(() => {
    const draftData: DraftData<T> = {
      data: form.getValues() as T,
      savedAt: Date.now(),
    }
    localStorage.setItem(storageKey, JSON.stringify(draftData))
    lastSavedRef.current = JSON.stringify(form.getValues())
    setHasDraft(true)
    setLastSavedAt(new Date())
  }, [form, storageKey])

  return {
    clearDraft,
    hasDraft,
    lastSavedAt,
    saveDraft,
  }
}
