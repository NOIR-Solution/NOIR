import { useEffect, useCallback } from 'react'

interface Shortcut {
  /** The key to listen for (e.g., 'k', 'Escape', 'Enter') */
  key: string
  /** Require Cmd (Mac) or Ctrl (Windows/Linux) */
  metaKey?: boolean
  /** Require Shift key */
  shiftKey?: boolean
  /** Require Alt/Option key */
  altKey?: boolean
  /** Callback when shortcut is triggered */
  callback: (event: KeyboardEvent) => void
  /** Description for help dialog */
  description?: string
}

/**
 * Check if the event target is an input element
 */
function isInputElement(target: EventTarget | null): boolean {
  if (!target || !(target instanceof HTMLElement)) return false

  const tagName = target.tagName.toLowerCase()

  // Check for input, textarea, or contenteditable
  if (tagName === 'input' || tagName === 'textarea') return true
  if (target.isContentEditable) return true

  // Check for role="textbox"
  if (target.getAttribute('role') === 'textbox') return true

  return false
}

/**
 * Hook for registering global keyboard shortcuts
 *
 * Automatically ignores shortcuts when user is typing in input fields.
 * Supports modifier keys (Cmd/Ctrl, Shift, Alt).
 *
 * @example
 * useKeyboardShortcuts([
 *   { key: 'k', metaKey: true, callback: toggleCommandPalette },
 *   { key: 'Escape', callback: closeModal },
 *   { key: 'n', metaKey: true, shiftKey: true, callback: createNew },
 * ])
 */
export function useKeyboardShortcuts(shortcuts: Shortcut[]) {
  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      // Ignore when typing in inputs (unless it's Escape)
      if (isInputElement(event.target) && event.key !== 'Escape') {
        return
      }

      for (const shortcut of shortcuts) {
        // Check key match (case-insensitive)
        if (event.key.toLowerCase() !== shortcut.key.toLowerCase()) continue

        // Check meta key (Cmd on Mac, Ctrl on Windows/Linux)
        const metaMatch = shortcut.metaKey
          ? event.metaKey || event.ctrlKey
          : !event.metaKey && !event.ctrlKey

        // Check shift key
        const shiftMatch = shortcut.shiftKey ? event.shiftKey : !event.shiftKey

        // Check alt key
        const altMatch = shortcut.altKey ? event.altKey : !event.altKey

        if (metaMatch && shiftMatch && altMatch) {
          event.preventDefault()
          shortcut.callback(event)
          return
        }
      }
    },
    [shortcuts]
  )

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [handleKeyDown])
}

/**
 * Format shortcut for display
 *
 * @example
 * formatShortcut({ key: 'k', metaKey: true }) // "⌘K" on Mac, "Ctrl+K" on Windows
 */
export function formatShortcut(shortcut: Pick<Shortcut, 'key' | 'metaKey' | 'shiftKey' | 'altKey'>): string {
  const isMac = typeof navigator !== 'undefined' && /Mac|iPod|iPhone|iPad/.test(navigator.platform)

  const parts: string[] = []

  if (shortcut.metaKey) {
    parts.push(isMac ? '⌘' : 'Ctrl')
  }
  if (shortcut.shiftKey) {
    parts.push(isMac ? '⇧' : 'Shift')
  }
  if (shortcut.altKey) {
    parts.push(isMac ? '⌥' : 'Alt')
  }

  // Format special keys
  const keyDisplay = shortcut.key === 'Escape' ? 'Esc' : shortcut.key.toUpperCase()
  parts.push(keyDisplay)

  return isMac ? parts.join('') : parts.join('+')
}
