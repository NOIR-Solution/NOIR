import type { ReactNode } from 'react'

/** A column in the Kanban board with its cards. */
export interface KanbanColumnDef<TCard> {
  id: string
  cards: TCard[]
  /** System columns (e.g. Won/Lost in CRM) are pinned at the end and not draggable as columns. */
  isSystem?: boolean
  /** Identifier passed to `onTerminateCard` when a card is dropped into this system column. */
  systemType?: string
}

/** Params passed to onMoveCard for a normal card move. */
export interface KanbanMoveCardParams {
  cardId: string
  fromColumnId: string
  toColumnId: string
  /** Card immediately before the moved card in the new position (null if first). */
  prevCardId: string | null
  /** Card immediately after the moved card in the new position (null if last). */
  nextCardId: string | null
}

/** Params passed to onTerminateCard when a card is dropped into a system column. */
export interface KanbanTerminateCardParams {
  cardId: string
  fromColumnId: string
  /** The `systemType` value from the target system column. */
  systemType: string
}

export interface KanbanBoardProps<TCard> {
  columns: KanbanColumnDef<TCard>[]
  /** Return the stable unique ID for a card. */
  getCardId: (card: TCard) => string
  /** Render a card. */
  renderCard: (card: TCard) => ReactNode
  /** Render the column header (receives the full column definition). */
  renderColumnHeader: (column: KanbanColumnDef<TCard>) => ReactNode
  /** Called when a card is moved to a normal (non-system) column. */
  onMoveCard: (params: KanbanMoveCardParams) => void
  /** Called when a card is dropped into a system column. Not called for same-column reorder. */
  onTerminateCard?: (params: KanbanTerminateCardParams) => void
  /** Called when the user reorders non-system columns; receives the new ordered IDs. */
  onReorderColumns?: (orderedColumnIds: string[]) => void
  isLoading?: boolean
  /** Column width in px (default: 280). */
  columnWidth?: number
  emptyState?: ReactNode
}
