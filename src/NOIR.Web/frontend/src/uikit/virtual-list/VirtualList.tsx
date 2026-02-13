import { useRef, type ReactNode, type CSSProperties } from 'react'
import { useVirtualizer, type VirtualItem } from '@tanstack/react-virtual'
import { cn } from '@/lib/utils'

interface VirtualListProps<T> {
  /** Array of items to render */
  items: T[]
  /** Estimated height of each item in pixels */
  estimateSize: number
  /** Render function for each item */
  renderItem: (item: T, index: number, virtualItem: VirtualItem) => ReactNode
  /** Number of items to render outside visible area (default: 5) */
  overscan?: number
  /** Container className */
  className?: string
  /** Item wrapper className */
  itemClassName?: string
  /** Container height (default: 100%) */
  height?: string | number
  /** Key extractor function (defaults to index) */
  getItemKey?: (item: T, index: number) => string | number
}

/**
 * VirtualList - Efficiently render large lists with virtualization
 *
 * Only renders items that are visible in the viewport (plus overscan),
 * dramatically improving performance for lists with hundreds/thousands of items.
 *
 * @example
 * <VirtualList
 *   items={logs}
 *   estimateSize={48}
 *   renderItem={(log, index) => (
 *     <LogEntry key={log.id} log={log} />
 *   )}
 *   getItemKey={(log) => log.id}
 * />
 */
export function VirtualList<T>({
  items,
  estimateSize,
  renderItem,
  overscan = 5,
  className,
  itemClassName,
  height = '100%',
  getItemKey,
}: VirtualListProps<T>) {
  const parentRef = useRef<HTMLDivElement>(null)

  const virtualizer = useVirtualizer({
    count: items.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => estimateSize,
    overscan,
    getItemKey: getItemKey
      ? (index) => getItemKey(items[index], index)
      : undefined,
  })

  const virtualItems = virtualizer.getVirtualItems()

  return (
    <div
      ref={parentRef}
      className={cn('overflow-auto', className)}
      style={{ height }}
    >
      <div
        style={{
          height: `${virtualizer.getTotalSize()}px`,
          width: '100%',
          position: 'relative',
        }}
      >
        {virtualItems.map((virtualItem) => (
          <div
            key={virtualItem.key}
            data-index={virtualItem.index}
            ref={virtualizer.measureElement}
            className={itemClassName}
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              width: '100%',
              transform: `translateY(${virtualItem.start}px)`,
            }}
          >
            {renderItem(items[virtualItem.index], virtualItem.index, virtualItem)}
          </div>
        ))}
      </div>
    </div>
  )
}

/**
 * Hook for custom virtualizer control
 *
 * Use when you need more control over the virtualization behavior.
 */
export function useVirtualList<T>({
  items,
  estimateSize,
  overscan = 5,
  getItemKey,
}: {
  items: T[]
  estimateSize: number
  overscan?: number
  getItemKey?: (item: T, index: number) => string | number
}) {
  const parentRef = useRef<HTMLDivElement>(null)

  const virtualizer = useVirtualizer({
    count: items.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => estimateSize,
    overscan,
    getItemKey: getItemKey
      ? (index) => getItemKey(items[index], index)
      : undefined,
  })

  return {
    parentRef,
    virtualizer,
    virtualItems: virtualizer.getVirtualItems(),
    totalSize: virtualizer.getTotalSize(),
    scrollToIndex: virtualizer.scrollToIndex,
    scrollToOffset: virtualizer.scrollToOffset,
  }
}

/**
 * Style helper for virtual item positioning
 */
export function getVirtualItemStyle(virtualItem: VirtualItem): CSSProperties {
  return {
    position: 'absolute',
    top: 0,
    left: 0,
    width: '100%',
    height: `${virtualItem.size}px`,
    transform: `translateY(${virtualItem.start}px)`,
  }
}
