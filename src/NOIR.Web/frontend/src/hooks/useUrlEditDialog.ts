import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'

/**
 * URL-synced edit dialog state hook.
 * - Stores the editing entity's ID in ?edit=<id> URL param
 * - Resolves the full entity from the provided items array
 * - Updates URL when dialog opens/closes (replace, no history pollution)
 * - Preserves existing search params
 * - Enables bookmarking and sharing edit dialog states
 *
 * Usage:
 * ```tsx
 * const { editItem, openEdit, closeEdit } = useUrlEditDialog<EntityType>(items)
 * <Dialog open={!!editItem} onOpenChange={(open) => !open && closeEdit()}>
 *   ...
 * </Dialog>
 * <Button onClick={() => openEdit(entity)}>Edit</Button>
 * ```
 */
export const useUrlEditDialog = <T extends { id: string }>(
  items: T[] | undefined,
  paramName = 'edit',
) => {
  const [searchParams, setSearchParams] = useSearchParams()

  const editId = searchParams.get(paramName)

  const editItem = useMemo(() => {
    if (!editId || !items) return null
    return items.find((item) => item.id === editId) ?? null
  }, [editId, items])

  const openEdit = useCallback(
    (item: T) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev)
          next.set(paramName, item.id)
          return next
        },
        { replace: true },
      )
    },
    [setSearchParams, paramName],
  )

  const closeEdit = useCallback(() => {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        next.delete(paramName)
        return next
      },
      { replace: true },
    )
  }, [setSearchParams, paramName])

  const onEditOpenChange = useCallback(
    (open: boolean) => {
      if (!open) closeEdit()
    },
    [closeEdit],
  )

  return { editItem, openEdit, closeEdit, onEditOpenChange }
}
