import { useState, useEffect } from 'react'
import { useParams, useNavigate, useLocation, Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
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
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { Separator } from '@/components/ui/separator'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { useProduct, useProductCategories } from '@/hooks/useProducts'
import {
  createProduct,
  updateProduct,
  publishProduct,
  addProductVariant,
  updateProductVariant,
  deleteProductVariant,
  addProductImage,
  deleteProductImage,
  setPrimaryProductImage,
} from '@/services/products'
import { toast } from 'sonner'
import { ApiError } from '@/services/apiClient'
import type { ProductVariant, ProductImage, CreateProductVariantRequest } from '@/types/product'
import { generateSlug } from '@/lib/utils/slug'
import { formatCurrency } from '@/lib/utils/currency'

// Form validation schema
const productSchema = z.object({
  name: z.string().min(1, 'Name is required').max(200, 'Name must be less than 200 characters'),
  slug: z.string().min(1, 'Slug is required').max(200, 'Slug must be less than 200 characters')
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Slug must be lowercase letters, numbers, and hyphens only'),
  description: z.string().optional().nullable(),
  descriptionHtml: z.string().optional().nullable(),
  basePrice: z.coerce.number().min(0, 'Price must be non-negative'),
  // Currency hardcoded to VND for Vietnam market - UI selector intentionally removed
  currency: z.string().default('VND'),
  categoryId: z.string().optional().nullable(),
  brand: z.string().optional().nullable(),
  sku: z.string().optional().nullable(),
  barcode: z.string().optional().nullable(),
  weight: z.coerce.number().optional().nullable(),
  trackInventory: z.boolean().default(true),
  metaTitle: z.string().optional().nullable(),
  metaDescription: z.string().optional().nullable(),
  sortOrder: z.coerce.number().default(0),
})

type ProductFormData = z.infer<typeof productSchema>

// Variant form schema
const variantSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  price: z.coerce.number().min(0, 'Price must be non-negative'),
  sku: z.string().optional().nullable(),
  compareAtPrice: z.coerce.number().optional().nullable(),
  stockQuantity: z.coerce.number().min(0, 'Stock must be non-negative').default(0),
  sortOrder: z.coerce.number().default(0),
})

type VariantFormData = z.infer<typeof variantSchema>

export default function ProductFormPage() {
  const { t } = useTranslation('common')
  const { hasPermission } = usePermissions()
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const isEditing = !!id
  const isViewMode = isEditing && !location.pathname.endsWith('/edit')

  // Permission checks
  const canUpdateProducts = hasPermission(Permissions.ProductsUpdate)
  const canPublishProducts = hasPermission(Permissions.ProductsPublish)

  usePageContext(isViewMode ? 'View Product' : isEditing ? 'Edit Product' : 'New Product')

  const { data: product, loading: productLoading, refresh: refreshProduct } = useProduct(id)
  const { data: categories } = useProductCategories()

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

  const form = useForm<ProductFormData>({
    resolver: zodResolver(productSchema),
    mode: 'onBlur',
    defaultValues: {
      name: '',
      slug: '',
      description: '',
      descriptionHtml: '',
      basePrice: 0,
      currency: 'VND',
      categoryId: null,
      brand: '',
      sku: '',
      barcode: '',
      weight: null,
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
        description: product.description || '',
        descriptionHtml: product.descriptionHtml || '',
        basePrice: product.basePrice,
        currency: product.currency,
        categoryId: product.categoryId || null,
        brand: product.brand || '',
        sku: product.sku || '',
        barcode: product.barcode || '',
        weight: product.weight || null,
        trackInventory: product.trackInventory,
        metaTitle: product.metaTitle || '',
        metaDescription: product.metaDescription || '',
        sortOrder: product.sortOrder,
      })
      setVariants(product.variants || [])
      setImages(product.images || [])
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
      if (isEditing && id) {
        await updateProduct(id, {
          ...data,
          currency: 'VND', // Hardcoded to VND for Vietnam market - UI selector removed
          categoryId: data.categoryId || null,
        })
        toast.success('Product updated successfully')
      } else {
        const newProduct = await createProduct({
          ...data,
          currency: 'VND', // Hardcoded to VND for Vietnam market - UI selector removed
          categoryId: data.categoryId || null,
          variants: [],
          images: [],
        })
        toast.success('Product created successfully')
        navigate(`/portal/ecommerce/products/${newProduct.id}/edit`)
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

  const handleUpdateVariant = async (variantId: string, data: VariantFormData) => {
    if (!id) return

    try {
      await updateProductVariant(id, variantId, {
        name: data.name,
        price: data.price,
        sku: data.sku || null,
        compareAtPrice: data.compareAtPrice || null,
        stockQuantity: data.stockQuantity,
        options: null,
        sortOrder: data.sortOrder,
      })
      toast.success('Variant updated successfully')
      setEditingVariantId(null)
      await refreshProduct()
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to update variant'
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
          <Link to="/portal/ecommerce/products">
            <Button
              variant="ghost"
              size="icon"
              className="cursor-pointer hover:bg-muted transition-all duration-300 hover:scale-105"
              aria-label="Go back to products list"
            >
              <ArrowLeft className="h-5 w-5" />
            </Button>
          </Link>
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
            <Link to={`/portal/ecommerce/products/${id}/edit`}>
              <Button>
                <Pencil className="h-4 w-4 mr-2" />
                Edit
              </Button>
            </Link>
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
                            placeholder="Enter product name"
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
                          <Input {...field} placeholder="product-url-slug" disabled={isViewMode} />
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
                    name="description"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Description</FormLabel>
                        <FormControl>
                          <Textarea
                            {...field}
                            value={field.value || ''}
                            placeholder="Enter product description"
                            rows={4}
                            disabled={isViewMode}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div className="grid grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="sku"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>SKU</FormLabel>
                          <FormControl>
                            <Input {...field} value={field.value || ''} placeholder="SKU-001" disabled={isViewMode} />
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
                    name="brand"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Brand</FormLabel>
                        <FormControl>
                          <Input {...field} value={field.value || ''} placeholder="Brand name" disabled={isViewMode} />
                        </FormControl>
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
                  <CardDescription>Manage stock tracking and weight</CardDescription>
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

                  <FormField
                    control={form.control}
                    name="weight"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Weight (kg)</FormLabel>
                        <FormControl>
                          <Input
                            {...field}
                            type="number"
                            min="0"
                            step="0.1"
                            value={field.value || ''}
                            placeholder="0.0"
                            disabled={isViewMode}
                          />
                        </FormControl>
                        <FormDescription>
                          Used for shipping calculations
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

            </form>
          </Form>

          {/* Variants Section (only show when editing) */}
          {isEditing && (
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
                        <Button size="sm" className="cursor-pointer" onClick={handleAddVariant}>
                          Add
                        </Button>
                      </div>
                    </div>
                  )}

                  {/* Existing Variants */}
                  {variants.length === 0 && !newVariant ? (
                    <p className="text-center text-muted-foreground py-8">
                      No variants yet. Add variants for different sizes, colors, etc.
                    </p>
                  ) : (
                    <div className="space-y-2">
                      {variants.map((variant) => (
                        <div
                          key={variant.id}
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
                            {variant.onSale && variant.compareAtPrice && (
                              <div className="text-sm text-muted-foreground line-through">
                                {formatCurrency(variant.compareAtPrice)}
                              </div>
                            )}
                          </div>
                          <div className="flex items-center gap-1">
                            {variant.lowStock && (
                              <Badge variant="destructive" className="text-xs">Low Stock</Badge>
                            )}
                            {!variant.inStock && (
                              <Badge variant="secondary" className="text-xs">Out of Stock</Badge>
                            )}
                          </div>
                          {!isViewMode && (
                            <Button
                              variant="ghost"
                              size="icon"
                              className="cursor-pointer"
                              onClick={() => setVariantToDelete(variant)}
                              aria-label={`Delete variant ${variant.name}`}
                            >
                              <Trash2 className="h-4 w-4 text-destructive" />
                            </Button>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          )}
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
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue placeholder="Select category" />
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

          {/* Images (only show when editing) */}
          {isEditing && (
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
                <CardTitle>Images</CardTitle>
                <CardDescription>Product gallery images</CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Image Grid - 21st.dev pattern */}
                {images.length > 0 && (
                  <div className="grid grid-cols-2 gap-3">
                    {images.map((image, index) => (
                      <div
                        key={image.id}
                        className="relative aspect-square rounded-xl border overflow-hidden group shadow-sm hover:shadow-md transition-all duration-300"
                      >
                        <img
                          src={image.url}
                          alt={image.altText || `${form.getValues('name') || t('products.product', 'Product')} - ${t('products.imageNumber', { number: index + 1, defaultValue: `Image ${index + 1}` })}`}
                          className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
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
                                onClick={() => handleSetPrimaryImage(image.id)}
                                aria-label="Set as primary image"
                              >
                                <Star className="h-4 w-4" />
                              </Button>
                            )}
                            <Button
                              size="icon"
                              variant="destructive"
                              className="h-8 w-8 shadow-lg backdrop-blur-sm hover:scale-110 transition-all duration-200 cursor-pointer"
                              onClick={() => setImageToDelete(image)}
                              aria-label="Delete image"
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}

                {/* Add Image */}
                {!isViewMode && (
                  <div className="space-y-2">
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
                        onClick={handleAddImage}
                        disabled={!newImageUrl}
                        aria-label="Add image"
                      >
                        <ImagePlus className="h-4 w-4" />
                      </Button>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Enter image URL to add to gallery
                    </p>
                  </div>
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
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90 cursor-pointer"
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
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90 cursor-pointer"
            >
              {isDeletingImage && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isDeletingImage ? 'Deleting...' : 'Delete'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  )
}
