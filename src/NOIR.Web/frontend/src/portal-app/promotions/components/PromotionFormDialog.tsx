import { useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, Shuffle, Tag } from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  DatePicker,
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
  Textarea,
} from '@uikit'
import { useCreatePromotionMutation, useUpdatePromotionMutation } from '@/portal-app/promotions/queries'
import type { PromotionDto, PromotionType, DiscountType, PromotionApplyLevel } from '@/types/promotion'
import { toast } from 'sonner'

// ============================================================================
// Schema
// ============================================================================

const createPromotionSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { count: 200 })),
    code: z.string().min(1, t('validation.required')).max(50, t('validation.maxLength', { count: 50 }))
      .regex(/^[A-Z0-9_-]+$/, t('promotions.validation.codeFormat')),
    description: z.string().max(500, t('validation.maxLength', { count: 500 })).optional().nullable(),
    promotionType: z.enum(['VoucherCode', 'FlashSale', 'BundleDeal', 'FreeShipping'] as const),
    discountType: z.enum(['FixedAmount', 'Percentage', 'FreeShipping', 'BuyXGetY'] as const),
    discountValue: z.coerce.number().min(0, t('validation.minValue', { min: 0 })),
    maxDiscountAmount: z.coerce.number().min(0).optional().nullable(),
    minOrderValue: z.coerce.number().min(0).optional().nullable(),
    minItemQuantity: z.coerce.number().int().min(0).optional().nullable(),
    usageLimitTotal: z.coerce.number().int().min(0).optional().nullable(),
    usageLimitPerUser: z.coerce.number().int().min(0).optional().nullable(),
    startDate: z.date({ error: t('validation.required') }),
    endDate: z.date({ error: t('validation.required') }),
    applyLevel: z.enum(['Cart', 'Product', 'Category'] as const).default('Cart'),
  }).refine((data) => data.endDate > data.startDate, {
    message: t('promotions.validation.endDateAfterStart'),
    path: ['endDate'],
  }).refine((data) => {
    if (data.discountType === 'Percentage' && data.discountValue > 100) {
      return false
    }
    return true
  }, {
    message: t('promotions.validation.percentageMax'),
    path: ['discountValue'],
  })

type PromotionFormData = z.infer<ReturnType<typeof createPromotionSchema>>

// ============================================================================
// Constants
// ============================================================================

const PROMOTION_TYPES: { value: PromotionType; labelKey: string; fallback: string }[] = [
  { value: 'VoucherCode', labelKey: 'promotions.type.vouchercode', fallback: 'Voucher Code' },
  { value: 'FlashSale', labelKey: 'promotions.type.flashsale', fallback: 'Flash Sale' },
  { value: 'BundleDeal', labelKey: 'promotions.type.bundledeal', fallback: 'Bundle Deal' },
  { value: 'FreeShipping', labelKey: 'promotions.type.freeshipping', fallback: 'Free Shipping' },
]

const DISCOUNT_TYPES: { value: DiscountType; labelKey: string; fallback: string }[] = [
  { value: 'FixedAmount', labelKey: 'promotions.discountType.fixedAmount', fallback: 'Fixed Amount' },
  { value: 'Percentage', labelKey: 'promotions.discountType.percentage', fallback: 'Percentage' },
  { value: 'FreeShipping', labelKey: 'promotions.discountType.freeShipping', fallback: 'Free Shipping' },
  { value: 'BuyXGetY', labelKey: 'promotions.discountType.buyXGetY', fallback: 'Buy X Get Y' },
]

const APPLY_LEVELS: { value: PromotionApplyLevel; labelKey: string; fallback: string }[] = [
  { value: 'Cart', labelKey: 'promotions.applyLevel.cart', fallback: 'Entire Cart' },
  { value: 'Product', labelKey: 'promotions.applyLevel.product', fallback: 'Specific Products' },
  { value: 'Category', labelKey: 'promotions.applyLevel.category', fallback: 'Specific Categories' },
]

// ============================================================================
// Component
// ============================================================================

interface PromotionFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  promotion?: PromotionDto | null
  onSuccess?: () => void
}

export const PromotionFormDialog = ({ open, onOpenChange, promotion, onSuccess }: PromotionFormDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!promotion
  const createMutation = useCreatePromotionMutation()
  const updateMutation = useUpdatePromotionMutation()

  const form = useForm<PromotionFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createPromotionSchema(t)) as unknown as Resolver<PromotionFormData>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      code: '',
      description: '',
      promotionType: 'VoucherCode',
      discountType: 'Percentage',
      discountValue: 0,
      maxDiscountAmount: null,
      minOrderValue: null,
      minItemQuantity: null,
      usageLimitTotal: null,
      usageLimitPerUser: null,
      startDate: undefined,
      endDate: undefined,
      applyLevel: 'Cart',
    },
  })

  const watchDiscountType = form.watch('discountType')

  // Reset form when dialog opens/closes or promotion changes
  useEffect(() => {
    if (open) {
      if (promotion) {
        form.reset({
          name: promotion.name,
          code: promotion.code,
          description: promotion.description ?? '',
          promotionType: promotion.promotionType,
          discountType: promotion.discountType,
          discountValue: promotion.discountValue,
          maxDiscountAmount: promotion.maxDiscountAmount ?? null,
          minOrderValue: promotion.minOrderValue ?? null,
          minItemQuantity: promotion.minItemQuantity ?? null,
          usageLimitTotal: promotion.usageLimitTotal ?? null,
          usageLimitPerUser: promotion.usageLimitPerUser ?? null,
          startDate: new Date(promotion.startDate),
          endDate: new Date(promotion.endDate),
          applyLevel: promotion.applyLevel,
        })
      } else {
        form.reset({
          name: '',
          code: '',
          description: '',
          promotionType: 'VoucherCode',
          discountType: 'Percentage',
          discountValue: 0,
          maxDiscountAmount: null,
          minOrderValue: null,
          minItemQuantity: null,
          usageLimitTotal: null,
          usageLimitPerUser: null,
          startDate: undefined,
          endDate: undefined,
          applyLevel: 'Cart',
        })
      }
    }
  }, [open, promotion, form])

  const generateCode = useCallback(() => {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
    const prefix = form.getValues('promotionType') === 'VoucherCode' ? 'VC' :
      form.getValues('promotionType') === 'FlashSale' ? 'FS' :
      form.getValues('promotionType') === 'BundleDeal' ? 'BD' : 'FP'
    let code = prefix + '-'
    for (let i = 0; i < 8; i++) {
      code += chars.charAt(Math.floor(Math.random() * chars.length))
    }
    form.setValue('code', code, { shouldValidate: true })
  }, [form])

  const onSubmit = async (data: PromotionFormData) => {
    const payload = {
      name: data.name,
      code: data.code,
      description: data.description || null,
      promotionType: data.promotionType,
      discountType: data.discountType,
      discountValue: data.discountValue,
      startDate: data.startDate.toISOString(),
      endDate: data.endDate.toISOString(),
      applyLevel: data.applyLevel,
      maxDiscountAmount: data.maxDiscountAmount || null,
      minOrderValue: data.minOrderValue || null,
      minItemQuantity: data.minItemQuantity || null,
      usageLimitTotal: data.usageLimitTotal || null,
      usageLimitPerUser: data.usageLimitPerUser || null,
    }

    try {
      if (isEditing && promotion) {
        await updateMutation.mutateAsync({ id: promotion.id, request: payload })
        toast.success(t('promotions.updateSuccess', 'Promotion updated successfully'))
      } else {
        await createMutation.mutateAsync(payload)
        toast.success(t('promotions.createSuccess', 'Promotion created successfully'))
      }
      onSuccess?.()
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : isEditing
        ? t('promotions.updateError', 'Failed to update promotion')
        : t('promotions.createError', 'Failed to create promotion')
      toast.error(message)
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[600px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <Tag className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>
                {isEditing ? t('promotions.editPromotion', 'Edit Promotion') : t('promotions.createPromotion', 'Create Promotion')}
              </CredenzaTitle>
              <CredenzaDescription>
                {isEditing
                  ? t('promotions.editPromotionDescription', 'Update the promotion details below.')
                  : t('promotions.createPromotionDescription', 'Fill in the details to create a new promotion.')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <CredenzaBody className="space-y-4">
              {/* Name */}
              <FormField
                control={form.control}
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('labels.name', 'Name')}</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        placeholder={t('promotions.namePlaceholder', 'Enter promotion name')}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Code with auto-generate */}
              <FormField
                control={form.control}
                name="code"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('promotions.code', 'Code')}</FormLabel>
                    <div className="flex gap-2">
                      <FormControl>
                        <Input
                          {...field}
                          placeholder={t('promotions.codePlaceholder', 'PROMO-CODE')}
                          className="font-mono uppercase"
                        />
                      </FormControl>
                      <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        onClick={generateCode}
                        className="cursor-pointer shrink-0"
                        aria-label={t('promotions.generateCode', 'Generate code')}
                      >
                        <Shuffle className="h-4 w-4" />
                      </Button>
                    </div>
                    <FormDescription>
                      {t('promotions.codeDescription', 'Unique code customers enter at checkout. Uppercase letters, numbers, hyphens only.')}
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Description */}
              <FormField
                control={form.control}
                name="description"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('labels.description', 'Description')}</FormLabel>
                    <FormControl>
                      <Textarea
                        {...field}
                        value={field.value ?? ''}
                        placeholder={t('promotions.descriptionPlaceholder', 'Brief description of the promotion')}
                        rows={2}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              {/* Promotion Type and Discount Type in a row */}
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="promotionType"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('promotions.type.label', 'Promotion Type')}</FormLabel>
                      <Select value={field.value} onValueChange={field.onChange}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue placeholder={t('promotions.selectType', 'Select type')} />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {PROMOTION_TYPES.map(({ value, labelKey, fallback }) => (
                            <SelectItem key={value} value={value} className="cursor-pointer">
                              {t(labelKey, fallback)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="discountType"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('promotions.discountType.label', 'Discount Type')}</FormLabel>
                      <Select value={field.value} onValueChange={field.onChange}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue placeholder={t('promotions.selectDiscountType', 'Select discount type')} />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {DISCOUNT_TYPES.map(({ value, labelKey, fallback }) => (
                            <SelectItem key={value} value={value} className="cursor-pointer">
                              {t(labelKey, fallback)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Discount Value and Max Discount */}
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="discountValue"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>
                        {watchDiscountType === 'Percentage'
                          ? t('promotions.discountPercentage', 'Discount (%)')
                          : t('promotions.discountAmount', 'Discount Amount')}
                      </FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          {...field}
                          onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)}
                          min={0}
                          max={watchDiscountType === 'Percentage' ? 100 : undefined}
                          step={watchDiscountType === 'Percentage' ? 1 : 0.01}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                {watchDiscountType === 'Percentage' && (
                  <FormField
                    control={form.control}
                    name="maxDiscountAmount"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('promotions.maxDiscount', 'Max Discount Amount')}</FormLabel>
                        <FormControl>
                          <Input
                            type="number"
                            value={field.value ?? ''}
                            onChange={(e) => field.onChange(e.target.value ? parseFloat(e.target.value) : null)}
                            min={0}
                            step={0.01}
                            placeholder={t('promotions.maxDiscountPlaceholder', 'No limit')}
                          />
                        </FormControl>
                        <FormDescription>
                          {t('promotions.maxDiscountDescription', 'Maximum discount cap for percentage discounts')}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                )}
              </div>

              {/* Min Order Value and Apply Level */}
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="minOrderValue"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('promotions.minOrderValue', 'Min Order Value')}</FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          value={field.value ?? ''}
                          onChange={(e) => field.onChange(e.target.value ? parseFloat(e.target.value) : null)}
                          min={0}
                          step={0.01}
                          placeholder={t('promotions.minOrderValuePlaceholder', 'No minimum')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="applyLevel"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('promotions.applyLevel.label', 'Apply Level')}</FormLabel>
                      <Select value={field.value} onValueChange={field.onChange}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {APPLY_LEVELS.map(({ value, labelKey, fallback }) => (
                            <SelectItem key={value} value={value} className="cursor-pointer">
                              {t(labelKey, fallback)}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Usage Limits */}
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="usageLimitTotal"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('promotions.usageLimitTotal', 'Total Usage Limit')}</FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          value={field.value ?? ''}
                          onChange={(e) => field.onChange(e.target.value ? parseInt(e.target.value) : null)}
                          min={0}
                          step={1}
                          placeholder={t('promotions.usageLimitPlaceholder', 'Unlimited')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="usageLimitPerUser"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('promotions.usageLimitPerUser', 'Per User Limit')}</FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          value={field.value ?? ''}
                          onChange={(e) => field.onChange(e.target.value ? parseInt(e.target.value) : null)}
                          min={0}
                          step={1}
                          placeholder={t('promotions.usageLimitPlaceholder', 'Unlimited')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Date Range */}
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="startDate"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('promotions.startDate', 'Start Date')}</FormLabel>
                      <FormControl>
                        <DatePicker
                          value={field.value}
                          onChange={field.onChange}
                          placeholder={t('promotions.selectStartDate', 'Select start date')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="endDate"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('promotions.endDate', 'End Date')}</FormLabel>
                      <FormControl>
                        <DatePicker
                          value={field.value}
                          onChange={field.onChange}
                          placeholder={t('promotions.selectEndDate', 'Select end date')}
                          minDate={form.getValues('startDate')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              {/* Min Item Quantity */}
              <FormField
                control={form.control}
                name="minItemQuantity"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('promotions.minItemQuantity', 'Min Item Quantity')}</FormLabel>
                    <FormControl>
                      <Input
                        type="number"
                        value={field.value ?? ''}
                        onChange={(e) => field.onChange(e.target.value ? parseInt(e.target.value) : null)}
                        min={0}
                        step={1}
                        placeholder={t('promotions.minItemQuantityPlaceholder', 'No minimum')}
                      />
                    </FormControl>
                    <FormDescription>
                      {t('promotions.minItemQuantityDescription', 'Minimum number of items required in the cart')}
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </CredenzaBody>

            <CredenzaFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                className="cursor-pointer"
              >
                {t('labels.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isSubmitting} className="cursor-pointer">
                {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isEditing ? t('labels.save', 'Save') : t('labels.create', 'Create')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
