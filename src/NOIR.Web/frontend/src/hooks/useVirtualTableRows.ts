import { useState, useLayoutEffect, useCallback } from 'react'
import { useVirtualizer } from '@tanstack/react-virtual'

const VIRTUALIZE_THRESHOLD = 40
// CardContent p-6 bottom (24px) + main p-6 bottom (24px)
const BOTTOM_GAP = 48

/**
 * Virtualizes long table body rows using the spacer-row technique.
 * Keeps normal <table> layout (no display:block on tbody) so column widths
 * stay consistent between header and body automatically.
 *
 * Uses a callback ref so height measurement fires when the element ACTUALLY
 * mounts — safe for conditional rendering (e.g. viewMode === 'table').
 *
 * Usage:
 *   const { scrollRef, height, shouldVirtualize, virtualItems, topPad, bottomPad } =
 *     useVirtualTableRows(items)
 *
 *   Wrap the table in: <div ref={scrollRef} style={{ height, overflowY: 'auto' }}>
 *   Make <TableHeader> sticky: className="sticky top-0 z-10 bg-background"
 *   Render spacer rows + virtualItems.map(vr => items[vr.index]) in <TableBody>
 */
export function useVirtualTableRows<T extends { id: string }>(
  items: T[],
  estimateRowHeight = 52,
) {
  const [scrollEl, setScrollEl] = useState<HTMLDivElement | null>(null)
  const [height, setHeight] = useState(400)
  const shouldVirtualize = items.length > VIRTUALIZE_THRESHOLD

  // Callback ref: fires when the element mounts/unmounts, even in conditional renders
  const scrollRef = useCallback((el: HTMLDivElement | null) => {
    setScrollEl(el)
  }, [])

  // Re-measure whenever the element is attached or the window is resized
  useLayoutEffect(() => {
    if (!scrollEl) return

    const measure = () => {
      const { top } = scrollEl.getBoundingClientRect()
      setHeight(Math.max(200, Math.floor(window.innerHeight - top - BOTTOM_GAP)))
    }

    measure()
    window.addEventListener('resize', measure)
    return () => window.removeEventListener('resize', measure)
  }, [scrollEl])

  const virtualizer = useVirtualizer({
    count: shouldVirtualize ? items.length : 0,
    getScrollElement: () => scrollEl,
    estimateSize: () => estimateRowHeight,
    overscan: 5,
    getItemKey: (index) => items[index].id,
  })

  const virtualItems = virtualizer.getVirtualItems()
  const totalSize = virtualizer.getTotalSize()
  const topPad = shouldVirtualize && virtualItems.length > 0 ? (virtualItems[0].start ?? 0) : 0
  const bottomPad =
    shouldVirtualize && virtualItems.length > 0
      ? totalSize - (virtualItems[virtualItems.length - 1].end ?? 0)
      : 0

  return { scrollRef, height, shouldVirtualize, virtualItems, totalSize, topPad, bottomPad }
}
