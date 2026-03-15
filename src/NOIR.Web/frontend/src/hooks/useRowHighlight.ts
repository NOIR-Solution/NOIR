import { useState, useCallback, useRef } from 'react'

/** Duration in ms for the highlight flash animation */
const HIGHLIGHT_DURATION = 1500
/** Duration in ms for the fade-out animation */
const FADEOUT_DURATION = 400

interface UseRowHighlightReturn {
  /** Set of row IDs currently highlighted (newly created/updated) */
  highlightedIds: Set<string>
  /** Set of row IDs currently fading out (being deleted) */
  fadingOutIds: Set<string>
  /** Mark a row as newly created/updated — triggers highlight pulse */
  highlightRow: (id: string) => void
  /** Mark multiple rows as newly created/updated */
  highlightRows: (ids: string[]) => void
  /**
   * Mark a row as being deleted — triggers fade-out, then calls onComplete.
   * Returns a promise that resolves after the fade-out animation completes.
   */
  fadeOutRow: (id: string) => Promise<void>
  /** Get the animation CSS class for a row ID */
  getRowAnimationClass: (id: string) => string
}

/**
 * Hook for DataTable row animations.
 *
 * - Highlight flash: brief background color pulse for newly created/updated rows
 * - Fade-out: smooth opacity transition for rows being deleted
 *
 * Usage in page components:
 * ```tsx
 * const { highlightRow, fadeOutRow, getRowAnimationClass } = useRowHighlight()
 *
 * // After create mutation succeeds:
 * const createMutation = useCreateEntity()
 * onSuccess: (data) => highlightRow(data.id)
 *
 * // Before delete mutation — animate first, then delete:
 * const handleDelete = async (id: string) => {
 *   await fadeOutRow(id)
 *   deleteMutation.mutate(id)
 * }
 *
 * // Pass to DataTable:
 * <DataTable table={table} getRowAnimationClass={getRowAnimationClass} />
 * ```
 */
export const useRowHighlight = (): UseRowHighlightReturn => {
  const [highlightedIds, setHighlightedIds] = useState<Set<string>>(new Set())
  const [fadingOutIds, setFadingOutIds] = useState<Set<string>>(new Set())
  const timersRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map())

  const highlightRow = useCallback((id: string) => {
    // Clear any existing timer for this ID
    const existing = timersRef.current.get(id)
    if (existing) clearTimeout(existing)

    setHighlightedIds(prev => new Set(prev).add(id))

    const timer = setTimeout(() => {
      setHighlightedIds(prev => {
        const next = new Set(prev)
        next.delete(id)
        return next
      })
      timersRef.current.delete(id)
    }, HIGHLIGHT_DURATION)

    timersRef.current.set(id, timer)
  }, [])

  const highlightRows = useCallback((ids: string[]) => {
    ids.forEach(id => highlightRow(id))
  }, [highlightRow])

  const fadeOutRow = useCallback((id: string): Promise<void> => {
    return new Promise(resolve => {
      setFadingOutIds(prev => new Set(prev).add(id))

      setTimeout(() => {
        setFadingOutIds(prev => {
          const next = new Set(prev)
          next.delete(id)
          return next
        })
        resolve()
      }, FADEOUT_DURATION)
    })
  }, [])

  const getRowAnimationClass = useCallback((id: string): string => {
    if (highlightedIds.has(id)) return 'animate-row-highlight'
    if (fadingOutIds.has(id)) return 'animate-row-fadeout'
    return ''
  }, [highlightedIds, fadingOutIds])

  return {
    highlightedIds,
    fadingOutIds,
    highlightRow,
    highlightRows,
    fadeOutRow,
    getRowAnimationClass,
  }
}
