import { useCallback, useState, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Upload, X, Loader2, ImagePlus } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'

interface LogoUploadFieldProps {
  value?: string | null
  onChange: (url: string | null) => void
  onUpload: (file: File) => Promise<string>
  disabled?: boolean
  maxSizeMB?: number
  acceptedFormats?: string[]
  className?: string
  placeholder?: string
}

export const LogoUploadField = ({
  value,
  onChange,
  onUpload,
  disabled = false,
  maxSizeMB = 5,
  acceptedFormats = ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/svg+xml'],
  className,
  placeholder,
}: LogoUploadFieldProps) => {
  const { t } = useTranslation('common')
  const [isUploading, setIsUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isDragging, setIsDragging] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  const handleFile = useCallback(
    async (file: File) => {
      // Validate file type
      if (!acceptedFormats.includes(file.type)) {
        setError(t('errors.invalidFileType', 'Invalid file type'))
        return
      }

      // Validate file size
      if (file.size > maxSizeMB * 1024 * 1024) {
        setError(t('errors.fileTooLarge', { maxSize: maxSizeMB, defaultValue: 'File must be less than {{maxSize}}MB' }))
        return
      }

      setError(null)
      setIsUploading(true)

      try {
        const url = await onUpload(file)
        onChange(url)
      } catch (err) {
        setError(err instanceof Error ? err.message : t('errors.uploadFailed', 'Upload failed'))
      } finally {
        setIsUploading(false)
      }
    },
    [acceptedFormats, maxSizeMB, onUpload, onChange, t]
  )

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      handleFile(file)
    }
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
    if (!disabled) {
      setIsDragging(true)
    }
  }

  const handleDragLeave = () => {
    setIsDragging(false)
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    if (disabled) return

    const file = e.dataTransfer.files?.[0]
    if (file) {
      handleFile(file)
    }
  }

  const handleRemove = () => {
    onChange(null)
    if (inputRef.current) {
      inputRef.current.value = ''
    }
  }

  const handleClick = () => {
    if (!disabled && !isUploading) {
      inputRef.current?.click()
    }
  }

  return (
    <div className={cn('space-y-2', className)}>
      <input
        ref={inputRef}
        type="file"
        accept={acceptedFormats.join(',')}
        onChange={handleInputChange}
        className="hidden"
        disabled={disabled}
      />

      {value ? (
        // Show preview with remove option
        <div className="relative group">
          <div className="flex items-center gap-3 rounded-lg border p-3 bg-muted/30">
            <div className="h-12 w-12 flex-shrink-0 overflow-hidden rounded-md bg-background border">
              <img
                src={value}
                alt={t('labels.logoPreview', 'Logo preview')}
                className="h-full w-full object-contain"
              />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm text-muted-foreground truncate">
                {t('labels.logoUploaded', 'Logo uploaded')}
              </p>
            </div>
            {!disabled && (
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-8 w-8 cursor-pointer"
                onClick={handleRemove}
                aria-label={t('labels.removeLogo', 'Remove logo')}
              >
                <X className="h-4 w-4" />
              </Button>
            )}
          </div>
        </div>
      ) : (
        // Show upload zone
        <div
          onClick={handleClick}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          className={cn(
            'flex items-center gap-3 rounded-lg border-2 border-dashed p-4 transition-all cursor-pointer',
            isDragging && 'border-primary bg-primary/5',
            disabled && 'cursor-not-allowed opacity-50',
            !isDragging && !disabled && 'hover:border-primary/50 hover:bg-muted/30'
          )}
        >
          <div
            className={cn(
              'rounded-full p-2 transition-colors',
              isDragging ? 'bg-primary/10' : 'bg-muted'
            )}
          >
            {isUploading ? (
              <Loader2 className="h-5 w-5 text-primary animate-spin" />
            ) : isDragging ? (
              <Upload className="h-5 w-5 text-primary" />
            ) : (
              <ImagePlus className="h-5 w-5 text-muted-foreground" />
            )}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium">
              {isUploading
                ? t('labels.uploading', 'Uploading...')
                : placeholder || t('labels.uploadLogo', 'Upload logo')}
            </p>
            <p className="text-xs text-muted-foreground">
              {t('labels.dragOrClick', 'Drag & drop or click to select')}
            </p>
          </div>
        </div>
      )}

      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  )
}
