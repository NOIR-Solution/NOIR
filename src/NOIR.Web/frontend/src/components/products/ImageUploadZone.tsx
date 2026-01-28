import { useCallback, useState } from 'react'
import { useDropzone } from 'react-dropzone'
import { useTranslation } from 'react-i18next'
import { Upload, X, Loader2, ImagePlus } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'

interface ImageUploadZoneProps {
  onUpload: (file: File) => Promise<void>
  disabled?: boolean
  maxSizeMB?: number
  acceptedFormats?: string[]
  className?: string
}

interface UploadProgress {
  file: File
  progress: number
  status: 'uploading' | 'success' | 'error'
  error?: string
}

export function ImageUploadZone({
  onUpload,
  disabled = false,
  maxSizeMB = 10,
  acceptedFormats = ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/avif'],
  className,
}: ImageUploadZoneProps) {
  const { t } = useTranslation('common')
  const [uploads, setUploads] = useState<UploadProgress[]>([])

  const onDrop = useCallback(
    async (acceptedFiles: File[]) => {
      for (const file of acceptedFiles) {
        // Add to uploads list
        setUploads((prev) => [
          ...prev,
          { file, progress: 0, status: 'uploading' },
        ])

        try {
          await onUpload(file)
          setUploads((prev) =>
            prev.map((u) =>
              u.file === file ? { ...u, progress: 100, status: 'success' } : u
            )
          )
          // Remove successful upload after 2 seconds
          setTimeout(() => {
            setUploads((prev) => prev.filter((u) => u.file !== file))
          }, 2000)
        } catch (error) {
          setUploads((prev) =>
            prev.map((u) =>
              u.file === file
                ? {
                    ...u,
                    status: 'error',
                    error: error instanceof Error ? error.message : 'Upload failed',
                  }
                : u
            )
          )
        }
      }
    },
    [onUpload]
  )

  const { getRootProps, getInputProps, isDragActive, isDragReject } = useDropzone({
    onDrop,
    accept: acceptedFormats.reduce((acc, format) => ({ ...acc, [format]: [] }), {}),
    maxSize: maxSizeMB * 1024 * 1024,
    disabled,
    multiple: true,
  })

  const removeUpload = (file: File) => {
    setUploads((prev) => prev.filter((u) => u.file !== file))
  }

  return (
    <div className={cn('space-y-4', className)}>
      <div
        {...getRootProps()}
        className={cn(
          'relative flex flex-col items-center justify-center rounded-xl border-2 border-dashed p-8 transition-all duration-200 cursor-pointer',
          isDragActive && !isDragReject && 'border-primary bg-primary/5 scale-[1.02]',
          isDragReject && 'border-destructive bg-destructive/5',
          disabled && 'cursor-not-allowed opacity-50',
          !isDragActive && !disabled && 'hover:border-primary/50 hover:bg-muted/30'
        )}
      >
        <input {...getInputProps()} />
        <div className="flex flex-col items-center gap-3 text-center">
          <div
            className={cn(
              'rounded-full p-4 transition-colors',
              isDragActive ? 'bg-primary/10' : 'bg-muted'
            )}
          >
            {isDragActive ? (
              <Upload className="h-8 w-8 text-primary animate-bounce" />
            ) : (
              <ImagePlus className="h-8 w-8 text-muted-foreground" />
            )}
          </div>
          <div className="space-y-1">
            <p className="font-medium">
              {isDragActive
                ? t('products.dropImagesHere', 'Drop images here')
                : t('products.dragAndDropImages', 'Drag & drop images here')}
            </p>
            <p className="text-sm text-muted-foreground">
              {t('products.orClickToSelect', 'or click to select files')}
            </p>
          </div>
          <p className="text-xs text-muted-foreground">
            {t('products.imageUploadHint', {
              formats: 'JPEG, PNG, GIF, WebP, AVIF',
              maxSize: maxSizeMB,
              defaultValue: `Supported: JPEG, PNG, GIF, WebP, AVIF (max ${maxSizeMB}MB)`,
            })}
          </p>
        </div>
      </div>

      {/* Upload progress list */}
      {uploads.length > 0 && (
        <div className="space-y-2">
          {uploads.map((upload, index) => (
            <div
              key={`${upload.file.name}-${index}`}
              className={cn(
                'flex items-center gap-3 rounded-lg border p-3 transition-all',
                upload.status === 'success' && 'border-green-200 bg-green-50',
                upload.status === 'error' && 'border-destructive/30 bg-destructive/5'
              )}
            >
              {/* Thumbnail preview */}
              <div className="h-12 w-12 flex-shrink-0 overflow-hidden rounded-md bg-muted">
                <img
                  src={URL.createObjectURL(upload.file)}
                  alt={upload.file.name}
                  className="h-full w-full object-cover"
                />
              </div>

              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{upload.file.name}</p>
                {upload.status === 'uploading' && (
                  <Progress value={upload.progress} className="h-1.5 mt-1" />
                )}
                {upload.status === 'error' && (
                  <p className="text-xs text-destructive mt-0.5">{upload.error}</p>
                )}
                {upload.status === 'success' && (
                  <p className="text-xs text-green-600 mt-0.5">
                    {t('products.uploadSuccess', 'Upload complete')}
                  </p>
                )}
              </div>

              <div className="flex items-center gap-2">
                {upload.status === 'uploading' && (
                  <Loader2 className="h-4 w-4 animate-spin text-primary" />
                )}
                {upload.status === 'error' && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 cursor-pointer"
                    onClick={() => removeUpload(upload.file)}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default ImageUploadZone
