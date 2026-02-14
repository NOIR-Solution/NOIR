import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core'
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  rectSortingStrategy,
} from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { Star, Trash2, Pencil, GripVertical } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Badge, Button, Input } from '@uikit'

import type { ProductImage } from '@/types/product'

interface SortableImageGalleryProps {
  images: ProductImage[]
  productName?: string
  isViewMode?: boolean
  onReorder: (images: ProductImage[]) => void
  onSetPrimary: (imageId: string) => void
  onDelete: (image: ProductImage) => void
  onUpdateAltText: (imageId: string, altText: string) => void
}

interface SortableImageCardProps {
  image: ProductImage
  productName?: string
  isViewMode?: boolean
  index: number
  editingAltText: boolean
  altTextValue: string
  onSetEditingAltText: (imageId: string | null, altText: string) => void
  onSetPrimary: (imageId: string) => void
  onDelete: (image: ProductImage) => void
  onSaveAltText: (imageId: string, altText: string) => void
}

const SortableImageCard = ({
  image,
  productName,
  isViewMode,
  index,
  editingAltText,
  altTextValue,
  onSetEditingAltText,
  onSetPrimary,
  onDelete,
  onSaveAltText,
}: SortableImageCardProps) => {
  const { t } = useTranslation('common')
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: image.id, disabled: isViewMode })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  }

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={cn(
        'space-y-2',
        isDragging && 'z-50 opacity-80'
      )}
    >
      <div
        className={cn(
          'relative aspect-square rounded-xl border overflow-hidden group shadow-sm hover:shadow-md transition-all duration-300',
          isDragging && 'ring-2 ring-primary shadow-lg'
        )}
      >
        {/* Drag handle */}
        {!isViewMode && (
          <div
            {...attributes}
            {...listeners}
            className="absolute top-2 right-2 z-10 p-1.5 rounded-md bg-white/90 shadow-sm opacity-0 group-hover:opacity-100 transition-opacity cursor-grab active:cursor-grabbing"
          >
            <GripVertical className="h-4 w-4 text-muted-foreground" />
          </div>
        )}

        <img
          src={image.url}
          alt={
            image.altText ||
            `${productName || t('products.product', 'Product')} - ${t('products.imageNumber', { number: index + 1, defaultValue: `Image ${index + 1}` })}`
          }
          className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
          draggable={false}
        />

        {image.isPrimary && (
          <Badge className="absolute top-2 left-2 text-xs shadow-md backdrop-blur-sm bg-primary/90">
            <Star className="h-3 w-3 mr-1 fill-current" />
            {t('products.primaryImage', 'Primary')}
          </Badge>
        )}

        {!isViewMode && (
          <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/20 to-transparent opacity-0 group-hover:opacity-100 transition-all duration-300 flex items-end justify-center gap-2 pb-3">
            {!image.isPrimary && (
              <Button
                size="icon"
                variant="secondary"
                className="h-8 w-8 shadow-lg backdrop-blur-sm bg-white/90 hover:bg-white transition-all duration-200 hover:scale-110 cursor-pointer"
                onClick={() => onSetPrimary(image.id)}
                aria-label={t('products.setAsPrimary', 'Set as primary image')}
              >
                <Star className="h-4 w-4" />
              </Button>
            )}
            <Button
              size="icon"
              variant="secondary"
              className="h-8 w-8 shadow-lg backdrop-blur-sm bg-white/90 hover:bg-white transition-all duration-200 hover:scale-110 cursor-pointer"
              onClick={() => onSetEditingAltText(image.id, image.altText || '')}
              aria-label={t('products.editAltText', 'Edit alt text')}
            >
              <Pencil className="h-4 w-4" />
            </Button>
            <Button
              size="icon"
              variant="destructive"
              className="h-8 w-8 shadow-lg backdrop-blur-sm hover:scale-110 transition-all duration-200 cursor-pointer"
              onClick={() => onDelete(image)}
              aria-label={t('products.deleteImage', 'Delete image')}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        )}
      </div>

      {/* Alt text display or edit form */}
      {editingAltText ? (
        <div className="flex gap-2">
          <Input
            value={altTextValue}
            onChange={(e) => onSetEditingAltText(image.id, e.target.value)}
            placeholder={t('products.imageAltText', 'Alt text')}
            className="flex-1 text-xs h-8"
          />
          <Button
            size="sm"
            className="h-8 cursor-pointer"
            onClick={() => onSaveAltText(image.id, altTextValue)}
          >
            {t('buttons.save', 'Save')}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            className="h-8 cursor-pointer"
            onClick={() => onSetEditingAltText(null, '')}
          >
            {t('buttons.cancel', 'Cancel')}
          </Button>
        </div>
      ) : (
        <p
          className="text-xs text-muted-foreground truncate"
          title={image.altText || t('products.noAltText', 'No alt text')}
        >
          {image.altText || (
            <span className="italic">{t('products.noAltText', 'No alt text')}</span>
          )}
        </p>
      )}
    </div>
  )
}

export const SortableImageGallery = ({
  images,
  productName,
  isViewMode = false,
  onReorder,
  onSetPrimary,
  onDelete,
  onUpdateAltText,
}: SortableImageGalleryProps) => {
  const { t } = useTranslation('common')
  const [editingImageId, setEditingImageId] = useState<string | null>(null)
  const [editingAltText, setEditingAltText] = useState('')

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8,
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  )

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event

      if (over && active.id !== over.id) {
        const oldIndex = images.findIndex((img) => img.id === active.id)
        const newIndex = images.findIndex((img) => img.id === over.id)

        const reorderedImages = arrayMove(images, oldIndex, newIndex).map(
          (img, index) => ({
            ...img,
            sortOrder: index,
          })
        )

        onReorder(reorderedImages)
      }
    },
    [images, onReorder]
  )

  const handleSetEditingAltText = useCallback(
    (imageId: string | null, altText: string) => {
      setEditingImageId(imageId)
      setEditingAltText(altText)
    },
    []
  )

  const handleSaveAltText = useCallback(
    (imageId: string, altText: string) => {
      onUpdateAltText(imageId, altText)
      setEditingImageId(null)
      setEditingAltText('')
    },
    [onUpdateAltText]
  )

  if (images.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-center">
        <div className="rounded-full bg-muted p-4 mb-4">
          <Star className="h-8 w-8 text-muted-foreground" />
        </div>
        <p className="text-muted-foreground">
          {t('products.noImages', 'No images yet')}
        </p>
        <p className="text-sm text-muted-foreground mt-1">
          {t('products.uploadFirstImage', 'Upload your first product image')}
        </p>
      </div>
    )
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragEnd={handleDragEnd}
    >
      <SortableContext
        items={images.map((img) => img.id)}
        strategy={rectSortingStrategy}
      >
        <div className="grid grid-cols-2 gap-3">
          {images.map((image, index) => (
            <SortableImageCard
              key={image.id}
              image={image}
              productName={productName}
              isViewMode={isViewMode}
              index={index}
              editingAltText={editingImageId === image.id}
              altTextValue={editingImageId === image.id ? editingAltText : ''}
              onSetEditingAltText={handleSetEditingAltText}
              onSetPrimary={onSetPrimary}
              onDelete={onDelete}
              onSaveAltText={handleSaveAltText}
            />
          ))}
        </div>
      </SortableContext>
    </DndContext>
  )
}

export default SortableImageGallery
