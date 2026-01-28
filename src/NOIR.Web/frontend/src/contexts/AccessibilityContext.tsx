import {
  createContext,
  useContext,
  useState,
  useCallback,
  type ReactNode,
} from 'react'

type AnnouncementPriority = 'polite' | 'assertive'

interface AccessibilityContextType {
  /**
   * Announce a message to screen readers
   * @param message - The message to announce
   * @param priority - 'polite' (default) for non-urgent, 'assertive' for urgent
   */
  announce: (message: string, priority?: AnnouncementPriority) => void
}

const AccessibilityContext = createContext<AccessibilityContextType | undefined>(
  undefined
)

interface AccessibilityProviderProps {
  children: ReactNode
}

/**
 * AccessibilityProvider - Provides screen reader announcement capabilities
 *
 * Uses aria-live regions to announce dynamic content changes.
 * - 'polite' announcements wait for user to finish current task
 * - 'assertive' announcements interrupt immediately (use sparingly)
 */
export function AccessibilityProvider({ children }: AccessibilityProviderProps) {
  const [politeMessage, setPoliteMessage] = useState('')
  const [assertiveMessage, setAssertiveMessage] = useState('')

  const announce = useCallback(
    (message: string, priority: AnnouncementPriority = 'polite') => {
      if (priority === 'assertive') {
        // Clear and re-set to trigger announcement even if same message
        setAssertiveMessage('')
        // Small delay ensures screen reader detects the change
        setTimeout(() => setAssertiveMessage(message), 50)
      } else {
        setPoliteMessage('')
        setTimeout(() => setPoliteMessage(message), 50)
      }

      // Clear message after announcement to prevent re-reading on rerender
      setTimeout(() => {
        if (priority === 'assertive') {
          setAssertiveMessage('')
        } else {
          setPoliteMessage('')
        }
      }, 1000)
    },
    []
  )

  return (
    <AccessibilityContext.Provider value={{ announce }}>
      {children}
      {/* Hidden aria-live regions for screen reader announcements */}
      <div
        role="status"
        aria-live="polite"
        aria-atomic="true"
        className="sr-only"
      >
        {politeMessage}
      </div>
      <div
        role="alert"
        aria-live="assertive"
        aria-atomic="true"
        className="sr-only"
      >
        {assertiveMessage}
      </div>
    </AccessibilityContext.Provider>
  )
}

/**
 * Hook to access accessibility announcement function
 */
export function useAccessibility() {
  const context = useContext(AccessibilityContext)
  if (!context) {
    throw new Error(
      'useAccessibility must be used within an AccessibilityProvider'
    )
  }
  return context
}
