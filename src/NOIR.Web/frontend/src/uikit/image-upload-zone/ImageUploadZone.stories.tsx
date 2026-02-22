import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { Upload, ImagePlus, X, Loader2 } from 'lucide-react'
import { Button, Progress } from '@uikit'
import { cn } from '@/lib/utils'

// --- Visual Replica ---
// ImageUploadZone uses react-dropzone and react-i18next.
// This self-contained demo replicates the visual appearance and interactions
// without requiring those external contexts.

type UploadStatus = 'uploading' | 'success' | 'error'

interface MockUpload {
  id: string
  name: string
  progress: number
  status: UploadStatus
  error?: string
}

interface ImageUploadZoneDemoProps {
  disabled?: boolean
  maxSizeMB?: number
  simulateError?: boolean
  initialUploads?: MockUpload[]
  isDragOver?: boolean
}

const ImageUploadZoneDemo = ({
  disabled = false,
  simulateError = false,
  initialUploads = [],
  isDragOver = false,
}: ImageUploadZoneDemoProps) => {
  const [uploads, setUploads] = useState<MockUpload[]>(initialUploads)
  const [dragOver, setDragOver] = useState(isDragOver)

  const handleSimulateUpload = () => {
    if (disabled) return
    const id = String(Date.now())
    const newUpload: MockUpload = {
      id,
      name: `product-image-${id.slice(-4)}.jpg`,
      progress: 0,
      status: 'uploading',
    }
    setUploads((prev) => [...prev, newUpload])

    // Simulate progress
    let progress = 0
    const interval = setInterval(() => {
      progress += 20
      if (progress >= 100) {
        clearInterval(interval)
        if (simulateError) {
          setUploads((prev) =>
            prev.map((u) =>
              u.id === id
                ? { ...u, status: 'error', error: 'File size exceeds limit' }
                : u
            )
          )
        } else {
          setUploads((prev) =>
            prev.map((u) =>
              u.id === id ? { ...u, progress: 100, status: 'success' } : u
            )
          )
          setTimeout(() => {
            setUploads((prev) => prev.filter((u) => u.id !== id))
          }, 2000)
        }
      } else {
        setUploads((prev) =>
          prev.map((u) => (u.id === id ? { ...u, progress } : u))
        )
      }
    }, 300)
  }

  const removeUpload = (id: string) => {
    setUploads((prev) => prev.filter((u) => u.id !== id))
  }

  return (
    <div className="space-y-4">
      <div
        onClick={handleSimulateUpload}
        onMouseEnter={() => !disabled && setDragOver(false)}
        className={cn(
          'relative flex flex-col items-center justify-center rounded-xl border-2 border-dashed p-8 transition-all duration-200 cursor-pointer',
          dragOver && 'border-primary bg-primary/5 scale-[1.02]',
          disabled && 'cursor-not-allowed opacity-50',
          !dragOver && !disabled && 'hover:border-primary/50 hover:bg-muted/30'
        )}
      >
        <div className="flex flex-col items-center gap-3 text-center">
          <div
            className={cn(
              'rounded-full p-4 transition-colors',
              dragOver ? 'bg-primary/10' : 'bg-muted'
            )}
          >
            {dragOver ? (
              <Upload className="h-8 w-8 text-primary animate-bounce" />
            ) : (
              <ImagePlus className="h-8 w-8 text-muted-foreground" />
            )}
          </div>
          <div className="space-y-1">
            <p className="font-medium">
              {dragOver ? 'Drop images here' : 'Drag & drop images here'}
            </p>
            <p className="text-sm text-muted-foreground">
              {disabled ? 'Upload disabled' : 'or click to select files'}
            </p>
          </div>
          <p className="text-xs text-muted-foreground">
            Supported: JPEG, PNG, GIF, WebP, AVIF (max 10MB)
          </p>
        </div>
      </div>

      {uploads.length > 0 && (
        <div className="space-y-2">
          {uploads.map((upload) => (
            <div
              key={upload.id}
              className={cn(
                'flex items-center gap-3 rounded-lg border p-3 transition-all',
                upload.status === 'success' && 'border-green-200 bg-green-50',
                upload.status === 'error' &&
                  'border-destructive/30 bg-destructive/5'
              )}
            >
              <div className="h-12 w-12 flex-shrink-0 overflow-hidden rounded-md bg-muted flex items-center justify-center">
                <ImagePlus className="h-5 w-5 text-muted-foreground" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{upload.name}</p>
                {upload.status === 'uploading' && (
                  <Progress value={upload.progress} className="h-1.5 mt-1" />
                )}
                {upload.status === 'error' && (
                  <p className="text-xs text-destructive mt-0.5">{upload.error}</p>
                )}
                {upload.status === 'success' && (
                  <p className="text-xs text-green-600 mt-0.5">Upload complete</p>
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
                    onClick={() => removeUpload(upload.id)}
                    aria-label="Remove failed upload"
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

// --- Meta ---

const meta = {
  title: 'UIKit/ImageUploadZone',
  component: ImageUploadZoneDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Drop zone for uploading product images. Supports drag-and-drop and click-to-select. ' +
          'Real component uses react-dropzone; this demo simulates upload progress by clicking the zone.',
      },
    },
  },
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 480 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ImageUploadZoneDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: { story: 'Default idle state. Click the zone to simulate an upload.' },
    },
  },
  args: {},
}

export const DragOver: Story = {
  parameters: {
    docs: {
      description: { story: 'Appearance when a file is being dragged over the drop zone.' },
    },
  },
  args: {
    isDragOver: true,
  },
}

export const Uploading: Story = {
  parameters: {
    docs: {
      description: { story: 'Active upload in progress with progress bar.' },
    },
  },
  args: {
    initialUploads: [
      { id: '1', name: 'hero-shot.jpg', progress: 45, status: 'uploading' },
      { id: '2', name: 'side-profile.png', progress: 80, status: 'uploading' },
    ],
  },
}

export const UploadSuccess: Story = {
  parameters: {
    docs: {
      description: { story: 'Upload completed successfully — shows green state briefly before auto-removing.' },
    },
  },
  args: {
    initialUploads: [
      { id: '1', name: 'product-hero.jpg', progress: 100, status: 'success' },
    ],
  },
}

export const UploadError: Story = {
  parameters: {
    docs: {
      description: { story: 'Failed upload with error message and dismiss button. Click zone to simulate a new error upload.' },
    },
  },
  args: {
    simulateError: true,
    initialUploads: [
      {
        id: '1',
        name: 'large-file.bmp',
        progress: 0,
        status: 'error',
        error: 'File size exceeds 10MB limit',
      },
    ],
  },
}

export const Disabled: Story = {
  parameters: {
    docs: {
      description: { story: 'Disabled state — cursor is not-allowed and zone is dimmed.' },
    },
  },
  args: {
    disabled: true,
  },
}

export const MixedStates: Story = {
  parameters: {
    docs: {
      description: { story: 'Multiple uploads simultaneously in different states.' },
    },
  },
  args: {
    initialUploads: [
      { id: '1', name: 'hero-shot.jpg', progress: 100, status: 'success' },
      { id: '2', name: 'side-view.png', progress: 60, status: 'uploading' },
      {
        id: '3',
        name: 'too-large.bmp',
        progress: 0,
        status: 'error',
        error: 'File size exceeds 10MB limit',
      },
    ],
  },
}
