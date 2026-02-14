import { useCallback } from 'react'
import { useNavigate, type NavigateOptions, type To } from 'react-router-dom'
import { flushSync } from 'react-dom'

/**
 * Feature detection for the browser-native View Transitions API.
 * Supported in Chrome 111+, Edge 111+, Firefox 144+, Safari 18+.
 */
export const supportsViewTransitions =
  typeof document !== 'undefined' && 'startViewTransition' in document

/**
 * Start a view transition wrapping a DOM-updating callback.
 * Sets a direction attribute on <html> for CSS-based directional animations.
 * Falls back to calling the callback directly in unsupported browsers.
 */
export const startViewTransition = (
  callback: () => void,
  direction: 'forward' | 'back' = 'forward'
): void => {
  if (!supportsViewTransitions) {
    callback()
    return
  }

  document.documentElement.dataset.vtDirection = direction

  const transition = (document as Document & {
    startViewTransition: (cb: () => void) => { finished: Promise<void> }
  }).startViewTransition(() => {
    flushSync(callback)
  })

  transition.finished.finally(() => {
    delete document.documentElement.dataset.vtDirection
  })
}

/**
 * Hook that wraps useNavigate with the View Transitions API.
 *
 * When the browser supports View Transitions, navigation is wrapped in
 * document.startViewTransition() + flushSync() for smooth native
 * compositor-thread animations.
 *
 * @example
 * const navigate = useViewTransitionNavigate()
 * navigate('/portal/products')
 * navigate('/portal', { vtDirection: 'back' })
 */
export const useViewTransitionNavigate = () => {
  const navigate = useNavigate()

  return useCallback(
    (to: To | number, options?: NavigateOptions & { vtDirection?: 'forward' | 'back' }) => {
      const direction = options?.vtDirection ?? 'forward'

      // For numeric navigation (history.go), skip view transitions
      if (typeof to === 'number') {
        navigate(to)
        return
      }

      if (!supportsViewTransitions) {
        navigate(to, options)
        return
      }

      startViewTransition(() => {
        navigate(to, options)
      }, direction)
    },
    [navigate]
  )
}
