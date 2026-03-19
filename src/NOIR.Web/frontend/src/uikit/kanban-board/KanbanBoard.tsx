import { useState, useCallback, useRef, useEffect, type ReactNode } from 'react'
import {
  DndContext,
  DragOverlay,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  closestCorners,
  pointerWithin,
  rectIntersection,
  useDroppable,
  type DragEndEvent,
  type DragStartEvent,
  type DragOverEvent,
  type CollisionDetection,
} from '@dnd-kit/core'
import {
  SortableContext,
  verticalListSortingStrategy,
  horizontalListSortingStrategy,
  sortableKeyboardCoordinates,
  useSortable,
  arrayMove,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { Kanban } from 'lucide-react'
import { Skeleton } from '../skeleton'
import { EmptyState } from '../empty-state'
import type { KanbanBoardProps, KanbanColumnDef } from './types'

// ─── SortableCard ─────────────────────────────────────────────────────────────

const SortableCard = ({
  id,
  columnId,
  disabled,
  children,
}: {
  id: string
  columnId: string
  disabled: boolean
  children: () => ReactNode
}) => {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id,
    disabled,
    data: { type: 'card', columnId },
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition: isDragging ? undefined : transition,
    opacity: isDragging ? 0.4 : 1,
  }

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...(disabled ? {} : listeners)}>
      {children()}
    </div>
  )
}

// ─── SortableColumn ───────────────────────────────────────────────────────────

const SortableColumn = ({
  id,
  width,
  children,
}: {
  id: string
  width: number
  children: (opts: { dragHandleProps: Record<string, unknown>; isDragging: boolean }) => ReactNode
}) => {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id,
    data: { type: 'column' },
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition: isDragging ? undefined : transition,
    width,
    minWidth: width,
    flexShrink: 0,
  }

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`flex flex-col ${isDragging ? 'opacity-40' : ''}`}
    >
      {children({ dragHandleProps: { ...listeners, ...attributes }, isDragging })}
    </div>
  )
}

// ─── DroppableColumnBody ──────────────────────────────────────────────────────

const DroppableColumnBody = ({
  id,
  isOver,
  children,
}: {
  id: string
  isOver: boolean
  children: ReactNode
}) => {
  const { setNodeRef } = useDroppable({ id })
  return (
    <div
      ref={setNodeRef}
      className={`flex-1 min-h-[100px] space-y-2 p-2 transition-all duration-150 ${
        isOver ? 'bg-primary/5 ring-1 ring-inset ring-primary/25 rounded-b-lg' : ''
      }`}
    >
      {children}
    </div>
  )
}

// ─── KanbanBoard ──────────────────────────────────────────────────────────────

export const KanbanBoard = <TCard,>({
  columns,
  getCardId,
  renderCard,
  renderColumnHeader,
  onMoveCard,
  onTerminateCard,
  onReorderColumns,
  isLoading,
  emptyState,
  columnWidth = 280,
}: KanbanBoardProps<TCard>) => {
  // ── Local optimistic state ─────────────────────────────────────────────────
  const [localColumns, setLocalColumns] = useState<KanbanColumnDef<TCard>[]>(columns)
  const localColumnsRef = useRef<KanbanColumnDef<TCard>[]>(localColumns)
  const isDraggingRef = useRef(false)

  useEffect(() => { localColumnsRef.current = localColumns }, [localColumns])

  // Sync server state when not dragging
  useEffect(() => {
    if (!isDraggingRef.current) {
      setLocalColumns(columns)
    }
  }, [columns])

  // ── Drag state ─────────────────────────────────────────────────────────────
  const [activeId, setActiveId] = useState<string | null>(null)
  const [dragType, setDragType] = useState<'card' | 'column' | null>(null)
  const [overColumnId, setOverColumnId] = useState<string | null>(null)

  const activeCard = activeId && dragType === 'card'
    ? columns.find(col => col.cards.some(c => getCardId(c) === activeId))?.cards.find(c => getCardId(c) === activeId) ?? null
    : null

  // ── Sensors ────────────────────────────────────────────────────────────────
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

  // Non-system column IDs for horizontal sortable context
  const activeColumnIds = localColumns.filter(c => !c.isSystem).map(c => c.id)

  // ── Collision detection ────────────────────────────────────────────────────
  const collisionDetection: CollisionDetection = useCallback((args) => {
    const dragId = String(args.active.id)
    const cols = localColumnsRef.current
    const colIdSet = new Set(cols.map(c => c.id))
    const isActiveColumnDrag = colIdSet.has(dragId) && !cols.find(c => c.id === dragId)?.isSystem

    if (isActiveColumnDrag) {
      // Column drag: only hit non-system columns
      return closestCorners({
        ...args,
        droppableContainers: args.droppableContainers.filter(c => {
          const id = String(c.id)
          return colIdSet.has(id) && !cols.find(col => col.id === id)?.isSystem
        }),
      })
    }

    // Card drag: pointer-within for precise targeting, fallback to rect intersection
    const pointerHits = pointerWithin(args)
    if (pointerHits.length > 0) {
      const cardHit = pointerHits.find(({ id }) => !colIdSet.has(String(id)))
      return cardHit ? [cardHit] : [pointerHits[0]]
    }
    return rectIntersection(args)
  }, [])

  // ── handleDragStart ────────────────────────────────────────────────────────
  const handleDragStart = useCallback((event: DragStartEvent) => {
    const id = String(event.active.id)
    setActiveId(id)
    isDraggingRef.current = true
    const isColumn = localColumnsRef.current.some(c => c.id === id)
    setDragType(isColumn ? 'column' : 'card')
  }, [])

  // ── handleDragOver: optimistic reorder ────────────────────────────────────
  const handleDragOver = useCallback((event: DragOverEvent) => {
    const { active, over } = event
    if (!over) { setOverColumnId(null); return }

    const activeId = String(active.id)
    const overId = String(over.id)
    if (activeId === overId) return

    setLocalColumns(prev => {
      const isColDrag = prev.some(c => c.id === activeId && !c.isSystem)

      if (isColDrag) {
        // Column reorder preview (only between active columns)
        const fromIdx = prev.findIndex(c => c.id === activeId)
        const toIdx = prev.findIndex(c => c.id === overId && !c.isSystem)
        if (fromIdx === -1 || toIdx === -1 || fromIdx === toIdx) return prev
        return arrayMove(prev, fromIdx, toIdx)
      }

      // Card drag — find source
      const srcColIdx = prev.findIndex(col => col.cards.some(c => getCardId(c) === activeId))
      if (srcColIdx === -1) return prev

      // Find target column (by column body droppable ID or card ID)
      let tgtColIdx = prev.findIndex(c => c.id === overId)
      if (tgtColIdx === -1) {
        tgtColIdx = prev.findIndex(col => col.cards.some(c => getCardId(c) === overId))
      }
      if (tgtColIdx === -1) return prev

      setOverColumnId(prev[tgtColIdx].id)

      // Don't move cards optimistically into system columns — just show drop highlight
      if (prev[tgtColIdx].isSystem) return prev

      const cols = prev.map(c => ({ ...c, cards: [...c.cards] }))
      const srcCards = cols[srcColIdx].cards
      const tgtCards = cols[tgtColIdx].cards
      const activeCardIdx = srcCards.findIndex(c => getCardId(c) === activeId)
      if (activeCardIdx === -1) return prev

      if (srcColIdx === tgtColIdx) {
        const overCardIdx = srcCards.findIndex(c => getCardId(c) === overId)
        if (overCardIdx === -1) return prev
        cols[srcColIdx].cards = arrayMove(srcCards, activeCardIdx, overCardIdx)
      } else {
        const [moved] = srcCards.splice(activeCardIdx, 1)
        const overCardIdx = tgtCards.findIndex(c => getCardId(c) === overId)
        if (overCardIdx === -1) {
          tgtCards.push(moved)
        } else {
          tgtCards.splice(overCardIdx, 0, moved)
        }
      }
      return cols
    })
  }, [getCardId])

  // ── handleDragEnd: commit ──────────────────────────────────────────────────
  const handleDragEnd = useCallback((event: DragEndEvent) => {
    const { active, over } = event
    isDraggingRef.current = false
    setActiveId(null)
    setOverColumnId(null)
    const currentDragType = dragType
    setDragType(null)

    if (!over) {
      setLocalColumns(columns)
      return
    }

    const activeId = String(active.id)
    const overId = String(over.id)

    if (currentDragType === 'column') {
      const latestCols = localColumnsRef.current
      const newNonSystemIds = latestCols.filter(c => !c.isSystem).map(c => c.id)
      const originalNonSystemIds = columns.filter(c => !c.isSystem).map(c => c.id).join(',')
      if (newNonSystemIds.join(',') === originalNonSystemIds) return
      onReorderColumns?.(newNonSystemIds)
      return
    }

    if (currentDragType === 'card') {
      // Find source column from ORIGINAL server data
      const originalSrcCol = columns.find(col => col.cards.some(c => getCardId(c) === activeId))
      if (!originalSrcCol) return

      const latestCols = localColumnsRef.current

      // Determine target column from the drop target (over.id)
      let targetCol: KanbanColumnDef<TCard> | undefined
      targetCol = latestCols.find(c => c.id === overId)
      if (!targetCol) {
        targetCol = latestCols.find(col => col.cards.some(c => getCardId(c) === overId))
      }
      if (!targetCol) return

      // System column: terminate the card
      if (targetCol.isSystem && targetCol.systemType) {
        onTerminateCard?.({
          cardId: activeId,
          fromColumnId: originalSrcCol.id,
          systemType: targetCol.systemType,
        })
        setLocalColumns(columns) // revert optimistic state; server will push updated data
        return
      }

      // Normal column: find neighbors from local (optimistic) position
      const tgtColCards = targetCol.cards
      const cardIdx = tgtColCards.findIndex(c => getCardId(c) === activeId)

      // No-op if position unchanged
      const origIdx = originalSrcCol.cards.findIndex(c => getCardId(c) === activeId)
      if (originalSrcCol.id === targetCol.id && origIdx === cardIdx) return

      const prevCard = cardIdx > 0 ? tgtColCards[cardIdx - 1] : null
      const nextCard = cardIdx < tgtColCards.length - 1 ? tgtColCards[cardIdx + 1] : null

      onMoveCard({
        cardId: activeId,
        fromColumnId: originalSrcCol.id,
        toColumnId: targetCol.id,
        prevCardId: prevCard ? getCardId(prevCard) : null,
        nextCardId: nextCard ? getCardId(nextCard) : null,
      })
    }
  }, [dragType, columns, getCardId, onMoveCard, onTerminateCard, onReorderColumns])

  // ── Loading state ──────────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div className="flex gap-4 overflow-x-auto pb-4">
        {[...Array(4)].map((_, i) => (
          <div key={i} style={{ minWidth: columnWidth }} className="space-y-3">
            <Skeleton className="h-12 w-full rounded-lg" />
            <Skeleton className="h-24 w-full rounded-lg" />
            <Skeleton className="h-24 w-full rounded-lg" />
          </div>
        ))}
      </div>
    )
  }

  // ── Empty state ────────────────────────────────────────────────────────────
  if (localColumns.length === 0) {
    return emptyState ?? (
      <EmptyState icon={Kanban} title="No columns" description="Add columns to get started." />
    )
  }

  const nonSystemColumns = localColumns.filter(c => !c.isSystem)
  const systemColumns = localColumns.filter(c => c.isSystem)

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={collisionDetection}
      onDragStart={handleDragStart}
      onDragOver={handleDragOver}
      onDragEnd={handleDragEnd}
    >
      <SortableContext items={activeColumnIds} strategy={horizontalListSortingStrategy}>
        <div className="flex gap-4 overflow-x-auto pb-4">
          {/* Sortable (active) columns */}
          {nonSystemColumns.map((column) => (
            <SortableColumn key={column.id} id={column.id} width={columnWidth}>
              {({ dragHandleProps, isDragging }) => (
                <div
                  className={`bg-muted/30 rounded-lg border border-border/50 flex flex-col h-full ${isDragging ? 'shadow-lg' : ''}`}
                >
                  <div {...dragHandleProps} className="cursor-grab active:cursor-grabbing rounded-t-lg">
                    {renderColumnHeader(column)}
                  </div>
                  <DroppableColumnBody id={column.id} isOver={overColumnId === column.id}>
                    <SortableContext
                      items={column.cards.map(getCardId)}
                      strategy={verticalListSortingStrategy}
                    >
                      {column.cards.map((card) => (
                        <SortableCard
                          key={getCardId(card)}
                          id={getCardId(card)}
                          columnId={column.id}
                          disabled={false}
                        >
                          {() => renderCard(card)}
                        </SortableCard>
                      ))}
                    </SortableContext>
                  </DroppableColumnBody>
                </div>
              )}
            </SortableColumn>
          ))}

          {/* System columns (pinned, not draggable as columns) */}
          {systemColumns.map((column) => (
            <div
              key={column.id}
              className="flex flex-col flex-shrink-0"
              style={{ width: columnWidth, minWidth: columnWidth }}
            >
              <div className="bg-muted/30 rounded-lg border border-border/50 flex flex-col h-full">
                <div className="rounded-t-lg">
                  {renderColumnHeader(column)}
                </div>
                <DroppableColumnBody id={column.id} isOver={overColumnId === column.id}>
                  <SortableContext
                    items={column.cards.map(getCardId)}
                    strategy={verticalListSortingStrategy}
                  >
                    {column.cards.map((card) => (
                      <SortableCard
                        key={getCardId(card)}
                        id={getCardId(card)}
                        columnId={column.id}
                        disabled={true}
                      >
                        {() => renderCard(card)}
                      </SortableCard>
                    ))}
                  </SortableContext>
                </DroppableColumnBody>
              </div>
            </div>
          ))}
        </div>
      </SortableContext>

      <DragOverlay>
        {activeCard && (
          <div style={{ width: columnWidth }}>
            {renderCard(activeCard)}
          </div>
        )}
      </DragOverlay>
    </DndContext>
  )
}
