import { useState, useEffect, useRef } from 'react'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Editor } from '@tinymce/tinymce-react'
import type { Editor as TinyMCEEditor } from 'tinymce'

// Import TinyMCE 6 for self-hosted usage
/* eslint-disable import/no-unresolved */
import 'tinymce/tinymce'
import 'tinymce/models/dom'
import 'tinymce/themes/silver'
import 'tinymce/icons/default'
import 'tinymce/plugins/advlist'
import 'tinymce/plugins/autolink'
import 'tinymce/plugins/lists'
import 'tinymce/plugins/link'
import 'tinymce/plugins/image'
import 'tinymce/plugins/charmap'
import 'tinymce/plugins/preview'
import 'tinymce/plugins/anchor'
import 'tinymce/plugins/searchreplace'
import 'tinymce/plugins/visualblocks'
import 'tinymce/plugins/code'
import 'tinymce/plugins/fullscreen'
import 'tinymce/plugins/insertdatetime'
import 'tinymce/plugins/media'
import 'tinymce/plugins/table'
import 'tinymce/plugins/wordcount'
/* eslint-enable import/no-unresolved */

import {
  ArrowLeft,
  Package,
  Save,
  Send,
  Plus,
  Trash2,
  ImagePlus,
  Star,
  Pencil,
  AlertTriangle,
  Loader2,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
  Textarea,
} from '@uikit'

import { useProduct } from '@/portal-app/products/states/useProducts'
import { useProductCategories } from '@/portal-app/products/states/useProductCategories'
import { useActiveBrands } from '@/portal-app/brands/states/useBrands'
import {
  createProduct,
  updateProduct,
  publishProduct,
  addProductVariant,
  deleteProductVariant,
  addProductImage,
  updateProductImage,
  deleteProductImage,
  setPrimaryProductImage,
  uploadProductImage,
  reorderProductImages,
} from '@/services/products'
import { ImageUploadZone } from '@/components/products/ImageUploadZone'
import { SortableImageGallery } from '@/components/products/SortableImageGallery'
import { ProductAttributesSection } from '@/components/products/ProductAttributesSection'
import { ProductAttributesSectionCreate } from '@/components/products/ProductAttributesSectionCreate'
import { EditableVariantsTable } from '@/components/products/EditableVariantsTable'
import { ProductActivityLog } from '../../components/products/ProductActivityLog'
import { useBulkUpdateProductAttributes } from '@/portal-app/products/states/useProductAttributes'
import { useStockHistory } from '@/hooks/useStockHistory'
import { StockHistoryTimeline, type StockMovement, type StockMovementType } from '@/components/products/StockHistoryTimeline'
import type { InventoryMovement, InventoryMovementType } from '@/types/inventory'
import { uploadMedia } from '@/services/media'
import { toast } from 'sonner'
import { ApiError } from '@/services/apiClient'
import type { ProductVariant, ProductImage, CreateProductVariantRequest, CreateProductImageRequest } from '@/types/product'

// Local types for create mode (before product exists)
interface LocalVariant {
  tempId: string
  name: string
  price: number
  sku: string | null
  compareAtPrice: number | null
  stockQuantity: number
  sortOrder: number
}

interface TempImage {
  tempId: string
  url: string
  altText: string | null
  sortOrder: number
  isPrimary: boolean
}
import { generateSlug } from '@/lib/utils/slug'
import { formatCurrency } from '@/lib/utils/currency'

// Form validation schema factory
const createProductSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { count: 200 })),
    slug: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { count: 200 }))
      .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, t('validation.identifierFormat')),
    shortDescription: z.string().max(300, t('validation.maxLength', { count: 300 })).optional().nullable(),
    description: z.string().optional().nullable(),
    descriptionHtml: z.string().optional().nullable(),
    basePrice: z.coerce.number().min(0, t('validation.minValue', { value: 0 })),
    // Currency hardcoded to VND for Vietnam market - UI selector intentionally removed
    currency: z.string().default('VND'),
    categoryId: z.string().optional().nullable(),
    brandId: z.string().optional().nullable(),
    sku: z.string().optional().nullable(),
    barcode: z.string().optional().nullable(),
    trackInventory: z.boolean().default(true),
    metaTitle: z.string().optional().nullable(),
    metaDescription: z.string().optional().nullable(),
    sortOrder: z.coerce.number().default(0),
  })

type ProductFormData = z.infer<ReturnType<typeof createProductSchema>>

// Variant form schema factory
const createVariantSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')),
    price: z.coerce.number().min(0, t('validation.minValue', { value: 0 })),
    sku: z.string().optional().nullable(),
    compareAtPrice: z.coerce.number().optional().nullable(),
    stockQuantity: z.coerce.number().min(0, t('validation.minValue', { value: 0 })).default(0),
    sortOrder: z.coerce.number().default(0),
  })

type VariantFormData = z.infer<ReturnType<typeof createVariantSchema>>

// Inline edit form for variants
const EditVariantForm = ({
  variant,
  onSave,
  onCancel,
}: {
  variant: ProductVariant
  onSave: (data: VariantFormData) => void
  onCancel: () => void
}) => {
  const { t } = useTranslation('common')
  const [formData, setFormData] = useState<VariantFormData>({
    name: variant.name,
    price: variant.price,
    sku: variant.sku || '',
    compareAtPrice: variant.compareAtPrice || null,
    stockQuantity: variant.stockQuantity,
    sortOrder: variant.sortOrder,
  })

  return (
    <div className="p-4 border border-primary/30 rounded-lg bg-primary/5 space-y-4">
      <h4 className="font-medium">{t('products.editVariant')}</h4>
      <div className="grid grid-cols-2 gap-4">
        <Input
          placeholder={t('products.variantName')}
          value={formData.name}
          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
        />
        <Input
          type="number"
          placeholder={t('products.variantPrice')}
          value={formData.price}
          onChange={(e) => setFormData({ ...formData, price: parseFloat(e.target.value) || 0 })}
        />
        <Input
          placeholder={t('products.variantSku')}
          value={formData.sku || ''}
          onChange={(e) => setFormData({ ...formData, sku: e.target.value })}
        />
        <Input
          type="number"
          placeholder={t('products.variantStock')}
          value={formData.stockQuantity}
          onChange={(e) => setFormData({ ...formData, stockQuantity: parseInt(e.target.value) || 0 })}
        />
        <Input
          type="number"
          placeholder={t('products.variantCompareAtPrice')}
          value={formData.compareAtPrice || ''}
          onChange={(e) => setFormData({ ...formData, compareAtPrice: e.target.value ? parseFloat(e.target.value) : null })}
        />
        <Input
          type="number"
          placeholder={t('products.variantSortOrder')}
          value={formData.sortOrder}
          onChange={(e) => setFormData({ ...formData, sortOrder: parseInt(e.target.value) || 0 })}
        />
      </div>
      <div className="flex justify-end gap-2">
        <Button variant="ghost" size="sm" className="cursor-pointer" onClick={onCancel}>
          Cancel
        </Button>
        <Button size="sm" className="cursor-pointer" onClick={() => onSave(formData)}>
          Save
        </Button>
      </div>
    </div>
  )
}

export const ProductFormPage = () => {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const isEditing = !!id
  const isViewMode = isEditing && !location.pathname.endsWith('/edit')

  // Permission checks
  const canPublishProducts = hasPermission(Permissions.ProductsPublish)

  usePageContext(isViewMode ? 'View Product' : isEditing ? 'Edit Product' : 'New Product')

  const { data: product, loading: productLoading, refresh: refreshProduct } = useProduct(id)
  const { data: categories } = useProductCategories()
  const { data: brands = [] } = useActiveBrands()

  const [isSaving, setIsSaving] = useState(false)
  const [isPublishing, setIsPublishing] = useState(false)
  const [variants, setVariants] = useState<ProductVariant[]>([])
  const [images, setImages] = useState<ProductImage[]>([])
  const [newVariant, setNewVariant] = useState<VariantFormData | null>(null)
  const [editingVariantId, setEditingVariantId] = useState<string | null>(null)
  const [newImageUrl, setNewImageUrl] = useState('')
  const [variantToDelete, setVariantToDelete] = useState<ProductVariant | null>(null)
  const [imageToDelete, setImageToDelete] = useState<ProductImage | null>(null)
  const [isDeletingVariant, setIsDeletingVariant] = useState(false)
  const [isDeletingImage, setIsDeletingImage] = useState(false)
  const [descriptionHtml, setDescriptionHtml] = useState('')
  const editorRef = useRef<TinyMCEEditor | null>(null)
  const [pendingAttributeValues, setPendingAttributeValues] = useState<Record<string, unknown>>({})

  // Local state for create mode (before product exists)
  const [localVariants, setLocalVariants] = useState<LocalVariant[]>([])
  const [tempImages, setTempImages] = useState<TempImage[]>([])
  const [localVariantToDelete, setLocalVariantToDelete] = useState<LocalVariant | null>(null)
  const [tempImageToDelete, setTempImageToDelete] = useState<TempImage | null>(null)

  // Attribute bulk update hook
  const { bulkUpdate: bulkUpdateAttributes } = useBulkUpdateProductAttributes()

  // Stock history state
  const [selectedVariantForHistory, setSelectedVariantForHistory] = useState<string | null>(null)
  const effectiveVariantId = selectedVariantForHistory ?? variants[0]?.id
  const { data: stockHistory, loading: stockHistoryLoading } = useStockHistory(
    id,
    effectiveVariantId
  )

  // Map backend InventoryMovement to frontend StockMovement type
  const mapToStockMovements = (movements: InventoryMovement[]): StockMovement[] => {
    const typeMapping: Record<InventoryMovementType, StockMovementType> = {
      StockIn: 'restock',
      StockOut: 'sale',
      Adjustment: 'adjustment',
      Return: 'return',
      Reservation: 'reserved',
      ReservationRelease: 'released',
      Damaged: 'adjustment',
      Expired: 'adjustment',
    }

    return movements.map((m) => ({
      id: m.id,
      type: typeMapping[m.movementType] || 'adjustment',
      quantity: m.quantityMoved,
      previousStock: m.quantityBefore,
      newStock: m.quantityAfter,
      reason: m.notes ?? undefined,
      orderId: m.reference ?? undefined,
      createdAt: m.createdAt,
      createdBy: m.userId ?? undefined,
    }))
  }

  // Handler for attribute value changes
  const handleAttributesChange = (values: Record<string, unknown>) => {
    setPendingAttributeValues(values)
  }

  const form = useForm<ProductFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createProductSchema(t)) as unknown as Resolver<ProductFormData>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      slug: '',
      shortDescription: '',
      description: '',
      descriptionHtml: '',
      basePrice: 0,
      currency: 'VND',
      categoryId: null,
      brandId: null,
      sku: '',
      barcode: '',
      trackInventory: true,
      metaTitle: '',
      metaDescription: '',
      sortOrder: 0,
    },
  })

  // Load product data when editing
  useEffect(() => {
    if (product) {
      form.reset({
        name: product.name,
        slug: product.slug,
        shortDescription: product.shortDescription || '',
        description: product.description || '',
        descriptionHtml: product.descriptionHtml || '',
        basePrice: product.basePrice,
        currency: product.currency,
        categoryId: product.categoryId || null,
        brandId: product.brandId || null,
        sku: product.sku || '',
        barcode: product.barcode || '',
        trackInventory: product.trackInventory,
        metaTitle: product.metaTitle || '',
        metaDescription: product.metaDescription || '',
        sortOrder: product.sortOrder,
      })
      setVariants(product.variants || [])
      setImages(product.images || [])
      setDescriptionHtml(product.descriptionHtml || '')
    }
  }, [product, form])

  // Auto-generate slug from name
  const handleNameChange = (name: string) => {
    form.setValue('name', name)
    if (!isEditing || !form.getValues('slug')) {
      form.setValue('slug', generateSlug(name))
    }
  }

  const onSubmit = async (data: ProductFormData) => {
    setIsSaving(true)
    try {
      let productId = id

      if (isEditing && id) {
        await updateProduct(id, {
          ...data,
          descriptionHtml, // Use TinyMCE editor state
          currency: 'VND', // Hardcoded to VND for Vietnam market - UI selector removed
          categoryId: data.categoryId || null,
        })
      } else {
        // Convert local variants to CreateProductVariantRequest format
        const variantsToCreate: CreateProductVariantRequest[] = localVariants.map((v) => ({
          name: v.name,
          price: v.price,
          sku: v.sku,
          compareAtPrice: v.compareAtPrice,
          stockQuantity: v.stockQuantity,
          options: null,
          sortOrder: v.sortOrder,
        }))

        // Convert temp images to CreateProductImageRequest format
        const imagesToCreate: CreateProductImageRequest[] = tempImages.map((img) => ({
          url: img.url,
          altText: img.altText,
          sortOrder: img.sortOrder,
          isPrimary: img.isPrimary,
        }))

        const newProduct = await createProduct({
          ...data,
          descriptionHtml, // Use TinyMCE editor state
          currency: 'VND', // Hardcoded to VND for Vietnam market - UI selector removed
          categoryId: data.categoryId || null,
          variants: variantsToCreate.length > 0 ? variantsToCreate : [],
          images: imagesToCreate.length > 0 ? imagesToCreate : [],
        })
        productId = newProduct.id
      }

      // Save attribute values if any pending changes
      if (productId && Object.keys(pendingAttributeValues).length > 0) {
        const attributeItems = Object.entries(pendingAttributeValues)
          .filter(([, value]) => value !== undefined)
          .map(([attributeId, value]) => ({
            attributeId,
            value,
          }))

        if (attributeItems.length > 0) {
          const result = await bulkUpdateAttributes(productId, {
            variantId: null,
            values: attributeItems,
          })

          if (!result.success) {
            toast.error(result.error || 'Failed to save attributes')
            return
          }
        }
      }

      toast.success(isEditing ? 'Product updated successfully' : 'Product created successfully')

      if (!isEditing && productId) {
        navigate(`/portal/ecommerce/products/${productId}/edit`)
      }
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to save product'
      toast.error(message)
    } finally {
      setIsSaving(false)
    }
  }

  const handlePublish = async () => {
    if (!id) return

    setIsPublishing(true)
    try {
      await publishProduct(id)
      toast.success('Product published successfully')
      await refreshProduct()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to publish product'
      toast.error(message)
    } finally {
      setIsPublishing(false)
    }
  }

  // Variant management
  const handleAddVariant = async () => {
    if (!newVariant || !id) return

    try {
      const request: CreateProductVariantRequest = {
        name: newVariant.name,
        price: newVariant.price,
        sku: newVariant.sku || null,
        compareAtPrice: newVariant.compareAtPrice || null,
        stockQuantity: newVariant.stockQuantity,
        options: null,
        sortOrder: newVariant.sortOrder,
      }
      await addProductVariant(id, request)
      toast.success('Variant added successfully')
      setNewVariant(null)
      await refreshProduct()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to add variant'
      toast.error(message)
    }
  }

  const handleConfirmDeleteVariant = async () => {
    if (!id || !variantToDelete) return

    setIsDeletingVariant(true)
    try {
      await deleteProductVariant(id, variantToDelete.id)
      toast.success(`Variant "${variantToDelete.name}" deleted successfully`)
      setVariantToDelete(null)
      await refreshProduct()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to delete variant'
      toast.error(message)
    } finally {
      setIsDeletingVariant(false)
    }
  }

  // Local variant management (for create mode)
  const handleAddLocalVariant = () => {
    if (!newVariant) return

    const localVariant: LocalVariant = {
      tempId: `temp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      name: newVariant.name,
      price: newVariant.price,
      sku: newVariant.sku || null,
      compareAtPrice: newVariant.compareAtPrice || null,
      stockQuantity: newVariant.stockQuantity,
      sortOrder: newVariant.sortOrder || localVariants.length,
    }

    setLocalVariants((prev) => [...prev, localVariant])
    setNewVariant(null)
    toast.success('Variant added (will be saved with product)')
  }

  const handleUpdateLocalVariant = (tempId: string, data: VariantFormData) => {
    setLocalVariants((prev) =>
      prev.map((v) =>
        v.tempId === tempId
          ? {
              ...v,
              name: data.name,
              price: data.price,
              sku: data.sku || null,
              compareAtPrice: data.compareAtPrice || null,
              stockQuantity: data.stockQuantity,
              sortOrder: data.sortOrder,
            }
          : v
      )
    )
    setEditingVariantId(null)
    toast.success('Variant updated')
  }

  const handleConfirmDeleteLocalVariant = () => {
    if (!localVariantToDelete) return

    setLocalVariants((prev) => prev.filter((v) => v.tempId !== localVariantToDelete.tempId))
    toast.success(`Variant "${localVariantToDelete.name}" removed`)
    setLocalVariantToDelete(null)
  }

  // Temp image management (for create mode)
  const handleUploadTempImage = async (file: File) => {
    try {
      const result = await uploadMedia(file, 'products')
      const tempImage: TempImage = {
        tempId: `temp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
        url: result.defaultUrl || '',
        altText: null,
        sortOrder: tempImages.length,
        isPrimary: tempImages.length === 0,
      }
      setTempImages((prev) => [...prev, tempImage])
      toast.success(t('messages.uploadSuccess', 'Image uploaded successfully'))
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to upload image'
      toast.error(message)
      throw err
    }
  }

  const handleAddTempImageByUrl = () => {
    if (!newImageUrl) return

    const tempImage: TempImage = {
      tempId: `temp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
      url: newImageUrl,
      altText: null,
      sortOrder: tempImages.length,
      isPrimary: tempImages.length === 0,
    }
    setTempImages((prev) => [...prev, tempImage])
    setNewImageUrl('')
    toast.success('Image added (will be saved with product)')
  }

  const handleSetPrimaryTempImage = (tempId: string) => {
    setTempImages((prev) =>
      prev.map((img) => ({
        ...img,
        isPrimary: img.tempId === tempId,
      }))
    )
    toast.success('Primary image set')
  }

  const handleConfirmDeleteTempImage = () => {
    if (!tempImageToDelete) return

    setTempImages((prev) => {
      const filtered = prev.filter((img) => img.tempId !== tempImageToDelete.tempId)
      // If we deleted the primary image, make the first remaining image primary
      if (tempImageToDelete.isPrimary && filtered.length > 0) {
        filtered[0].isPrimary = true
      }
      return filtered
    })
    toast.success('Image removed')
    setTempImageToDelete(null)
  }

  // Image management
  const handleAddImage = async () => {
    if (!newImageUrl || !id) return

    try {
      await addProductImage(id, {
        url: newImageUrl,
        altText: null,
        sortOrder: images.length,
        isPrimary: images.length === 0,
      })
      toast.success('Image added successfully')
      setNewImageUrl('')
      await refreshProduct()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to add image'
      toast.error(message)
    }
  }

  const handleConfirmDeleteImage = async () => {
    if (!id || !imageToDelete) return

    setIsDeletingImage(true)
    try {
      await deleteProductImage(id, imageToDelete.id)
      toast.success('Image deleted successfully')
      setImageToDelete(null)
      await refreshProduct()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to delete image'
      toast.error(message)
    } finally {
      setIsDeletingImage(false)
    }
  }

  const handleSetPrimaryImage = async (imageId: string) => {
    if (!id) return

    try {
      await setPrimaryProductImage(id, imageId)
      toast.success('Primary image set successfully')
      await refreshProduct()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to set primary image'
      toast.error(message)
    }
  }

  // New image upload handler
  const handleUploadImage = async (file: File) => {
    if (!id) return

    try {
      await uploadProductImage(id, file, undefined, images.length === 0)
      toast.success(t('messages.uploadSuccess', 'Image uploaded successfully'))
      await refreshProduct()
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to upload image'
      toast.error(message)
      throw err // Re-throw to let ImageUploadZone show error state
    }
  }

  // Image reorder handler
  const handleReorderImages = async (reorderedImages: ProductImage[]) => {
    if (!id) return

    // Optimistically update local state
    setImages(reorderedImages)

    try {
      await reorderProductImages(
        id,
        reorderedImages.map((img, index) => ({
          imageId: img.id,
          sortOrder: index,
        }))
      )
      toast.success(t('products.imagesReordered', 'Images reordered successfully'))
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to reorder images'
      toast.error(message)
      // Revert on error
      await refreshProduct()
    }
  }

  // Handler for SortableImageGallery's onUpdateAltText
  const handleGalleryUpdateAltText = async (imageId: string, altText: string) => {
    if (!id) return

    const image = images.find(img => img.id === imageId)
    if (!image) return

    try {
      await updateProductImage(id, imageId, {
        url: image.url,
        altText: altText || null,
        sortOrder: image.sortOrder,
      })
      toast.success('Alt text updated successfully')
      await refreshProduct()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to update alt text'
      toast.error(message)
    }
  }

  if (productLoading) {
    return (
      <div className="flex items-center justify-center h-96 animate-in fade-in-0 duration-300">
        <div className="flex flex-col items-center gap-4">
          <div className="p-4 rounded-xl bg-muted/50 border border-border shadow-sm">
            <Package className="h-8 w-8 text-muted-foreground animate-pulse" />
          </div>
          <p className="text-muted-foreground">Loading product...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6 animate-in fade-in-0 slide-in-from-bottom-4 duration-500">
      {/* Page Header with Glassmorphism */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div className="flex items-center gap-4">
          <ViewTransitionLink to="/portal/ecommerce/products">
            <Button
              variant="ghost"
              size="icon"
              className="cursor-pointer hover:bg-muted transition-all duration-300 hover:scale-105"
              aria-label={t('labels.goBackToProducts', 'Go back to products list')}
            >
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </ViewTransitionLink>
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br from-primary/20 to-primary/10 shadow-lg shadow-primary/20 backdrop-blur-sm border border-primary/20 transition-all duration-300 hover:shadow-xl hover:shadow-primary/30 hover:scale-105">
            <Package className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight bg-gradient-to-r from-foreground to-foreground/70 bg-clip-text text-transparent">
              {isViewMode ? 'View Product' : isEditing ? 'Edit Product' : 'New Product'}
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              {isViewMode ? product?.name : isEditing ? `Editing: ${product?.name}` : 'Create a new product'}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {isViewMode ? (
            <ViewTransitionLink to={`/portal/ecommerce/products/${id}/edit`}>
              <Button>
                <Pencil className="h-4 w-4 mr-2" />
                Edit
              </Button>
            </ViewTransitionLink>
          ) : (
            <>
              {isEditing && product?.status === 'Draft' && canPublishProducts && (
                <Button variant="outline" onClick={handlePublish} disabled={isPublishing}>
                  <Send className="h-4 w-4 mr-2" />
                  {isPublishing ? t('labels.publishing', 'Publishing...') : t('labels.publish', 'Publish')}
                </Button>
              )}
              <Button onClick={form.handleSubmit(onSubmit)} disabled={isSaving}>
                <Save className="h-4 w-4 mr-2" />
                {isSaving ? 'Saving...' : 'Save'}
              </Button>
            </>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Form */}
        <div className="lg:col-span-2 space-y-6">
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
              {/* Basic Information */}
              <Card className="shadow-sm hover:shadow-lg transition-shadow duration-300">
                <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
                  <CardTitle>Basic Information</CardTitle>
                  <CardDescription>Product name, description, and identifiers</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Product Name</FormLabel>
                        <FormControl>
                          <Input
                            {...field}
                            onChange={(e) => handleNameChange(e.target.value)}
                            placeholder={t('products.productNamePlaceholder')}
                            disabled={isViewMode}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="slug"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>URL Slug</FormLabel>
                        <FormControl>
                          <Input {...field} placeholder={t('products.urlSlugPlaceholder')} disabled={isViewMode} />
                        </FormControl>
                        <FormDescription>
                          Used in the product URL. Auto-generated from name.
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="shortDescription"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('products.shortDescription')}</FormLabel>
                        <FormControl>
                          <Textarea
                            {...field}
                            value={field.value || ''}
                            placeholder={t('products.shortDescriptionPlaceholder')}
                            rows={2}
                            maxLength={300}
                            disabled={isViewMode}
                          />
                        </FormControl>
                        <FormDescription>
                          {t('products.shortDescriptionHelper', { count: (field.value || '').length })}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div>
                    <FormLabel className="mb-2 block">{t('products.richDescription')}</FormLabel>
                    {isViewMode ? (
                      <div
                        className="prose prose-sm max-w-none border rounded-md p-4 bg-muted/30"
                        dangerouslySetInnerHTML={{ __html: descriptionHtml || `<p class="text-muted-foreground">${t('products.noDescription')}</p>` }}
                      />
                    ) : (
                      <Editor
                        onInit={(_evt, editor) => {
                          editorRef.current = editor
                        }}
                        value={descriptionHtml}
                        onEditorChange={(content) => setDescriptionHtml(content)}
                        init={{
                          height: 400,
                          menubar: false,
                          skin_url: '/tinymce/skins/ui/oxide',
                          content_css: '/tinymce/skins/content/default/content.min.css',
                          plugins: [
                            'advlist',
                            'autolink',
                            'lists',
                            'link',
                            'image',
                            'charmap',
                            'preview',
                            'anchor',
                            'searchreplace',
                            'visualblocks',
                            'code',
                            'fullscreen',
                            'insertdatetime',
                            'media',
                            'table',
                            'wordcount',
                          ],
                          toolbar:
                            'undo redo | blocks | ' +
                            'bold italic forecolor | alignleft aligncenter ' +
                            'alignright alignjustify | bullist numlist outdent indent | ' +
                            'link image | code preview',
                          content_style: `
                            body {
                              font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                              font-size: 14px;
                              line-height: 1.6;
                              color: #333;
                              padding: 12px;
                              max-width: 100%;
                              margin: 0;
                            }
                            p { margin: 0.75em 0; }
                            h1, h2, h3, h4, h5, h6 { margin-top: 1.25em; margin-bottom: 0.5em; font-weight: 600; }
                            img { max-width: 100%; height: auto; }
                            ul, ol { margin: 0.75em 0; padding-left: 1.5em; }
                          `,
                        }}
                      />
                    )}
                    <p className="text-xs text-muted-foreground mt-2">
                      {t('products.richDescriptionHelper')}
                    </p>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="sku"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>SKU</FormLabel>
                          <FormControl>
                            <Input {...field} value={field.value || ''} placeholder={t('products.skuPlaceholder')} disabled={isViewMode} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="barcode"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Barcode</FormLabel>
                          <FormControl>
                            <Input {...field} value={field.value || ''} placeholder="1234567890123" disabled={isViewMode} />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  <FormField
                    control={form.control}
                    name="brandId"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('labels.brand', 'Brand')}</FormLabel>
                        <Select
                          onValueChange={(value) => field.onChange(value === 'none' ? null : value)}
                          value={field.value || 'none'}
                          disabled={isViewMode}
                        >
                          <FormControl>
                            <SelectTrigger className="cursor-pointer" aria-label={t('labels.brand', 'Brand')}>
                              <SelectValue placeholder={t('products.selectBrand', 'Select brand')} />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value="none" className="cursor-pointer">{t('products.noBrand', 'No brand')}</SelectItem>
                            {brands.map((brand) => (
                              <SelectItem key={brand.id} value={brand.id} className="cursor-pointer">
                                {brand.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

              {/* Pricing */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
                  <CardTitle>Pricing</CardTitle>
                  <CardDescription>Set the product base price (VND)</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="basePrice"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Base Price (VND)</FormLabel>
                        <FormControl>
                          <Input
                            {...field}
                            type="number"
                            min="0"
                            step="1000"
                            placeholder="0"
                            disabled={isViewMode}
                          />
                        </FormControl>
                        <FormDescription>
                          Enter price in Vietnamese Dong (VND)
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

              {/* Inventory */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
                  <CardTitle>Inventory</CardTitle>
                  <CardDescription>Manage stock tracking for this product</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="trackInventory"
                    render={({ field }) => (
                      <FormItem className="flex items-center justify-between rounded-lg border p-4">
                        <div className="space-y-0.5">
                          <FormLabel className="text-base">Track Inventory</FormLabel>
                          <FormDescription>
                            Enable stock tracking for this product
                          </FormDescription>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="cursor-pointer"
                            disabled={isViewMode}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

            </form>
          </Form>

          {/* Variants Section - works in both create and edit modes */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
              <div className="flex items-center justify-between">
                <div>
                  <CardTitle>Variants</CardTitle>
                  <CardDescription>Product variations like size, color</CardDescription>
                </div>
                {!isViewMode && (
                  <Button
                    variant="outline"
                    size="sm"
                    className="cursor-pointer"
                    onClick={() => setNewVariant({ name: '', price: 0, sku: '', compareAtPrice: null, stockQuantity: 0, sortOrder: 0 })}
                  >
                    <Plus className="h-4 w-4 mr-2" />
                    Add Variant
                  </Button>
                )}
              </div>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {/* New Variant Form */}
                {newVariant && (
                  <div className="p-4 border rounded-lg bg-muted/50 space-y-4">
                    <h4 className="font-medium">New Variant</h4>
                    <div className="grid grid-cols-2 gap-4">
                      <Input
                        placeholder="Variant name"
                        value={newVariant.name}
                        onChange={(e) => setNewVariant({ ...newVariant, name: e.target.value })}
                      />
                      <Input
                        type="number"
                        placeholder="Price"
                        value={newVariant.price}
                        onChange={(e) => setNewVariant({ ...newVariant, price: parseFloat(e.target.value) || 0 })}
                      />
                      <Input
                        placeholder="SKU"
                        value={newVariant.sku || ''}
                        onChange={(e) => setNewVariant({ ...newVariant, sku: e.target.value })}
                      />
                      <Input
                        type="number"
                        placeholder="Stock"
                        value={newVariant.stockQuantity}
                        onChange={(e) => setNewVariant({ ...newVariant, stockQuantity: parseInt(e.target.value) || 0 })}
                      />
                    </div>
                    <div className="flex justify-end gap-2">
                      <Button variant="ghost" size="sm" className="cursor-pointer" onClick={() => setNewVariant(null)}>
                        Cancel
                      </Button>
                      <Button size="sm" className="cursor-pointer" onClick={isEditing ? handleAddVariant : handleAddLocalVariant}>
                        Add
                      </Button>
                    </div>
                  </div>
                )}

                {/* Display variants - local in create mode, from API in edit mode */}
                {isEditing ? (
                  // Edit mode: show editable variants table
                  <EditableVariantsTable
                    productId={id!}
                    variants={variants}
                    isReadOnly={isViewMode}
                    onDelete={setVariantToDelete}
                    onSaveSuccess={async (updatedVariant) => {
                      // Update local variants state for immediate UI feedback
                      setVariants((prev) =>
                        prev.map((v) => (v.id === updatedVariant.id ? updatedVariant : v))
                      )
                      // Refresh product data to ensure full sync (computed fields like lowStock, inStock)
                      await refreshProduct()
                    }}
                  />
                ) : (
                  // Create mode: show local variants
                  localVariants.length === 0 && !newVariant ? (
                    <p className="text-center text-muted-foreground py-8">
                      No variants yet. Add variants for different sizes, colors, etc.
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {localVariants.map((variant) => (
                        editingVariantId === variant.tempId ? (
                          <EditVariantForm
                            key={variant.tempId}
                            variant={{
                              id: variant.tempId,
                              name: variant.name,
                              price: variant.price,
                              sku: variant.sku,
                              compareAtPrice: variant.compareAtPrice,
                              stockQuantity: variant.stockQuantity,
                              sortOrder: variant.sortOrder,
                              inStock: true,
                              lowStock: false,
                              onSale: false,
                            }}
                            onSave={(data) => handleUpdateLocalVariant(variant.tempId, data)}
                            onCancel={() => setEditingVariantId(null)}
                          />
                        ) : (
                          <div
                            key={variant.tempId}
                            className="flex items-center gap-4 p-4 border rounded-xl bg-background hover:bg-muted/50 hover:shadow-sm transition-all duration-200 group"
                          >
                            <div className="flex-1">
                              <div className="font-medium">{variant.name}</div>
                              <div className="text-sm text-muted-foreground">
                                {variant.sku && `SKU: ${variant.sku} • `}
                                Stock: {variant.stockQuantity}
                              </div>
                            </div>
                            <div className="text-right">
                              <div className="font-medium">
                                {formatCurrency(variant.price)}
                              </div>
                            </div>
                            <Badge variant="outline" className="text-xs">Pending</Badge>
                            <div className="flex items-center gap-1">
                              <Button
                                variant="ghost"
                                size="icon"
                                className="cursor-pointer"
                                onClick={() => setEditingVariantId(variant.tempId)}
                                aria-label={`Edit variant ${variant.name}`}
                              >
                                <Pencil className="h-4 w-4 text-muted-foreground" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="cursor-pointer"
                                onClick={() => setLocalVariantToDelete(variant)}
                                aria-label={`Delete variant ${variant.name}`}
                              >
                                <Trash2 className="h-4 w-4 text-destructive" />
                              </Button>
                            </div>
                          </div>
                        )
                      ))}
                    </div>
                  )
                )}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Status */}
          {isEditing && product && (
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
                <CardTitle>Status</CardTitle>
              </CardHeader>
              <CardContent>
                <Badge
                  variant={product.status === 'Active' ? 'default' : 'secondary'}
                  className="text-sm"
                >
                  {product.status}
                </Badge>
              </CardContent>
            </Card>
          )}

          {/* Category */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
              <CardTitle>Organization</CardTitle>
            </CardHeader>
            <CardContent>
              <Form {...form}>
                <FormField
                  control={form.control}
                  name="categoryId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Category</FormLabel>
                      <Select
                        onValueChange={(value) => field.onChange(value === 'none' ? null : value)}
                        value={field.value || 'none'}
                        disabled={isViewMode}
                      >
                        <FormControl>
                          <SelectTrigger className="cursor-pointer" aria-label={t('products.selectCategory', 'Select product category')}>
                            <SelectValue placeholder={t('categories.selectParent')} />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          <SelectItem value="none" className="cursor-pointer">No category</SelectItem>
                          {categories.map((cat) => (
                            <SelectItem key={cat.id} value={cat.id} className="cursor-pointer">
                              {cat.parentName ? `${cat.parentName} > ${cat.name}` : cat.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </Form>
            </CardContent>
          </Card>

          {/* Product Attributes - works in both create and edit modes */}
          {isEditing && id ? (
            <ProductAttributesSection
              productId={id}
              categoryId={form.watch('categoryId') || null}
              isViewMode={isViewMode}
              onAttributesChange={handleAttributesChange}
            />
          ) : (
            <ProductAttributesSectionCreate
              categoryId={form.watch('categoryId') || null}
              isViewMode={isViewMode}
              onAttributesChange={handleAttributesChange}
            />
          )}

          {/* SEO */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
              <CardTitle>SEO</CardTitle>
              <CardDescription>Search engine optimization</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <Form {...form}>
                <FormField
                  control={form.control}
                  name="metaTitle"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Meta Title</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          value={field.value || ''}
                          placeholder="SEO title"
                          maxLength={60}
                          disabled={isViewMode}
                        />
                      </FormControl>
                      <FormDescription>
                        {(field.value || '').length}/60 characters
                      </FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="metaDescription"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Meta Description</FormLabel>
                      <FormControl>
                        <Textarea
                          {...field}
                          value={field.value || ''}
                          placeholder="SEO description"
                          maxLength={160}
                          rows={3}
                          disabled={isViewMode}
                        />
                      </FormControl>
                      <FormDescription>
                        {(field.value || '').length}/160 characters
                      </FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </Form>
            </CardContent>
          </Card>

          {/* Images - works in both create and edit modes */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
              <CardTitle>Images</CardTitle>
              <CardDescription>Product gallery images</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {isEditing ? (
                // Edit mode: full sortable gallery with API
                <>
                  {/* Drag & Drop Upload Zone */}
                  {!isViewMode && (
                    <ImageUploadZone
                      onUpload={handleUploadImage}
                      disabled={false}
                      maxSizeMB={10}
                    />
                  )}

                  {/* Sortable Image Gallery */}
                  <SortableImageGallery
                    images={images}
                    productName={form.getValues('name')}
                    isViewMode={isViewMode}
                    onReorder={handleReorderImages}
                    onSetPrimary={handleSetPrimaryImage}
                    onDelete={(image) => setImageToDelete(image)}
                    onUpdateAltText={handleGalleryUpdateAltText}
                  />

                  {/* Fallback: Add Image by URL */}
                  {!isViewMode && (
                    <div className="space-y-2 pt-4 border-t">
                      <p className="text-xs font-medium text-muted-foreground">Or add by URL</p>
                      <div className="flex gap-2">
                        <Input
                          placeholder="Image URL"
                          value={newImageUrl}
                          onChange={(e) => setNewImageUrl(e.target.value)}
                          aria-label={t('products.imageUrlAriaLabel', 'Image URL')}
                        />
                        <Button
                          variant="outline"
                          size="icon"
                          className="cursor-pointer"
                          onClick={handleAddImage}
                          disabled={!newImageUrl}
                          aria-label={t('products.addImageAriaLabel', 'Add image')}
                        >
                          <ImagePlus className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  )}
                </>
              ) : (
                // Create mode: upload to temp storage
                <>
                  {/* Drag & Drop Upload Zone for temp images */}
                  <ImageUploadZone
                    onUpload={handleUploadTempImage}
                    disabled={false}
                    maxSizeMB={10}
                  />

                  {/* Display temp images */}
                  {tempImages.length > 0 && (
                    <div className="grid grid-cols-2 gap-3">
                      {tempImages.map((img) => (
                        <div
                          key={img.tempId}
                          className={`relative group rounded-lg overflow-hidden border-2 ${
                            img.isPrimary ? 'border-primary' : 'border-border'
                          }`}
                        >
                          <img
                            src={img.url}
                            alt={img.altText || 'Product image'}
                            className="w-full aspect-square object-cover"
                          />
                          {img.isPrimary && (
                            <div className="absolute top-2 left-2">
                              <Badge className="bg-primary text-primary-foreground text-xs">
                                <Star className="h-3 w-3 mr-1" />
                                Primary
                              </Badge>
                            </div>
                          )}
                          <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2">
                            {!img.isPrimary && (
                              <Button
                                variant="secondary"
                                size="sm"
                                className="cursor-pointer"
                                onClick={() => handleSetPrimaryTempImage(img.tempId)}
                              >
                                <Star className="h-4 w-4" />
                              </Button>
                            )}
                            <Button
                              variant="destructive"
                              size="sm"
                              className="cursor-pointer"
                              onClick={() => setTempImageToDelete(img)}
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Add Image by URL */}
                  <div className="space-y-2 pt-4 border-t">
                    <p className="text-xs font-medium text-muted-foreground">Or add by URL</p>
                    <div className="flex gap-2">
                      <Input
                        placeholder="Image URL"
                        value={newImageUrl}
                        onChange={(e) => setNewImageUrl(e.target.value)}
                        aria-label="Image URL"
                      />
                      <Button
                        variant="outline"
                        size="icon"
                        className="cursor-pointer"
                        onClick={handleAddTempImageByUrl}
                        disabled={!newImageUrl}
                        aria-label="Add image"
                      >
                        <ImagePlus className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {/* Activity Log (only show when editing) */}
          {isEditing && id && (
            <ProductActivityLog
              productId={id}
              productName={form.getValues('name')}
            />
          )}

          {/* Stock History (only show when editing and variants exist) */}
          {isEditing && id && variants.length > 0 && (
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader>
                <CardTitle>{t('products.stock.history', 'Stock History')}</CardTitle>
                <CardDescription>
                  {t('products.stock.historyDescription', 'Movement history for inventory')}
                </CardDescription>
              </CardHeader>
              <CardContent>
                {/* Variant selector if multiple variants */}
                {variants.length > 1 && (
                  <Select
                    value={selectedVariantForHistory ?? variants[0]?.id}
                    onValueChange={setSelectedVariantForHistory}
                  >
                    <SelectTrigger className="mb-4 cursor-pointer" aria-label={t('products.selectVariant', 'Select variant')}>
                      <SelectValue placeholder={t('products.selectVariant', 'Select variant')} />
                    </SelectTrigger>
                    <SelectContent>
                      {variants.map((v) => (
                        <SelectItem key={v.id} value={v.id} className="cursor-pointer">
                          {v.name} ({v.stockQuantity} {t('products.inStock', 'in stock')})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}

                {stockHistoryLoading ? (
                  <div className="flex items-center justify-center py-8">
                    <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
                  </div>
                ) : (
                  <StockHistoryTimeline
                    movements={mapToStockMovements(stockHistory?.items ?? [])}
                    currentStock={
                      variants.find((v) => v.id === effectiveVariantId)?.stockQuantity ?? 0
                    }
                    variantName={
                      variants.length > 1
                        ? variants.find((v) => v.id === effectiveVariantId)?.name
                        : undefined
                    }
                    maxHeight="400px"
                  />
                )}
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Delete Variant Confirmation Dialog */}
      <AlertDialog open={!!variantToDelete} onOpenChange={(open) => !open && setVariantToDelete(null)}>
        <AlertDialogContent className="border-destructive/30">
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <AlertTriangle className="h-5 w-5 text-destructive" />
              </div>
              <AlertDialogTitle>Delete Variant</AlertDialogTitle>
            </div>
            <AlertDialogDescription className="pt-2">
              Are you sure you want to delete the variant "{variantToDelete?.name}"? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isDeletingVariant} className="cursor-pointer">
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmDeleteVariant}
              disabled={isDeletingVariant}
              className="bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors cursor-pointer"
            >
              {isDeletingVariant && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isDeletingVariant ? 'Deleting...' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Delete Image Confirmation Dialog */}
      <AlertDialog open={!!imageToDelete} onOpenChange={(open) => !open && setImageToDelete(null)}>
        <AlertDialogContent className="border-destructive/30">
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <AlertTriangle className="h-5 w-5 text-destructive" />
              </div>
              <AlertDialogTitle>Delete Image</AlertDialogTitle>
            </div>
            <AlertDialogDescription className="pt-2">
              Are you sure you want to delete this image? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isDeletingImage} className="cursor-pointer">
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmDeleteImage}
              disabled={isDeletingImage}
              className="bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors cursor-pointer"
            >
              {isDeletingImage && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isDeletingImage ? 'Deleting...' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Delete Local Variant Confirmation Dialog (create mode) */}
      <AlertDialog open={!!localVariantToDelete} onOpenChange={(open) => !open && setLocalVariantToDelete(null)}>
        <AlertDialogContent className="border-destructive/30">
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <AlertTriangle className="h-5 w-5 text-destructive" />
              </div>
              <AlertDialogTitle>Remove Variant</AlertDialogTitle>
            </div>
            <AlertDialogDescription className="pt-2">
              Are you sure you want to remove the variant "{localVariantToDelete?.name}"?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmDeleteLocalVariant}
              className="bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors cursor-pointer"
            >
              Remove
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Delete Temp Image Confirmation Dialog (create mode) */}
      <AlertDialog open={!!tempImageToDelete} onOpenChange={(open) => !open && setTempImageToDelete(null)}>
        <AlertDialogContent className="border-destructive/30">
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <AlertTriangle className="h-5 w-5 text-destructive" />
              </div>
              <AlertDialogTitle>Remove Image</AlertDialogTitle>
            </div>
            <AlertDialogDescription className="pt-2">
              Are you sure you want to remove this image?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleConfirmDeleteTempImage}
              className="bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors cursor-pointer"
            >
              Remove
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}

export default ProductFormPage
