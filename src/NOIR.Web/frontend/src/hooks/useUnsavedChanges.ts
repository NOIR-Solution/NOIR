import { useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useBlocker, type Blocker } from 'react-router-dom'

interface UseUnsavedChangesOptions {
  /** Whether there are unsaved changes */
  isDirty: boolean
  /** Custom message for the browser confirmation dialog */
  message?: string
  /** Whether blocking is enabled (default: true) */
  enabled?: boolean
}

interface UseUnsavedChangesReturn {
  /** The blocker state from react-router */
  blocker: Blocker
  /** Proceed with navigation (confirm leaving) */
  proceed: () => void
  /** Cancel navigation (stay on page) */
  cancel: () => void
}

/**
 * Hook to warn users about unsaved changes when navigating away
 *
 * Handles both:
 * 1. SPA navigation (react-router) - shows custom dialog
 * 2. Browser navigation/refresh - shows browser's native dialog
 *
 * @example
 * const { blocker, proceed, cancel } = useUnsavedChanges({
 *   isDirty: form.formState.isDirty,
 * })
 *
 * // Show confirmation dialog when blocker is triggered
 * {blocker.state === 'blocked' && (
 *   <Credenza open onOpenChange={(open) => !open && cancel()}>
 *     <CredenzaContent>
 *       <CredenzaHeader>
 *         <CredenzaTitle>Unsaved changes</CredenzaTitle>
 *         <CredenzaDescription>
 *           You have unsaved changes. Are you sure you want to leave?
 *         </CredenzaDescription>
 *       </CredenzaHeader>
 *       <CredenzaBody />
 *       <CredenzaFooter>
 *         <Button variant="outline" onClick={cancel}>Stay</Button>
 *         <Button variant="destructive" onClick={proceed}>Leave</Button>
 *       </CredenzaFooter>
 *     </CredenzaContent>
 *   </Credenza>
 * )}
 */
export const useUnsavedChanges = ({
  isDirty,
  message,
  enabled = true,
}: UseUnsavedChangesOptions): UseUnsavedChangesReturn => {
  const { t } = useTranslation('common')
  const effectiveMessage = message ?? t('messages.unsavedChanges', 'You have unsaved changes. Are you sure you want to leave?')
  // Block SPA navigation
  const shouldBlock = enabled && isDirty
  const blocker = useBlocker(shouldBlock)

  // Handle browser navigation/refresh
  useEffect(() => {
    if (!shouldBlock) return

    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault()
      // Modern browsers ignore custom messages, but we set it anyway
      e.returnValue = effectiveMessage
      return effectiveMessage
    }

    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => window.removeEventListener('beforeunload', handleBeforeUnload)
  }, [shouldBlock, effectiveMessage])

  const proceed = useCallback(() => {
    if (blocker.state === 'blocked') {
      blocker.proceed()
    }
  }, [blocker])

  const cancel = useCallback(() => {
    if (blocker.state === 'blocked') {
      blocker.reset()
    }
  }, [blocker])

  return {
    blocker,
    proceed,
    cancel,
  }
}
