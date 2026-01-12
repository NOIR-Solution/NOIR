/**
 * Image upload hook for avatar and other image uploads
 *
 * Provides:
 * - File selection via hidden input
 * - Local preview before upload
 * - File validation (type, size)
 * - Clean up on unmount
 */
import { useState, useRef, useCallback, useEffect } from 'react'

interface UseImageUploadOptions {
  /** Maximum file size in bytes (default: 2MB) */
  maxSize?: number
  /** Allowed MIME types */
  allowedTypes?: string[]
}

interface UseImageUploadReturn {
  /** Current preview URL (local blob or existing URL) */
  previewUrl: string | null
  /** Currently selected file (null if none selected) */
  selectedFile: File | null
  /** Reference to hidden file input */
  fileInputRef: React.RefObject<HTMLInputElement | null>
  /** Trigger file picker dialog */
  openFilePicker: () => void
  /** Handle file selection from input */
  handleFileChange: (event: React.ChangeEvent<HTMLInputElement>) => void
  /** Clear current selection */
  clearSelection: () => void
  /** Set an existing URL as preview (for initial state) */
  setExistingUrl: (url: string | null) => void
  /** Validation error message (if any) */
  error: string | null
  /** Whether a file is selected but not yet uploaded */
  hasChanges: boolean
}

const DEFAULT_MAX_SIZE = 2 * 1024 * 1024 // 2MB
const DEFAULT_ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/gif', 'image/webp']

export function useImageUpload(options: UseImageUploadOptions = {}): UseImageUploadReturn {
  const {
    maxSize = DEFAULT_MAX_SIZE,
    allowedTypes = DEFAULT_ALLOWED_TYPES,
  } = options

  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [existingUrl, setExistingUrlState] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const fileInputRef = useRef<HTMLInputElement>(null)

  // Cleanup blob URLs on unmount or when preview changes
  useEffect(() => {
    return () => {
      if (previewUrl && previewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(previewUrl)
      }
    }
  }, [previewUrl])

  const openFilePicker = useCallback(() => {
    fileInputRef.current?.click()
  }, [])

  const handleFileChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement>) => {
      setError(null)
      const file = event.target.files?.[0]

      if (!file) {
        return
      }

      // Validate type
      if (!allowedTypes.includes(file.type)) {
        setError(`Invalid file type. Allowed: ${allowedTypes.map(t => t.split('/')[1]).join(', ')}`)
        return
      }

      // Validate size
      if (file.size > maxSize) {
        const maxSizeMB = (maxSize / (1024 * 1024)).toFixed(1)
        setError(`File too large. Maximum size: ${maxSizeMB}MB`)
        return
      }

      // Revoke previous blob URL if exists
      if (previewUrl && previewUrl.startsWith('blob:')) {
        URL.revokeObjectURL(previewUrl)
      }

      // Create preview
      const blobUrl = URL.createObjectURL(file)
      setPreviewUrl(blobUrl)
      setSelectedFile(file)
    },
    [allowedTypes, maxSize, previewUrl]
  )

  const clearSelection = useCallback(() => {
    if (previewUrl && previewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(previewUrl)
    }

    setPreviewUrl(existingUrl) // Revert to existing URL
    setSelectedFile(null)
    setError(null)

    // Reset file input
    if (fileInputRef.current) {
      fileInputRef.current.value = ''
    }
  }, [existingUrl, previewUrl])

  const setExistingUrl = useCallback((url: string | null) => {
    setExistingUrlState(url)
    if (!selectedFile) {
      setPreviewUrl(url)
    }
  }, [selectedFile])

  const hasChanges = selectedFile !== null

  return {
    previewUrl,
    selectedFile,
    fileInputRef,
    openFilePicker,
    handleFileChange,
    clearSelection,
    setExistingUrl,
    error,
    hasChanges,
  }
}
