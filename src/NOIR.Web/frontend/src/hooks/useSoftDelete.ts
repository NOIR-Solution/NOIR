import { useState, useCallback, useRef } from 'react'
import { toast } from '@/lib/toast'

interface UseSoftDeleteOptions<T> {
  /** Function to perform the soft delete */
  onDelete: (item: T) => Promise<void>
  /** Function to restore (undo) the delete */
  onRestore: (item: T) => Promise<void>
  /** Duration in ms to allow undo (default: 5000) */
  undoDuration?: number
  /** Function to get display name for the item */
  getItemName: (item: T) => string
  /** Entity type name for messages (e.g., "Product", "Role") */
  entityType: string
  /** Success message template (use {name} placeholder) */
  successMessage?: string
  /** Restored message template (use {name} placeholder) */
  restoredMessage?: string
}

interface UseSoftDeleteReturn<T> {
  /** Perform soft delete with undo capability */
  handleDelete: (item: T) => Promise<void>
  /** Whether a delete is pending (within undo window) */
  isPending: boolean
  /** The item currently pending deletion */
  pendingItem: T | null
  /** Cancel the pending delete (restore immediately) */
  cancelDelete: () => Promise<void>
}

/**
 * Hook for soft-delete with undo capability
 *
 * Implements the "undo toast" pattern:
 * 1. User clicks delete
 * 2. Item is soft-deleted immediately (optimistic update)
 * 3. Toast shows with "Undo" button for X seconds
 * 4. If undo clicked: Item is restored
 * 5. If timer expires: Delete is permanent
 *
 * @example
 * const { handleDelete, isPending } = useSoftDelete({
 *   onDelete: (product) => deleteProduct(product.id),
 *   onRestore: (product) => restoreProduct(product.id),
 *   getItemName: (product) => product.name,
 *   entityType: 'Product',
 * })
 *
 * // In delete button onClick:
 * onClick={() => handleDelete(product)}
 */
export function useSoftDelete<T>({
  onDelete,
  onRestore,
  undoDuration = 5000,
  getItemName,
  entityType,
  successMessage = '{entityType} "{name}" deleted',
  restoredMessage = '{entityType} "{name}" restored',
}: UseSoftDeleteOptions<T>): UseSoftDeleteReturn<T> {
  const [pendingItem, setPendingItem] = useState<T | null>(null)
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const toastIdRef = useRef<string | number | null>(null)

  const formatMessage = useCallback(
    (template: string, item: T) => {
      return template
        .replace('{entityType}', entityType)
        .replace('{name}', getItemName(item))
    },
    [entityType, getItemName]
  )

  const handleDelete = useCallback(
    async (item: T) => {
      const itemName = getItemName(item)

      // Clear any existing pending delete
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current)
      }
      if (toastIdRef.current) {
        toast.dismiss(toastIdRef.current)
      }

      setPendingItem(item)

      try {
        // Perform soft delete
        await onDelete(item)

        // Show undo toast
        toastIdRef.current = toast.undo(
          formatMessage(successMessage, item),
          async () => {
            // Undo was clicked
            if (timeoutRef.current) {
              clearTimeout(timeoutRef.current)
            }

            try {
              await onRestore(item)
              setPendingItem(null)
              toast.success(formatMessage(restoredMessage, item))
            } catch (error) {
              toast.error(`Failed to restore ${entityType.toLowerCase()}`)
              throw error
            }
          },
          undoDuration
        )

        // Set timeout to finalize deletion
        timeoutRef.current = setTimeout(() => {
          setPendingItem(null)
          toastIdRef.current = null
        }, undoDuration)
      } catch (error) {
        setPendingItem(null)
        toast.error(`Failed to delete ${entityType.toLowerCase()} "${itemName}"`)
        throw error
      }
    },
    [
      onDelete,
      onRestore,
      undoDuration,
      getItemName,
      entityType,
      formatMessage,
      successMessage,
      restoredMessage,
    ]
  )

  const cancelDelete = useCallback(async () => {
    if (!pendingItem) return

    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current)
    }
    if (toastIdRef.current) {
      toast.dismiss(toastIdRef.current)
    }

    try {
      await onRestore(pendingItem)
      toast.success(formatMessage(restoredMessage, pendingItem))
    } finally {
      setPendingItem(null)
    }
  }, [pendingItem, onRestore, formatMessage, restoredMessage])

  return {
    handleDelete,
    isPending: pendingItem !== null,
    pendingItem,
    cancelDelete,
  }
}
