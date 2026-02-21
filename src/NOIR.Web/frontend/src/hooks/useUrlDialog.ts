import { useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'

interface UseUrlDialogOptions {
  /** The param value that triggers this dialog (e.g., 'create-category') */
  paramValue: string
  /** URL search param name. Default: 'dialog' */
  paramName?: string
}

/**
 * URL-synced dialog state hook.
 * - Opens dialog when URL contains ?dialog=paramValue
 * - Updates URL when dialog opens/closes (replace, no history pollution)
 * - Preserves existing search params
 * - Enables bookmarking and sharing dialog states
 *
 * Note: Unlike useUrlTab, this hook omits useTransition because dialog
 * open/close is an immediate user action that should not be deferred.
 *
 * Usage:
 * ```tsx
 * const { isOpen, open, onOpenChange } = useUrlDialog({ paramValue: 'create-category' })
 * <Credenza open={isOpen} onOpenChange={onOpenChange}>...</Credenza>
 * <Button onClick={open}>Create Category</Button>
 * ```
 */
export const useUrlDialog = ({ paramValue, paramName = 'dialog' }: UseUrlDialogOptions) => {
  const [searchParams, setSearchParams] = useSearchParams()

  const isOpen = searchParams.get(paramName) === paramValue

  const open = useCallback(() => {
    setSearchParams(prev => {
      const next = new URLSearchParams(prev)
      next.set(paramName, paramValue)
      return next
    }, { replace: true })
  }, [setSearchParams, paramName, paramValue])

  const close = useCallback(() => {
    setSearchParams(prev => {
      const next = new URLSearchParams(prev)
      next.delete(paramName)
      return next
    }, { replace: true })
  }, [setSearchParams, paramName])

  const onOpenChange = useCallback((isOpen: boolean) => {
    if (isOpen) open()
    else close()
  }, [open, close])

  return { isOpen, open, close, onOpenChange }
}
