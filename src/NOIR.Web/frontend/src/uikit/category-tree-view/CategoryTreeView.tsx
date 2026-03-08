/**
 * CategoryTreeView Component
 *
 * A reusable hierarchical tree view for displaying categories with
 * expand/collapse, drag-drop reordering/reparenting, and inline actions.
 *
 * Built on @headless-tree for accessible, keyboard-navigable tree interactions.
 * Uses @tanstack/react-virtual for virtualized rendering of large lists.
 * Used by both ProductCategoriesPage and BlogCategoriesPage.
 */
import { useMemo, useCallback, useRef, useState, useLayoutEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useTree } from '@headless-tree/react'
import {
  syncDataLoaderFeature,
  hotkeysCoreFeature,
  dragAndDropFeature,
  expandAllFeature,
  removeItemsFromParents,
  insertItemsAtTarget,
  type ItemInstance,
  type DragTarget,
} from '@headless-tree/core'
import { useVirtualizer } from '@tanstack/react-virtual'
import {
  ChevronRight,
  ChevronDown,
  UnfoldVertical,
  FoldVertical,
  FolderTree,
  Pencil,
  Plus,
  Trash2,
  MoreHorizontal,
  GripVertical,
} from 'lucide-react'
import { Button } from '../button/Button'
import { Badge } from '../badge/Badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '../dropdown-menu/DropdownMenu'
import { cn } from '@/lib/utils'

// Generic category interface that both Product and Blog categories satisfy
export interface TreeCategory {
  id: string
  name: string
  slug: string
  description?: string | null
  sortOrder: number
  parentId?: string | null
  parentName?: string | null
  childCount: number
  // Item count - could be productCount or postCount
  itemCount?: number
}

// Reorder event item
export interface ReorderItem {
  id: string
  parentId: string | null
  sortOrder: number
}

interface CategoryTreeViewProps<T extends TreeCategory> {
  categories: T[]
  loading?: boolean
  onEdit?: (category: T) => void
  onDelete?: (category: T) => void
  onAddChild?: (category: T) => void
  canEdit?: boolean
  canDelete?: boolean
  itemCountLabel?: string // "products" or "posts"
  emptyMessage?: string
  emptyDescription?: string
  onCreateClick?: () => void
  onReorder?: (items: ReorderItem[]) => void
  /** Height of the virtual scroll container. Accepts px number or CSS string (default: fills remaining viewport). Applied when items > VIRTUALIZE_THRESHOLD. */
  maxHeight?: number | string
}

// Internal tree data item
interface TreeDataItem<T extends TreeCategory> {
  name: string
  children: string[]
  category: T | null
}

// Virtualize when visible item count exceeds this threshold
const VIRTUALIZE_THRESHOLD = 40
// Estimated row height in px (actual height measured dynamically)
const ROW_ESTIMATED_HEIGHT = 44
// Default scroll container height when virtualizing
// PageHeader (~80px) + CardHeader with search (~130px) + page padding (~48px) + toolbar (~52px) + buffer (~16px) = ~326px
const DEFAULT_MAX_HEIGHT = 'calc(100vh - 326px)'
// Delay (ms) before auto-expanding a collapsed folder on drag hover
const OPEN_ON_DROP_DELAY = 600

// Build a flat data map for headless-tree from categories
const buildDataMap = <T extends TreeCategory>(categories: T[]): Record<string, TreeDataItem<T>> => {
  // Group by parentId to find children
  const childrenMap = new Map<string | null, string[]>()
  const catMap = new Map(categories.map(c => [c.id, c]))

  categories.forEach(cat => {
    const parentKey = cat.parentId || null
    if (!childrenMap.has(parentKey)) childrenMap.set(parentKey, [])
    childrenMap.get(parentKey)!.push(cat.id)
  })

  // Sort children within each parent by sortOrder
  childrenMap.forEach((ids) => {
    ids.sort((a, b) => (catMap.get(a)!.sortOrder) - (catMap.get(b)!.sortOrder))
  })

  // Build data map with virtual root
  const data: Record<string, TreeDataItem<T>> = {
    root: {
      name: 'Root',
      children: childrenMap.get(null) || [],
      category: null,
    },
  }

  categories.forEach(cat => {
    data[cat.id] = {
      name: cat.name,
      children: childrenMap.get(cat.id) || [],
      category: cat,
    }
  })

  return data
}

// Compute changed items after a drop and produce ReorderItem[] for the backend
const computeReorderItems = <T extends TreeCategory>(
  dataMap: Record<string, TreeDataItem<T>>,
  changedParentIds: Set<string>,
): ReorderItem[] => {
  const items: ReorderItem[] = []
  for (const parentId of changedParentIds) {
    const parent = dataMap[parentId]
    if (!parent) continue
    const resolvedParentId = parentId === 'root' ? null : parentId
    parent.children.forEach((childId, index) => {
      items.push({ id: childId, parentId: resolvedParentId, sortOrder: index })
    })
  }
  return items
}

export const CategoryTreeView = <T extends TreeCategory>({
  categories,
  loading = false,
  onEdit,
  onDelete,
  onAddChild,
  canEdit = true,
  canDelete = true,
  itemCountLabel = 'items',
  emptyMessage = 'No categories found',
  emptyDescription = 'Get started by creating your first category.',
  onCreateClick,
  onReorder,
  maxHeight = DEFAULT_MAX_HEIGHT,
}: CategoryTreeViewProps<T>) => {
  const { t } = useTranslation('common')
  const dataMapRef = useRef<Record<string, TreeDataItem<T>>>(buildDataMap(categories))
  const treeRef = useRef<ReturnType<typeof useTree<TreeDataItem<T>>> | null>(null)
  const onReorderRef = useRef(onReorder)
  onReorderRef.current = onReorder

  // Fill-parent mode: measure exact available height via toolbar position in viewport.
  // This avoids relying on the h-full chain (which breaks through AnimatedOutlet/Suspense).
  // Runs after every render so sidebar toggles, breadcrumb changes, etc. are captured.
  // getBoundingClientRect on one element is ~0.1ms — negligible even during drag.
  // Bottom constant: space-y-2 (8) + tree-wrapper p-4 (16) + Card py-6 (24) + main p-6 (24) = 72px
  const fillParent = maxHeight === '100%'
  const toolbarRef = useRef<HTMLDivElement>(null)
  const virtualHeightRef = useRef(400)
  const [virtualHeight, setVirtualHeight] = useState(400)
  useLayoutEffect(() => {
    if (!fillParent || !toolbarRef.current) return
    const bottom = toolbarRef.current.getBoundingClientRect().bottom
    const available = Math.max(100, Math.floor(window.innerHeight - bottom - 72))
    if (Math.abs(available - virtualHeightRef.current) > 2) {
      virtualHeightRef.current = available
      setVirtualHeight(available)
    }
  })
  // Resize listener: the no-deps effect above covers render-triggered updates (sidebar toggle etc.)
  // but a plain browser resize doesn't trigger a re-render, so we need an explicit listener.
  useLayoutEffect(() => {
    if (!fillParent) return
    const handleResize = () => {
      if (!toolbarRef.current) return
      const bottom = toolbarRef.current.getBoundingClientRect().bottom
      const available = Math.max(100, Math.floor(window.innerHeight - bottom - 72))
      if (Math.abs(available - virtualHeightRef.current) > 2) {
        virtualHeightRef.current = available
        setVirtualHeight(available)
      }
    }
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [fillParent])
  // Skip server-data rebuilds briefly after a drop to prevent flicker
  const dropTimestampRef = useRef(0)

  const canDrag = useMemo(() => !!onReorder, [onReorder])

  // Batch all parent changes from a single drop into one update + one API call
  const handleDrop = useMemo(() => {
    if (!onReorder) return undefined
    return async (
      items: ItemInstance<TreeDataItem<T>>[],
      target: DragTarget<TreeDataItem<T>>,
    ) => {
      const changedParentIds = new Set<string>()
      const applyChanges = (
        item: ItemInstance<TreeDataItem<T>>,
        newChildren: string[],
      ) => {
        const parentId = item.getId()
        changedParentIds.add(parentId)
        dataMapRef.current = {
          ...dataMapRef.current,
          [parentId]: { ...dataMapRef.current[parentId], children: newChildren },
        }
      }
      await removeItemsFromParents(items, applyChanges)
      await insertItemsAtTarget(items.map(i => i.getId()), target, applyChanges)

      dropTimestampRef.current = Date.now()
      treeRef.current?.rebuildTree()
      const reorderItems = computeReorderItems(dataMapRef.current, changedParentIds)
      onReorderRef.current?.(reorderItems)
    }
  }, [onReorder])

  const features = useMemo(
    () => [syncDataLoaderFeature, hotkeysCoreFeature, dragAndDropFeature, expandAllFeature],
    [],
  )

  const tree = useTree<TreeDataItem<T>>({
    rootItemId: 'root',
    getItemName: (item) => item.getItemData().name,
    isItemFolder: () => true,
    indent: 24,
    canReorder: canDrag,
    canDrag: () => canDrag,
    seperateDragHandle: false,
    openOnDropDelay: OPEN_ON_DROP_DELAY,
    dataLoader: {
      getItem: (id) => dataMapRef.current[id],
      getChildren: (id) => dataMapRef.current[id]?.children ?? [],
    },
    onDrop: handleDrop,
    features,
  })
  treeRef.current = tree

  // Snapshot dragged item IDs — read from dnd state only once per render
  const dndState = tree.getState().dnd
  const draggedIds = useMemo(() => {
    if (!dndState?.draggedItems) return new Set<string>()
    return new Set(dndState.draggedItems.map(i => i.getId()))
  }, [dndState?.draggedItems])
  const isDragging = draggedIds.size > 0

  // Rebuild tree when categories change from server.
  // Skip rebuilds within 2s of a drop to prevent the server refetch from overwriting
  // the local optimistic order and causing a visible flicker.
  const prevCategoriesRef = useRef(categories)
  if (prevCategoriesRef.current !== categories) {
    prevCategoriesRef.current = categories
    const msSinceDrop = Date.now() - dropTimestampRef.current
    if (msSinceDrop > 2000) {
      dataMapRef.current = buildDataMap(categories)
      tree.rebuildTree()
    }
  }

  const expandAll = useCallback(() => tree.expandAll(), [tree])
  const collapseAll = useCallback(() => tree.collapseAll(), [tree])

  // Get flat visible items (headless-tree only includes expanded items)
  const treeItems = tree.getItems()
  // Base virtualization on total category count (stable), not visible items count.
  // Using treeItems.length would flip between virtual/standard modes as items expand/collapse,
  // causing full DOM rebuilds during interaction and perceived lag.
  const shouldVirtualize = categories.length > VIRTUALIZE_THRESHOLD

  // Virtual scroll container ref (only used when virtualizing)
  const scrollParentRef = useRef<HTMLDivElement>(null)
  const rowVirtualizer = useVirtualizer({
    count: shouldVirtualize ? treeItems.length : 0,
    getScrollElement: () => scrollParentRef.current,
    estimateSize: () => ROW_ESTIMATED_HEIGHT,
    overscan: 8,
    getItemKey: (index) => treeItems[index]?.getId() ?? index,
  })

  if (loading) {
    return (
      <div className="space-y-2 p-4">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="flex items-center gap-3 animate-pulse">
            <div className="h-6 w-6 bg-muted rounded" />
            <div className="h-4 w-4 bg-muted rounded" />
            <div className="h-4 flex-1 bg-muted rounded" />
            <div className="h-5 w-16 bg-muted rounded-full" />
          </div>
        ))}
      </div>
    )
  }

  if (categories.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <div className="p-4 rounded-xl bg-muted/50 border border-border mb-4">
          <FolderTree className="h-8 w-8 text-muted-foreground" />
        </div>
        <h3 className="font-medium text-foreground mb-1">{emptyMessage}</h3>
        <p className="text-sm text-muted-foreground mb-4">{emptyDescription}</p>
        {onCreateClick && (
          <Button onClick={onCreateClick} className="cursor-pointer">
            {t('buttons.create', 'Create')}
          </Button>
        )}
      </div>
    )
  }

  // Renders a single tree row (shared between virtual and non-virtual paths)
  const renderTreeItem = (item: ItemInstance<TreeDataItem<T>>) => {
    const data = item.getItemData()
    const category = data.category
    if (!category) return null

    const meta = item.getItemMeta()
    const hasChildren = data.children.length > 0
    const isExpanded = item.isExpanded()
    const isBeingDragged = draggedIds.has(item.getId())
    const isDropTarget = item.isDragTarget()
    const isDirectDropTarget = item.isUnorderedDragTarget()

    return (
      <div
        {...item.getProps()}
        key={item.getId()}
        className={cn(
          'group flex items-center gap-2 py-2 px-3 rounded-lg',
          'transition-colors duration-150',
          'hover:bg-muted/50',
          canDrag && 'cursor-grab active:cursor-grabbing',
          isBeingDragged && 'opacity-40 scale-[0.98]',
          isDirectDropTarget && !isBeingDragged && [
            'bg-primary/5 ring-2 ring-primary/40 ring-offset-1 ring-offset-background',
            'shadow-[0_0_12px_-3px] shadow-primary/20',
          ],
          isDropTarget && !isDirectDropTarget && !isBeingDragged && 'bg-accent/30',
          !isBeingDragged && !isDropTarget && item.isFocused() && 'bg-muted/30',
        )}
        style={{ paddingLeft: `${meta.level * 24 + 12}px` }}
      >
        {/* Expand/Collapse Button */}
        <Button
          variant="ghost"
          size="icon"
          className={cn(
            'h-6 w-6 p-0 cursor-pointer shrink-0',
            'transition-transform duration-200',
            isExpanded && 'text-foreground',
            !hasChildren && 'invisible'
          )}
          onClick={(e) => {
            e.stopPropagation()
            if (isExpanded) item.collapse()
            else item.expand()
          }}
          aria-label={isExpanded ? t('nav.collapse', 'Collapse') : t('nav.expand', 'Expand')}
        >
          {isExpanded ? (
            <ChevronDown className="h-4 w-4" />
          ) : (
            <ChevronRight className="h-4 w-4" />
          )}
        </Button>

        {/* Drag Handle (visual indicator only — the whole row is draggable) */}
        {canDrag ? (
          <div className="flex items-center shrink-0 pointer-events-none">
            <GripVertical className={cn(
              'h-4 w-4 text-muted-foreground/40 shrink-0',
              'transition-opacity duration-150',
              'opacity-0 group-hover:opacity-70',
              isDragging && 'opacity-50',
            )} />
          </div>
        ) : (
          <div className="w-4 shrink-0" />
        )}

        {/* Icon */}
        <FolderTree className={cn(
          'h-4 w-4 text-muted-foreground shrink-0 transition-colors duration-200',
          isDirectDropTarget && !isBeingDragged && 'text-primary',
        )} />

        {/* Name & Description */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className="font-medium truncate">{category.name}</span>
            <code className="text-xs text-muted-foreground bg-muted px-1.5 py-0.5 rounded hidden sm:inline">
              {category.slug}
            </code>
          </div>
          {category.description && (
            <p className="text-xs text-muted-foreground line-clamp-1 mt-0.5">
              {category.description}
            </p>
          )}
        </div>

        {/* Badges */}
        <div className="flex items-center gap-2 shrink-0">
          {category.itemCount !== undefined && category.itemCount > 0 && (
            <Badge variant="secondary" className="text-xs">
              {category.itemCount} {itemCountLabel}
            </Badge>
          )}
          {hasChildren && (
            <Badge variant="outline" className="text-xs">
              {data.children.length} {t('labels.children', 'children')}
            </Badge>
          )}
        </div>

        {/* Actions */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer shrink-0"
              aria-label={t('labels.actionsFor', { name: category.name })}
              onClick={(e) => e.stopPropagation()}
            >
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-40">
            {canEdit && onEdit && (
              <DropdownMenuItem
                className="cursor-pointer"
                onClick={() => onEdit(category)}
              >
                <Pencil className="h-4 w-4 mr-2" />
                {t('labels.edit', 'Edit')}
              </DropdownMenuItem>
            )}
            {onAddChild && (
              <DropdownMenuItem
                className="cursor-pointer"
                onClick={() => onAddChild(category)}
              >
                <Plus className="h-4 w-4 mr-2" />
                {t('labels.addChild', 'Add Child')}
              </DropdownMenuItem>
            )}
            {canDelete && onDelete && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  className="text-destructive cursor-pointer"
                  onClick={() => onDelete(category)}
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  {t('labels.delete', 'Delete')}
                </DropdownMenuItem>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    )
  }

  return (
    <div className="space-y-2">
      {/* Toolbar */}
      <div ref={toolbarRef} className="flex items-center justify-end pb-2 border-b border-border/50">
        <div className="flex items-center gap-1 p-1 rounded-lg bg-muted">
          <Button
            variant="ghost"
            size="sm"
            onClick={expandAll}
            className="cursor-pointer text-xs h-7 px-2.5 hover:bg-background hover:text-foreground hover:shadow-sm transition-all duration-200"
          >
            <UnfoldVertical className="h-3.5 w-3.5 mr-1.5" />
            {t('buttons.expandAll', 'Expand All')}
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={collapseAll}
            className="cursor-pointer text-xs h-7 px-2.5 hover:bg-background hover:text-foreground hover:shadow-sm transition-all duration-200"
          >
            <FoldVertical className="h-3.5 w-3.5 mr-1.5" />
            {t('buttons.collapseAll', 'Collapse All')}
          </Button>
        </div>
      </div>

      {shouldVirtualize ? (
        /* ── Virtualized path: explicit-height scroll container ── */
        <div
          ref={scrollParentRef}
          className="overflow-auto rounded-lg"
          style={{ height: fillParent ? virtualHeight : maxHeight }}
        >
          <div
            {...tree.getContainerProps('Category Tree')}
            style={{ height: `${rowVirtualizer.getTotalSize()}px`, position: 'relative' }}
          >
            {rowVirtualizer.getVirtualItems().map((virtualItem) => {
              const item = treeItems[virtualItem.index]
              if (!item) return null
              return (
                <div
                  key={item.getId()}
                  data-index={virtualItem.index}
                  ref={rowVirtualizer.measureElement}
                  style={{
                    position: 'absolute',
                    top: 0,
                    left: 0,
                    width: '100%',
                    transform: `translateY(${virtualItem.start}px)`,
                  }}
                >
                  {renderTreeItem(item)}
                </div>
              )
            })}

            {/* Drag indicator line */}
            {canDrag && (
              <>
                <div
                  style={tree.getDragLineStyle()}
                  className={cn(
                    'absolute h-[2px] bg-primary rounded-full pointer-events-none',
                    'transition-[top,left,width] duration-150 ease-out',
                    isDragging ? 'opacity-100' : 'opacity-0',
                  )}
                />
                {isDragging && tree.getDragLineData() && (() => {
                  const lineStyle = tree.getDragLineStyle()
                  return lineStyle.top ? (
                    <div
                      className="absolute pointer-events-none"
                      style={{
                        top: lineStyle.top,
                        left: lineStyle.left,
                        transform: 'translate(-3px, -3px)',
                      }}
                    >
                      <div className="h-2 w-2 rounded-full bg-primary ring-2 ring-background" />
                    </div>
                  ) : null
                })()}
              </>
            )}
          </div>
        </div>
      ) : (
        /* ── Standard path: auto-height, no virtualization ── */
        <div
          {...tree.getContainerProps('Category Tree')}
          className="space-y-0.5 relative"
        >
          {treeItems.map((item) => renderTreeItem(item))}

          {/* Drag indicator line */}
          {canDrag && (
            <>
              <div
                style={tree.getDragLineStyle()}
                className={cn(
                  'absolute h-[2px] bg-primary rounded-full pointer-events-none',
                  'transition-[top,left,width] duration-150 ease-out',
                  isDragging ? 'opacity-100' : 'opacity-0',
                )}
              />
              {isDragging && tree.getDragLineData() && (() => {
                const lineStyle = tree.getDragLineStyle()
                return lineStyle.top ? (
                  <div
                    className="absolute pointer-events-none"
                    style={{
                      top: lineStyle.top,
                      left: lineStyle.left,
                      transform: 'translate(-3px, -3px)',
                    }}
                  >
                    <div className="h-2 w-2 rounded-full bg-primary ring-2 ring-background" />
                  </div>
                ) : null
              })()}
            </>
          )}
        </div>
      )}
    </div>
  )
}
