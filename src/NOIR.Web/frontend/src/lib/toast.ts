import { toast as sonnerToast } from 'sonner'
import type { ExternalToast } from 'sonner'

interface ActionButton {
  label: string
  onClick: () => void
}

interface EnhancedToastOptions extends ExternalToast {
  /** Primary action button */
  action?: ActionButton
  /** Cancel/secondary action button */
  cancel?: ActionButton
}

interface ProgressToastHandle {
  /** Update progress (0-100) and optionally description */
  update: (progress: number, description?: string) => void
  /** Mark as success and dismiss */
  success: (message: string) => void
  /** Mark as error and dismiss */
  error: (message: string) => void
  /** Dismiss without status change */
  dismiss: () => void
}

interface ProgressToastOptions {
  /** Initial title */
  title: string
  /** Initial description */
  description?: string
  /** Callback when operation completes successfully */
  onComplete?: () => void
}

/**
 * Enhanced toast wrapper with additional features:
 * - Action buttons
 * - Undo capability with timer
 * - Progress tracking for long operations
 */
export const toast = {
  /**
   * Show a success toast
   */
  success: (message: string, options?: EnhancedToastOptions) => {
    return sonnerToast.success(message, {
      ...options,
      action: options?.action
        ? {
            label: options.action.label,
            onClick: options.action.onClick,
          }
        : undefined,
      cancel: options?.cancel
        ? {
            label: options.cancel.label,
            onClick: options.cancel.onClick,
          }
        : undefined,
    })
  },

  /**
   * Show an error toast
   */
  error: (message: string, options?: EnhancedToastOptions) => {
    return sonnerToast.error(message, {
      ...options,
      action: options?.action
        ? {
            label: options.action.label,
            onClick: options.action.onClick,
          }
        : undefined,
    })
  },

  /**
   * Show a loading toast
   */
  loading: (message: string, options?: ExternalToast) => {
    return sonnerToast.loading(message, options)
  },

  /**
   * Show an info toast
   */
  info: (message: string, options?: EnhancedToastOptions) => {
    return sonnerToast.info(message, {
      ...options,
      action: options?.action
        ? {
            label: options.action.label,
            onClick: options.action.onClick,
          }
        : undefined,
    })
  },

  /**
   * Show an undo toast with timer
   *
   * Returns a promise that resolves to true if undo was clicked,
   * or false if the timer expired.
   */
  undo: (
    message: string,
    onUndo: () => void | Promise<void>,
    duration = 5000,
    undoLabel = 'Undo'
  ): string | number => {
    let undone = false

    const id = sonnerToast(message, {
      duration,
      action: {
        label: undoLabel,
        onClick: async () => {
          undone = true
          await onUndo()
          sonnerToast.dismiss(id)
        },
      },
      onDismiss: () => {
        // Timer expired without undo - action is final
        if (!undone) {
          // Action was not undone, it's permanent now
        }
      },
    })

    return id
  },

  /**
   * Create a progress toast for long-running operations
   *
   * @example
   * const progress = toast.progress({ title: 'Uploading files...' })
   * progress.update(50, '3 of 6 files uploaded')
   * progress.success('All files uploaded!')
   */
  progress: (options: ProgressToastOptions): ProgressToastHandle => {
    const id = sonnerToast.loading(options.title, {
      description: options.description,
    })

    return {
      update: (progress: number, description?: string) => {
        sonnerToast.loading(options.title, {
          id,
          description: description || `${Math.round(progress)}% complete`,
        })
      },
      success: (message: string) => {
        sonnerToast.success(message, { id })
        options.onComplete?.()
      },
      error: (message: string) => {
        sonnerToast.error(message, { id })
      },
      dismiss: () => {
        sonnerToast.dismiss(id)
      },
    }
  },

  /**
   * Dismiss a toast by ID
   */
  dismiss: (id?: string | number) => {
    sonnerToast.dismiss(id)
  },
}

// Re-export sonnerToast for cases where you need direct access
export { sonnerToast }
