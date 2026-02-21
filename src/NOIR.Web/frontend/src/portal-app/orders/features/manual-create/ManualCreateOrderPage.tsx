import { useState, useMemo, useDeferredValue, useCallback, useRef, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { toast } from 'sonner'
import { useQuery } from '@tanstack/react-query'
import {
  ArrowLeft,
  Loader2,
  Package,
  Plus,
  Search,
  ShoppingCart,
  Trash2,
  AlertTriangle,
  ChevronDown,
  ChevronUp,
  FileText,
  CreditCard,
  User,
  X,
} from 'lucide-react'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Checkbox,
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Label,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Separator,
  Textarea,
  FilePreviewTrigger,
} from '@uikit'
import {
  useManualCreateOrderMutation,
  useSearchProductVariantsQuery,
} from '@/portal-app/orders/queries'
import type { ManualCreateOrderRequest, ProductVariantLookupDto } from '@/services/orders'
import { getCustomers } from '@/services/customers'
import type { CustomerSummaryDto } from '@/types/customer'
import { formatCurrency } from '@/lib/utils/currency'

// --- Types ---

interface OrderItemRow {
  variantId: string
  productName: string
  variantName: string
  sku?: string
  imageUrl?: string
  quantity: number
  originalPrice: number
  customPrice: number | null
  discount: number
  stockQuantity: number
}

// --- Validation Schema ---

const createManualOrderSchema = (t: (key: string, opts?: Record<string, unknown>) => string) =>
  z.object({
    customerEmail: z.string().min(1, t('validation.required')).email(t('validation.invalidEmail')),
    customerName: z.string().optional(),
    customerPhone: z.string().optional(),
    shippingAmount: z.coerce.number().min(0, t('validation.positive')).default(0),
    discountAmount: z.coerce.number().min(0, t('validation.positive')).default(0),
    taxAmount: z.coerce.number().min(0, t('validation.positive')).default(0),
    currency: z.string().max(3).default('VND'),
    shippingMethod: z.string().optional(),
    couponCode: z.string().optional(),
    customerNotes: z.string().optional(),
    internalNotes: z.string().optional(),
    paymentMethod: z.string().optional(),
    initialPaymentStatus: z.string().optional(),
  })

type ManualOrderFormData = z.infer<ReturnType<typeof createManualOrderSchema>>

// --- Product Search Typeahead ---

interface ProductSearchTypeaheadProps {
  onSelect: (variant: ProductVariantLookupDto) => void
  existingVariantIds: Set<string>
  currency: string
}

const ProductSearchTypeahead = ({ onSelect, existingVariantIds, currency }: ProductSearchTypeaheadProps) => {
  const { t } = useTranslation('common')
  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const { data: searchResults } = useSearchProductVariantsQuery(deferredSearch)
  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleSelect = (variant: ProductVariantLookupDto) => {
    onSelect(variant)
    setSearchInput('')
    setIsOpen(false)
  }

  const items = searchResults?.items ?? []

  return (
    <div ref={containerRef} className="relative">
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder={t('orders.manualCreate.searchProducts')}
          value={searchInput}
          onChange={(e) => {
            setSearchInput(e.target.value)
            setIsOpen(true)
          }}
          onFocus={() => {
            if (searchInput.length >= 2) setIsOpen(true)
          }}
          className="pl-9"
          aria-label={t('orders.manualCreate.searchProducts')}
        />
        {isSearchStale && searchInput.length >= 2 && (
          <Loader2 className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-muted-foreground" />
        )}
      </div>

      {isOpen && searchInput.length >= 2 && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-popover shadow-lg max-h-80 overflow-y-auto">
          {items.length === 0 && !isSearchStale ? (
            <div className="p-4 text-center text-sm text-muted-foreground">
              {t('orders.manualCreate.noProducts')}
            </div>
          ) : (
            items.map((variant) => {
              const alreadyAdded = existingVariantIds.has(variant.id)
              const isOutOfStock = variant.stockQuantity <= 0
              const isLowStock = variant.stockQuantity > 0 && variant.stockQuantity <= 5

              return (
                <button
                  key={variant.id}
                  type="button"
                  disabled={alreadyAdded}
                  className="w-full flex items-center gap-3 p-3 text-left hover:bg-accent transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed border-b last:border-b-0"
                  onClick={() => handleSelect(variant)}
                >
                  {variant.imageUrl ? (
                    <img
                      src={variant.imageUrl}
                      alt={variant.productName}
                      className="h-10 w-10 rounded-md object-cover flex-shrink-0"
                    />
                  ) : (
                    <div className="h-10 w-10 rounded-md bg-muted flex items-center justify-center flex-shrink-0">
                      <Package className="h-5 w-5 text-muted-foreground" />
                    </div>
                  )}
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate">{variant.productName}</p>
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      {variant.variantName && <span>{variant.variantName}</span>}
                      {variant.sku && (
                        <code className="bg-muted px-1 py-0.5 rounded">{variant.sku}</code>
                      )}
                    </div>
                  </div>
                  <div className="flex flex-col items-end gap-1 flex-shrink-0">
                    <span className="text-sm font-medium">
                      {formatCurrency(variant.price, currency)}
                    </span>
                    <div className="flex items-center gap-1">
                      {isOutOfStock ? (
                        <Badge variant="destructive" className="text-[10px] px-1.5 py-0">
                          {t('orders.manualCreate.outOfStock')}
                        </Badge>
                      ) : isLowStock ? (
                        <Badge variant="outline" className="text-[10px] px-1.5 py-0 border-amber-300 text-amber-600">
                          <AlertTriangle className="h-3 w-3 mr-0.5" />
                          {t('orders.manualCreate.lowStock')}
                        </Badge>
                      ) : (
                        <span className="text-[10px] text-muted-foreground">
                          {t('orders.manualCreate.stock')}: {variant.stockQuantity}
                        </span>
                      )}
                    </div>
                  </div>
                  {alreadyAdded && (
                    <Badge variant="secondary" className="text-[10px] ml-1">
                      {t('orders.manualCreate.addItem')}
                    </Badge>
                  )}
                </button>
              )
            })
          )}
        </div>
      )}
    </div>
  )
}

// --- Customer Search Typeahead ---

interface CustomerSearchTypeaheadProps {
  onSelect: (customer: CustomerSummaryDto) => void
  onClear: () => void
  selectedCustomer: CustomerSummaryDto | null
}

const CustomerSearchTypeahead = ({ onSelect, onClear, selectedCustomer }: CustomerSearchTypeaheadProps) => {
  const { t } = useTranslation('common')
  const [searchInput, setSearchInput] = useState('')
  const deferredSearch = useDeferredValue(searchInput)
  const isSearchStale = searchInput !== deferredSearch
  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  const { data: searchResults } = useQuery({
    queryKey: ['customers', 'search', deferredSearch],
    queryFn: () => getCustomers({ search: deferredSearch, pageSize: 8 }),
    enabled: deferredSearch.length >= 2,
  })

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleSelect = (customer: CustomerSummaryDto) => {
    onSelect(customer)
    setSearchInput('')
    setIsOpen(false)
  }

  const items = searchResults?.items ?? []

  // Show selected customer card
  if (selectedCustomer) {
    return (
      <div className="flex items-center gap-3 rounded-lg border bg-muted/30 p-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary/10 flex-shrink-0">
          <User className="h-5 w-5 text-primary" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium truncate">
            {selectedCustomer.firstName} {selectedCustomer.lastName}
          </p>
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <span>{selectedCustomer.email}</span>
            {selectedCustomer.phone && (
              <>
                <span>·</span>
                <span>{selectedCustomer.phone}</span>
              </>
            )}
          </div>
        </div>
        <div className="flex items-center gap-2 flex-shrink-0">
          <Badge variant="outline" className="text-[10px] px-1.5 py-0">
            {selectedCustomer.tier}
          </Badge>
          <span className="text-xs text-muted-foreground">
            {selectedCustomer.totalOrders} {t('orders.manualCreate.orders', 'orders')}
          </span>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="h-7 w-7 cursor-pointer text-muted-foreground hover:text-destructive"
            onClick={onClear}
            aria-label={t('orders.manualCreate.clearCustomer')}
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
      </div>
    )
  }

  // Show search input
  return (
    <div ref={containerRef} className="relative">
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder={t('orders.manualCreate.searchCustomer')}
          value={searchInput}
          onChange={(e) => {
            setSearchInput(e.target.value)
            setIsOpen(true)
          }}
          onFocus={() => {
            if (searchInput.length >= 2) setIsOpen(true)
          }}
          className="pl-9"
          aria-label={t('orders.manualCreate.searchCustomer')}
        />
        {isSearchStale && searchInput.length >= 2 && (
          <Loader2 className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-muted-foreground" />
        )}
      </div>

      {isOpen && searchInput.length >= 2 && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-popover shadow-lg max-h-80 overflow-y-auto">
          {items.length === 0 && !isSearchStale ? (
            <div className="p-4 text-center text-sm text-muted-foreground">
              {t('orders.manualCreate.noCustomersFound')}
            </div>
          ) : (
            items.map((customer) => (
              <button
                key={customer.id}
                type="button"
                className="w-full flex items-center gap-3 p-3 text-left hover:bg-accent transition-colors cursor-pointer border-b last:border-b-0"
                onClick={() => handleSelect(customer)}
              >
                <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10 flex-shrink-0">
                  <User className="h-4 w-4 text-primary" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">
                    {customer.firstName} {customer.lastName}
                  </p>
                  <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    <span className="truncate">{customer.email}</span>
                    {customer.phone && (
                      <>
                        <span>·</span>
                        <span>{customer.phone}</span>
                      </>
                    )}
                  </div>
                </div>
                <div className="flex flex-col items-end gap-1 flex-shrink-0">
                  <Badge variant="outline" className="text-[10px] px-1.5 py-0">
                    {customer.tier}
                  </Badge>
                  <span className="text-[10px] text-muted-foreground">
                    {customer.totalOrders} {t('orders.manualCreate.orders', 'orders')}
                  </span>
                </div>
              </button>
            ))
          )}
        </div>
      )}
    </div>
  )
}

// --- Address Section ---

interface AddressSectionProps {
  title: string
  expanded: boolean
  onToggle: () => void
  values: AddressValues
  onChange: (values: AddressValues) => void
}

interface AddressValues {
  fullName: string
  phone: string
  addressLine1: string
  addressLine2: string
  ward: string
  district: string
  province: string
  country: string
  postalCode: string
}

const emptyAddress: AddressValues = {
  fullName: '',
  phone: '',
  addressLine1: '',
  addressLine2: '',
  ward: '',
  district: '',
  province: '',
  country: 'Vietnam',
  postalCode: '',
}

const AddressSection = ({ title, expanded, onToggle, values, onChange }: AddressSectionProps) => {
  const { t } = useTranslation('common')

  const handleChange = (field: keyof AddressValues, value: string) => {
    onChange({ ...values, [field]: value })
  }

  return (
    <Card>
      <CardHeader className="pb-0">
        <button
          type="button"
          className="flex items-center justify-between w-full cursor-pointer"
          onClick={onToggle}
        >
          <CardTitle className="text-base">{title}</CardTitle>
          {expanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
        </button>
      </CardHeader>
      {expanded && (
        <CardContent className="pt-4 grid gap-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('labels.name')}</label>
              <Input
                value={values.fullName}
                onChange={(e) => handleChange('fullName', e.target.value)}
                placeholder={t('labels.name')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('labels.phone')}</label>
              <Input
                value={values.phone}
                onChange={(e) => handleChange('phone', e.target.value)}
                placeholder={t('labels.phone')}
              />
            </div>
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t('labels.addressLine1', 'Address Line 1')}</label>
            <Input
              value={values.addressLine1}
              onChange={(e) => handleChange('addressLine1', e.target.value)}
              placeholder={t('labels.addressLine1', 'Address Line 1')}
            />
          </div>
          <div className="space-y-2">
            <label className="text-sm font-medium">{t('labels.addressLine2', 'Address Line 2')}</label>
            <Input
              value={values.addressLine2}
              onChange={(e) => handleChange('addressLine2', e.target.value)}
              placeholder={t('labels.addressLine2', 'Address Line 2')}
            />
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('labels.ward', 'Ward')}</label>
              <Input
                value={values.ward}
                onChange={(e) => handleChange('ward', e.target.value)}
                placeholder={t('labels.ward', 'Ward')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('labels.district', 'District')}</label>
              <Input
                value={values.district}
                onChange={(e) => handleChange('district', e.target.value)}
                placeholder={t('labels.district', 'District')}
              />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t('labels.province', 'Province')}</label>
              <Input
                value={values.province}
                onChange={(e) => handleChange('province', e.target.value)}
                placeholder={t('labels.province', 'Province')}
              />
            </div>
          </div>
        </CardContent>
      )}
    </Card>
  )
}

// --- Main Page Component ---

export const ManualCreateOrderPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const createOrderMutation = useManualCreateOrderMutation()
  usePageContext('Orders')

  // Form state
  const form = useForm<ManualOrderFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createManualOrderSchema(t)) as unknown as Resolver<ManualOrderFormData>,
    mode: 'onBlur',
    defaultValues: {
      customerEmail: '',
      customerName: '',
      customerPhone: '',
      shippingAmount: 0,
      discountAmount: 0,
      taxAmount: 0,
      currency: 'VND',
      shippingMethod: '',
      couponCode: '',
      customerNotes: '',
      internalNotes: '',
      paymentMethod: '',
      initialPaymentStatus: '',
    },
  })

  const currency = form.watch('currency') || 'VND'
  const shippingAmount = form.watch('shippingAmount') || 0
  const orderDiscount = form.watch('discountAmount') || 0
  const taxAmount = form.watch('taxAmount') || 0

  // Order items state
  const [orderItems, setOrderItems] = useState<OrderItemRow[]>([])

  // Customer search state
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerSummaryDto | null>(null)

  const handleSelectCustomer = useCallback((customer: CustomerSummaryDto) => {
    setSelectedCustomer(customer)
    form.setValue('customerEmail', customer.email)
    form.setValue('customerName', `${customer.firstName} ${customer.lastName}`.trim())
    if (customer.phone) {
      form.setValue('customerPhone', customer.phone)
    }
  }, [form])

  const handleClearCustomer = useCallback(() => {
    setSelectedCustomer(null)
    // Don't clear form fields - let user keep or modify them
  }, [])

  // Address state
  const [shippingExpanded, setShippingExpanded] = useState(false)
  const [billingExpanded, setBillingExpanded] = useState(false)
  const [sameAsShipping, setSameAsShipping] = useState(true)
  const [shippingAddress, setShippingAddress] = useState<AddressValues>(emptyAddress)
  const [billingAddress, setBillingAddress] = useState<AddressValues>(emptyAddress)

  // Permission check
  const canManageOrders = hasPermission(Permissions.OrdersManage)

  // Computed values
  const existingVariantIds = useMemo(
    () => new Set(orderItems.map((item) => item.variantId)),
    [orderItems],
  )

  const subtotal = useMemo(
    () => orderItems.reduce((sum, item) => {
      const price = item.customPrice ?? item.originalPrice
      return sum + price * item.quantity - item.discount
    }, 0),
    [orderItems],
  )

  const grandTotal = useMemo(
    () => subtotal - orderDiscount + shippingAmount + taxAmount,
    [subtotal, orderDiscount, shippingAmount, taxAmount],
  )

  // Handlers
  const handleAddVariant = useCallback((variant: ProductVariantLookupDto) => {
    setOrderItems((prev) => [
      ...prev,
      {
        variantId: variant.id,
        productName: variant.productName,
        variantName: variant.variantName,
        sku: variant.sku,
        imageUrl: variant.imageUrl,
        quantity: 1,
        originalPrice: variant.price,
        customPrice: null,
        discount: 0,
        stockQuantity: variant.stockQuantity,
      },
    ])
  }, [])

  const handleRemoveItem = useCallback((variantId: string) => {
    setOrderItems((prev) => prev.filter((item) => item.variantId !== variantId))
  }, [])

  const handleItemChange = useCallback(
    (variantId: string, field: 'quantity' | 'customPrice' | 'discount', value: number | null) => {
      setOrderItems((prev) =>
        prev.map((item) => {
          if (item.variantId !== variantId) return item
          return { ...item, [field]: value }
        }),
      )
    },
    [],
  )

  const buildAddressPayload = (addr: AddressValues) => {
    if (!addr.fullName && !addr.addressLine1) return undefined
    return {
      fullName: addr.fullName,
      phone: addr.phone,
      addressLine1: addr.addressLine1,
      addressLine2: addr.addressLine2 || undefined,
      ward: addr.ward,
      district: addr.district,
      province: addr.province,
      country: addr.country || undefined,
      postalCode: addr.postalCode || undefined,
    }
  }

  const handleSubmit = form.handleSubmit(async (data) => {
    const request: ManualCreateOrderRequest = {
      customerEmail: data.customerEmail,
      customerName: data.customerName || undefined,
      customerPhone: data.customerPhone || undefined,
      customerId: selectedCustomer?.id,
      items: orderItems.map((item) => ({
        productVariantId: item.variantId,
        quantity: item.quantity,
        unitPrice: item.customPrice ?? undefined,
        discountAmount: item.discount > 0 ? item.discount : undefined,
      })),
      shippingAddress: buildAddressPayload(shippingAddress),
      billingAddress: sameAsShipping
        ? buildAddressPayload(shippingAddress)
        : buildAddressPayload(billingAddress),
      shippingMethod: data.shippingMethod || undefined,
      shippingAmount: data.shippingAmount || undefined,
      couponCode: data.couponCode || undefined,
      discountAmount: data.discountAmount || undefined,
      taxAmount: data.taxAmount || undefined,
      customerNotes: data.customerNotes || undefined,
      internalNotes: data.internalNotes || undefined,
      paymentMethod: data.paymentMethod || undefined,
      initialPaymentStatus: data.initialPaymentStatus || undefined,
      currency: data.currency || undefined,
    }

    try {
      const order = await createOrderMutation.mutateAsync(request)
      toast.success(t('orders.manualCreate.success'))
      navigate(`/portal/ecommerce/orders/${order.id}`)
    } catch {
      toast.error(t('orders.manualCreate.error'))
    }
  })

  if (!canManageOrders) {
    return (
      <div className="space-y-6">
        <div className="p-8 text-center">
          <p className="text-muted-foreground">{t('messages.permissionDenied', 'You do not have permission to access this page.')}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center gap-4">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => navigate('/portal/ecommerce/orders')}
          className="cursor-pointer"
          aria-label={t('orders.backToOrders')}
        >
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <PageHeader
          icon={ShoppingCart}
          title={t('orders.manualCreate.title')}
          description={t('orders.manualCreate.description')}
          responsive
        />
      </div>

      <Form {...form}>
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Section 1: Customer Info */}
          <Card className="shadow-sm">
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <CreditCard className="h-4 w-4" />
                {t('orders.manualCreate.customerInfo')}
              </CardTitle>
            </CardHeader>
            <CardContent className="grid gap-4">
              {/* Customer Search */}
              <div className="space-y-2">
                <Label>{t('orders.manualCreate.searchCustomer')}</Label>
                <CustomerSearchTypeahead
                  onSelect={handleSelectCustomer}
                  onClear={handleClearCustomer}
                  selectedCustomer={selectedCustomer}
                />
                {!selectedCustomer && (
                  <p className="text-xs text-muted-foreground">{t('orders.manualCreate.orEnterManually')}</p>
                )}
              </div>
              <Separator className="my-2" />
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <FormField
                  control={form.control}
                  name="customerEmail"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('labels.email')}</FormLabel>
                      <FormControl>
                        <Input {...field} type="email" placeholder={t('labels.email')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="customerName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('labels.name')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder={t('labels.name')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="customerPhone"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('labels.phone')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder={t('labels.phone')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
            </CardContent>
          </Card>

          {/* Section 2: Order Items */}
          <Card className="shadow-sm">
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <Package className="h-4 w-4" />
                {t('orders.manualCreate.orderItems')}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {/* Product Search */}
              <ProductSearchTypeahead
                onSelect={handleAddVariant}
                existingVariantIds={existingVariantIds}
                currency={currency}
              />

              {/* Order Items Table */}
              {orderItems.length > 0 ? (
                <div className="rounded-lg border overflow-hidden">
                  <table className="w-full">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="text-left p-3 text-sm font-medium">{t('orders.manualCreate.product')}</th>
                        <th className="text-left p-3 text-sm font-medium">{t('orders.manualCreate.sku')}</th>
                        <th className="text-center p-3 text-sm font-medium w-20">{t('orders.manualCreate.quantity')}</th>
                        <th className="text-right p-3 text-sm font-medium w-32">{t('orders.manualCreate.unitPrice')}</th>
                        <th className="text-right p-3 text-sm font-medium w-28">{t('orders.manualCreate.discount')}</th>
                        <th className="text-right p-3 text-sm font-medium w-32">{t('orders.manualCreate.lineTotal')}</th>
                        <th className="w-10 p-3"></th>
                      </tr>
                    </thead>
                    <tbody>
                      {orderItems.map((item) => {
                        const price = item.customPrice ?? item.originalPrice
                        const lineTotal = price * item.quantity - item.discount

                        return (
                          <tr key={item.variantId} className="border-b last:border-b-0">
                            <td className="p-3">
                              <div className="flex items-center gap-3">
                                <FilePreviewTrigger
                                  file={{
                                    url: item.imageUrl ?? '',
                                    name: item.productName,
                                  }}
                                  thumbnailWidth={40}
                                  thumbnailHeight={40}
                                  className="rounded-md"
                                />
                                <div className="min-w-0">
                                  <p className="text-sm font-medium truncate">{item.productName}</p>
                                  {item.variantName && (
                                    <p className="text-xs text-muted-foreground">{item.variantName}</p>
                                  )}
                                </div>
                              </div>
                            </td>
                            <td className="p-3">
                              {item.sku ? (
                                <code className="text-xs bg-muted px-1.5 py-0.5 rounded">{item.sku}</code>
                              ) : (
                                <span className="text-muted-foreground">-</span>
                              )}
                            </td>
                            <td className="p-3">
                              <Input
                                type="number"
                                min={1}
                                max={item.stockQuantity > 0 ? item.stockQuantity : undefined}
                                value={item.quantity}
                                onChange={(e) =>
                                  handleItemChange(item.variantId, 'quantity', Math.max(1, parseInt(e.target.value) || 1))
                                }
                                className="w-20 text-center h-8"
                                aria-label={t('orders.manualCreate.quantity')}
                              />
                            </td>
                            <td className="p-3">
                              <Input
                                type="number"
                                min={0}
                                value={item.customPrice ?? item.originalPrice}
                                onChange={(e) => {
                                  const val = parseFloat(e.target.value)
                                  handleItemChange(
                                    item.variantId,
                                    'customPrice',
                                    isNaN(val) ? null : val,
                                  )
                                }}
                                className="w-32 text-right h-8"
                                aria-label={t('orders.manualCreate.unitPrice')}
                              />
                            </td>
                            <td className="p-3">
                              <Input
                                type="number"
                                min={0}
                                value={item.discount}
                                onChange={(e) =>
                                  handleItemChange(
                                    item.variantId,
                                    'discount',
                                    Math.max(0, parseFloat(e.target.value) || 0),
                                  )
                                }
                                className="w-28 text-right h-8"
                                aria-label={t('orders.manualCreate.discount')}
                              />
                            </td>
                            <td className="p-3 text-right">
                              <span className="text-sm font-medium">
                                {formatCurrency(lineTotal, currency)}
                              </span>
                            </td>
                            <td className="p-3">
                              <Button
                                type="button"
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 cursor-pointer text-muted-foreground hover:text-destructive"
                                onClick={() => handleRemoveItem(item.variantId)}
                                aria-label={`${t('orders.manualCreate.removeItem')} ${item.productName}`}
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            </td>
                          </tr>
                        )
                      })}
                    </tbody>
                    <tfoot>
                      <tr className="bg-muted/30">
                        <td colSpan={5} className="p-3 text-right text-sm font-medium">
                          {t('orders.manualCreate.subtotal')}
                        </td>
                        <td className="p-3 text-right text-sm font-medium">
                          {formatCurrency(subtotal, currency)}
                        </td>
                        <td></td>
                      </tr>
                    </tfoot>
                  </table>
                </div>
              ) : (
                <div className="flex flex-col items-center justify-center py-12 border rounded-lg border-dashed">
                  <Plus className="h-8 w-8 text-muted-foreground/40 mb-2" />
                  <p className="text-sm text-muted-foreground">
                    {t('orders.manualCreate.searchProducts')}
                  </p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Section 3: Shipping Address */}
          <AddressSection
            title={t('orders.manualCreate.shippingAddress')}
            expanded={shippingExpanded}
            onToggle={() => setShippingExpanded(!shippingExpanded)}
            values={shippingAddress}
            onChange={setShippingAddress}
          />

          {/* Section 4: Billing Address */}
          <Card>
            <CardHeader className="pb-0">
              <div className="flex items-center justify-between">
                <button
                  type="button"
                  className="flex items-center justify-between flex-1 cursor-pointer"
                  onClick={() => setBillingExpanded(!billingExpanded)}
                >
                  <CardTitle className="text-base">{t('orders.manualCreate.billingAddress')}</CardTitle>
                  {billingExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                </button>
              </div>
              <div className="flex items-center gap-2 pt-2">
                <Checkbox
                  id="sameAsShipping"
                  checked={sameAsShipping}
                  onCheckedChange={(checked) => setSameAsShipping(!!checked)}
                  className="cursor-pointer"
                />
                <label
                  htmlFor="sameAsShipping"
                  className="text-sm text-muted-foreground cursor-pointer"
                >
                  {t('orders.manualCreate.sameAsShipping')}
                </label>
              </div>
            </CardHeader>
            {billingExpanded && !sameAsShipping && (
              <CardContent className="pt-4 grid gap-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <label className="text-sm font-medium">{t('labels.name')}</label>
                    <Input
                      value={billingAddress.fullName}
                      onChange={(e) => setBillingAddress({ ...billingAddress, fullName: e.target.value })}
                      placeholder={t('labels.name')}
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">{t('labels.phone')}</label>
                    <Input
                      value={billingAddress.phone}
                      onChange={(e) => setBillingAddress({ ...billingAddress, phone: e.target.value })}
                      placeholder={t('labels.phone')}
                    />
                  </div>
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium">{t('labels.addressLine1', 'Address Line 1')}</label>
                  <Input
                    value={billingAddress.addressLine1}
                    onChange={(e) => setBillingAddress({ ...billingAddress, addressLine1: e.target.value })}
                    placeholder={t('labels.addressLine1', 'Address Line 1')}
                  />
                </div>
                <div className="space-y-2">
                  <label className="text-sm font-medium">{t('labels.addressLine2', 'Address Line 2')}</label>
                  <Input
                    value={billingAddress.addressLine2}
                    onChange={(e) => setBillingAddress({ ...billingAddress, addressLine2: e.target.value })}
                    placeholder={t('labels.addressLine2', 'Address Line 2')}
                  />
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                  <div className="space-y-2">
                    <label className="text-sm font-medium">{t('labels.ward', 'Ward')}</label>
                    <Input
                      value={billingAddress.ward}
                      onChange={(e) => setBillingAddress({ ...billingAddress, ward: e.target.value })}
                      placeholder={t('labels.ward', 'Ward')}
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">{t('labels.district', 'District')}</label>
                    <Input
                      value={billingAddress.district}
                      onChange={(e) => setBillingAddress({ ...billingAddress, district: e.target.value })}
                      placeholder={t('labels.district', 'District')}
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="text-sm font-medium">{t('labels.province', 'Province')}</label>
                    <Input
                      value={billingAddress.province}
                      onChange={(e) => setBillingAddress({ ...billingAddress, province: e.target.value })}
                      placeholder={t('labels.province', 'Province')}
                    />
                  </div>
                </div>
              </CardContent>
            )}
          </Card>

          {/* Section 5: Order Summary */}
          <Card className="shadow-sm">
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <FileText className="h-4 w-4" />
                {t('orders.manualCreate.orderSummary')}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              {/* Shipping & Discount Row */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <FormField
                  control={form.control}
                  name="shippingMethod"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('orders.manualCreate.shippingMethod')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder={t('orders.manualCreate.shippingMethod')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="shippingAmount"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('orders.manualCreate.shippingAmount')}</FormLabel>
                      <FormControl>
                        <Input {...field} type="number" min={0} className="text-right" />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="couponCode"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('orders.manualCreate.couponCode')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder={t('orders.manualCreate.couponCode')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <FormField
                  control={form.control}
                  name="discountAmount"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('orders.manualCreate.orderDiscount')}</FormLabel>
                      <FormControl>
                        <Input {...field} type="number" min={0} className="text-right" />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="taxAmount"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('orders.manualCreate.taxAmount')}</FormLabel>
                      <FormControl>
                        <Input {...field} type="number" min={0} className="text-right" />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="currency"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('labels.currency', 'Currency')}</FormLabel>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          <SelectItem value="VND" className="cursor-pointer">VND</SelectItem>
                          <SelectItem value="USD" className="cursor-pointer">USD</SelectItem>
                          <SelectItem value="EUR" className="cursor-pointer">EUR</SelectItem>
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Grand Total */}
              <div className="space-y-2 border-t pt-4">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">{t('orders.manualCreate.subtotal')}</span>
                  <span>{formatCurrency(subtotal, currency)}</span>
                </div>
                {orderDiscount > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{t('orders.manualCreate.orderDiscount')}</span>
                    <span className="text-green-600">-{formatCurrency(orderDiscount, currency)}</span>
                  </div>
                )}
                {shippingAmount > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{t('orders.manualCreate.shippingAmount')}</span>
                    <span>{formatCurrency(shippingAmount, currency)}</span>
                  </div>
                )}
                {taxAmount > 0 && (
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">{t('orders.manualCreate.taxAmount')}</span>
                    <span>{formatCurrency(taxAmount, currency)}</span>
                  </div>
                )}
                <Separator />
                <div className="flex justify-between font-semibold text-lg">
                  <span>{t('orders.manualCreate.grandTotal')}</span>
                  <span>{formatCurrency(grandTotal, currency)}</span>
                </div>
              </div>

              <Separator />

              {/* Payment */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="paymentMethod"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('orders.manualCreate.paymentMethod')}</FormLabel>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue placeholder={t('orders.manualCreate.paymentMethod')} />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          <SelectItem value="COD" className="cursor-pointer">{t('payments.methods.COD')}</SelectItem>
                          <SelectItem value="BankTransfer" className="cursor-pointer">{t('payments.methods.BankTransfer')}</SelectItem>
                          <SelectItem value="CreditCard" className="cursor-pointer">{t('payments.methods.CreditCard')}</SelectItem>
                          <SelectItem value="EWallet" className="cursor-pointer">{t('payments.methods.EWallet')}</SelectItem>
                          <SelectItem value="DebitCard" className="cursor-pointer">{t('payments.methods.DebitCard')}</SelectItem>
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="initialPaymentStatus"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('orders.manualCreate.paymentStatus')}</FormLabel>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue placeholder={t('orders.manualCreate.paymentStatus')} />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          <SelectItem value="Pending" className="cursor-pointer">{t('payments.statuses.Pending')}</SelectItem>
                          <SelectItem value="Paid" className="cursor-pointer">{t('payments.statuses.Paid')}</SelectItem>
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Notes */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="customerNotes"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('orders.manualCreate.customerNotes')}</FormLabel>
                      <FormControl>
                        <Textarea {...field} rows={3} placeholder={t('orders.manualCreate.customerNotes')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="internalNotes"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('orders.manualCreate.internalNotes')}</FormLabel>
                      <FormControl>
                        <Textarea
                          {...field}
                          rows={3}
                          placeholder={t('orders.manualCreate.internalNotes')}
                          className="border-amber-200 dark:border-amber-800"
                        />
                      </FormControl>
                      <p className="text-xs text-muted-foreground mt-1">
                        {t('orders.manualCreate.internalNotesHint')}
                      </p>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
            </CardContent>
          </Card>

          {/* Sticky Action Bar */}
          <div className="sticky bottom-0 bg-background border-t p-4 -mx-6 px-6 flex justify-end gap-3 z-10">
            <Button
              type="button"
              variant="outline"
              onClick={() => navigate('/portal/ecommerce/orders')}
              className="cursor-pointer"
            >
              {t('labels.cancel')}
            </Button>
            <Button
              type="submit"
              disabled={createOrderMutation.isPending || orderItems.length === 0}
              className="cursor-pointer"
            >
              {createOrderMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('orders.manualCreate.createOrder')}
            </Button>
          </div>
        </form>
      </Form>
    </div>
  )
}

export default ManualCreateOrderPage
