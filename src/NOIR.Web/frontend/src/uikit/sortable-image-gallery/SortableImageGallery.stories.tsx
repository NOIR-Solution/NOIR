import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { Star, Trash2, Pencil, GripVertical } from 'lucide-react'
import { Badge, Button, Input } from '@uikit'

// --- Visual Replica ---
// SortableImageGallery uses @dnd-kit and react-i18next which require special providers.
// This self-contained demo replicates the visual appearance and interactions
// without requiring those external contexts.

interface DemoImage {
  id: string
  url: string
  altText?: string
  isPrimary?: boolean
}

const makePlaceholder = (bg: string, label: string) => {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="400" height="400"><rect width="400" height="400" fill="${bg}"/><text x="200" y="215" font-size="20" fill="white" text-anchor="middle" font-family="system-ui,sans-serif">${label}</text></svg>`
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`
}

const PALETTES = [
  { bg: '#f87171', label: 'Image 1' },
  { bg: '#60a5fa', label: 'Image 2' },
  { bg: '#34d399', label: 'Image 3' },
  { bg: '#fbbf24', label: 'Image 4' },
  { bg: '#a78bfa', label: 'Image 5' },
  { bg: '#f472b6', label: 'Image 6' },
  { bg: '#38bdf8', label: 'Image 7' },
  { bg: '#fb923c', label: 'Image 8' },
]

const makeImage = (
  id: string,
  paletteIndex: number,
  altText?: string,
  isPrimary = false
): DemoImage => ({
  id,
  url: makePlaceholder(PALETTES[paletteIndex % PALETTES.length].bg, PALETTES[paletteIndex % PALETTES.length].label),
  altText,
  isPrimary,
})

// --- Single Image Card ---

interface ImageCardProps {
  image: DemoImage
  index: number
  isViewMode: boolean
  isEditing: boolean
  editingAlt: string
  onEditAlt: (id: string, alt: string) => void
  onChangeAlt: (alt: string) => void
  onSaveAlt: (id: string) => void
  onCancelEdit: () => void
  onSetPrimary: (id: string) => void
  onDelete: (id: string) => void
}

const ImageCard = ({
  image,
  index,
  isViewMode,
  isEditing,
  editingAlt,
  onEditAlt,
  onChangeAlt,
  onSaveAlt,
  onCancelEdit,
  onSetPrimary,
  onDelete,
}: ImageCardProps) => (
  <div className="space-y-2">
    <div className="relative aspect-square rounded-xl border overflow-hidden group shadow-sm hover:shadow-md transition-all duration-300">
      {/* Drag handle — visible on hover */}
      {!isViewMode && (
        <div className="absolute top-2 right-2 z-10 p-1.5 rounded-md bg-white/90 shadow-sm opacity-0 group-hover:opacity-100 transition-opacity cursor-grab">
          <GripVertical className="h-4 w-4 text-muted-foreground" />
        </div>
      )}

      <img
        src={image.url}
        alt={image.altText || `Product - Image ${index + 1}`}
        className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
        draggable={false}
      />

      {image.isPrimary && (
        <Badge className="absolute top-2 left-2 text-xs shadow-md backdrop-blur-sm bg-primary/90">
          <Star className="h-3 w-3 mr-1 fill-current" />
          Primary
        </Badge>
      )}

      {/* Action overlay — visible on hover */}
      {!isViewMode && (
        <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-all duration-300 flex items-end justify-center gap-2 pb-3">
          {!image.isPrimary && (
            <Button
              size="icon"
              variant="secondary"
              className="h-8 w-8 shadow-lg backdrop-blur-sm bg-white/90 hover:bg-white transition-all duration-200 hover:scale-110 cursor-pointer"
              onClick={() => onSetPrimary(image.id)}
              aria-label="Set as primary image"
            >
              <Star className="h-4 w-4" />
            </Button>
          )}
          <Button
            size="icon"
            variant="secondary"
            className="h-8 w-8 shadow-lg backdrop-blur-sm bg-white/90 hover:bg-white transition-all duration-200 hover:scale-110 cursor-pointer"
            onClick={() => onEditAlt(image.id, image.altText || '')}
            aria-label="Edit alt text"
          >
            <Pencil className="h-4 w-4" />
          </Button>
          <Button
            size="icon"
            variant="destructive"
            className="h-8 w-8 shadow-lg backdrop-blur-sm hover:scale-110 transition-all duration-200 cursor-pointer"
            onClick={() => onDelete(image.id)}
            aria-label="Delete image"
          >
            <Trash2 className="h-4 w-4" />
          </Button>
        </div>
      )}
    </div>

    {/* Alt text display / edit */}
    {isEditing ? (
      <div className="flex gap-2">
        <Input
          value={editingAlt}
          onChange={(e) => onChangeAlt(e.target.value)}
          placeholder="Alt text"
          className="flex-1 text-xs h-8"
          autoFocus
        />
        <Button
          size="sm"
          className="h-8 cursor-pointer"
          onClick={() => onSaveAlt(image.id)}
        >
          Save
        </Button>
        <Button
          size="sm"
          variant="ghost"
          className="h-8 cursor-pointer"
          onClick={onCancelEdit}
        >
          Cancel
        </Button>
      </div>
    ) : (
      <p
        className="text-xs text-muted-foreground truncate"
        title={image.altText || 'No alt text'}
      >
        {image.altText || <span className="italic">No alt text</span>}
      </p>
    )}
  </div>
)

// --- Demo Component ---

interface SortableImageGalleryDemoProps {
  images: DemoImage[]
  isViewMode?: boolean
}

const SortableImageGalleryDemo = ({
  images: initialImages,
  isViewMode = false,
}: SortableImageGalleryDemoProps) => {
  const [images, setImages] = useState(initialImages)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingAlt, setEditingAlt] = useState('')

  const handleEditAlt = (id: string, currentAlt: string) => {
    setEditingId(id)
    setEditingAlt(currentAlt)
  }

  const handleSaveAlt = (id: string) => {
    setImages((imgs) =>
      imgs.map((img) => (img.id === id ? { ...img, altText: editingAlt } : img))
    )
    setEditingId(null)
    setEditingAlt('')
  }

  const handleSetPrimary = (id: string) => {
    setImages((imgs) =>
      imgs.map((img) => ({ ...img, isPrimary: img.id === id }))
    )
  }

  const handleDelete = (id: string) => {
    setImages((imgs) => imgs.filter((img) => img.id !== id))
    if (editingId === id) setEditingId(null)
  }

  if (images.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <div className="rounded-full bg-muted p-4 mb-4">
          <Star className="h-8 w-8 text-muted-foreground" />
        </div>
        <p className="text-muted-foreground">No images yet</p>
        <p className="text-sm text-muted-foreground mt-1">
          Upload your first product image
        </p>
      </div>
    )
  }

  return (
    <div className="grid grid-cols-2 gap-3">
      {images.map((image, index) => (
        <ImageCard
          key={image.id}
          image={image}
          index={index}
          isViewMode={isViewMode}
          isEditing={editingId === image.id}
          editingAlt={editingAlt}
          onEditAlt={handleEditAlt}
          onChangeAlt={setEditingAlt}
          onSaveAlt={handleSaveAlt}
          onCancelEdit={() => setEditingId(null)}
          onSetPrimary={handleSetPrimary}
          onDelete={handleDelete}
        />
      ))}
    </div>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/SortableImageGallery',
  component: SortableImageGalleryDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
  },
} satisfies Meta<typeof SortableImageGalleryDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Drag-and-drop reordering is not functional in this visual replica (requires @dnd-kit context). ' +
          'The drag handle is visible on hover but non-functional. ' +
          'All other interactions — set primary, edit alt text, delete — are fully interactive.',
      },
    },
  },
  args: {
    images: [
      makeImage('1', 0, 'Hero shot front view', true),
      makeImage('2', 1, 'Side profile'),
      makeImage('3', 2, 'Detail close-up'),
      makeImage('4', 3),
    ],
  },
}

export const Empty: Story = {
  args: {
    images: [],
  },
}

export const SingleImage: Story = {
  args: {
    images: [makeImage('1', 4, 'Product hero image', true)],
  },
}

export const ManyImages: Story = {
  args: {
    images: PALETTES.map((_, i) =>
      makeImage(String(i + 1), i, i === 0 ? 'Primary hero image' : undefined, i === 0)
    ),
  },
}

export const ViewMode: Story = {
  args: {
    images: [
      makeImage('1', 0, 'Hero shot', true),
      makeImage('2', 1, 'Side view'),
      makeImage('3', 2, 'Detail close-up'),
      makeImage('4', 3),
    ],
    isViewMode: true,
  },
}
