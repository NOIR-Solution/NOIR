/**
 * Image Upload Field Component
 *
 * A form field that combines image preview with upload functionality.
 * Auto-uploads on file selection (consistent with blog featured image).
 * Used for logos, favicons, and other branding images.
 */
import { useState, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Trash2, Loader2, ImageIcon } from 'lucide-react'
import { Button } from '../button/Button'
import { uploadMedia, type MediaFolder } from '@/services/media'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'

interface ImageUploadFieldProps {
  /** Current image URL */
  value: string
  /** Called when image URL changes (upload or remove) */
  onChange: (url: string) => void
  /** Media folder for upload (determines processing) */
  folder?: MediaFolder
  /** Placeholder text when no image is set */
  placeholder?: string
  /** Hint text below placeholder (e.g. file type / size info) */
  hint?: string
  /** Whether the field is disabled */
  disabled?: boolean
  /** Preview aspect ratio class (default: aspect-video for logos) */
  aspectClass?: string
  /** Accepted file types */
  accept?: string
  /** Max file size in bytes */
  maxSize?: number
  /** Label text */
  label?: string
}

export const ImageUploadField = ({
  value,
  onChange,
  folder = 'branding',
  placeholder,
  hint,
  disabled = false,
  aspectClass = 'aspect-video',
  accept = 'image/jpeg,image/png,image/gif,image/webp,image/x-icon,image/svg+xml',
  maxSize = 2 * 1024 * 1024,
  label,
}: ImageUploadFieldProps) => {
  const { t } = useTranslation('common')
  const effectivePlaceholder = placeholder ?? t('media.clickToUpload', 'Click to upload')
  const [uploading, setUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleClick = () => {
    if (!disabled && !uploading) {
      fileInputRef.current?.click()
    }
  }

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    // Reset file input for same file re-selection
    event.target.value = ''

    // Validate file type
    const allowedTypes = accept.split(',').map(t => t.trim())
    if (!allowedTypes.some(type => file.type === type || type === '*/*')) {
      setError(t('errors.invalidFileType', 'Invalid file type'))
      return
    }

    // Validate file size
    if (file.size > maxSize) {
      const maxSizeMB = (maxSize / 1024 / 1024).toFixed(1)
      setError(t('errors.fileTooLarge', { defaultValue: 'File must be less than {{maxSize}}MB', maxSize: maxSizeMB }))
      return
    }

    setError(null)
    setUploading(true)

    try {
      const result = await uploadMedia(file, folder)
      const uploadedUrl = result.defaultUrl || ''
      onChange(uploadedUrl)
      toast.success(t('media.imageUploaded', 'Image uploaded'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('errors.uploadFailed', 'Upload failed')
      setError(message)
      toast.error(message)
    } finally {
      setUploading(false)
    }
  }

  const handleRemove = (e: React.MouseEvent) => {
    e.stopPropagation()
    onChange('')
    setError(null)
  }

  return (
    <div className="space-y-2">
      {label && (
        <p className="text-sm font-medium leading-none">{label}</p>
      )}

      {/* Clickable Preview Area */}
      <div
        onClick={handleClick}
        className={cn(
          'relative w-full rounded-lg border-2 border-dashed bg-muted/50 flex items-center justify-center overflow-hidden group',
          aspectClass,
          disabled ? 'cursor-not-allowed opacity-50' : 'cursor-pointer hover:border-primary/50 hover:bg-muted/70',
          'transition-colors'
        )}
      >
        {uploading ? (
          <div className="flex flex-col items-center gap-2 text-muted-foreground">
            <Loader2 className="h-8 w-8 animate-spin" />
            <span className="text-xs">{t('media.uploading', 'Uploading...')}</span>
          </div>
        ) : value ? (
          <>
            <img
              src={value}
              alt={label || t('media.preview', 'Preview')}
              className="max-w-full max-h-full object-contain p-2"
            />
            {/* Delete button on hover */}
            {!disabled && (
              <Button
                type="button"
                size="icon"
                variant="destructive"
                onClick={handleRemove}
                className="absolute top-2 right-2 h-7 w-7 opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            )}
          </>
        ) : (
          <div className="flex flex-col items-center gap-2 text-muted-foreground">
            <ImageIcon className="h-8 w-8" />
            <span className="text-sm font-medium">{effectivePlaceholder}</span>
            {hint && <span className="text-xs">{hint}</span>}
          </div>
        )}
      </div>

      {/* Hidden File Input */}
      <input
        ref={fileInputRef}
        type="file"
        accept={accept}
        onChange={handleFileChange}
        className="hidden"
        aria-hidden="true"
        disabled={disabled || uploading}
      />

      {/* Error message */}
      {error && (
        <p className="text-sm font-medium text-destructive">{error}</p>
      )}
    </div>
  )
}
