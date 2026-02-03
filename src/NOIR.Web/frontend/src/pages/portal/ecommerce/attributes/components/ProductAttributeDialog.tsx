import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Combobox } from '@/components/ui/combobox'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/switch'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useCreateProductAttribute, useUpdateProductAttribute } from '@/hooks/useProductAttributes'
import type { ProductAttributeListItem, AttributeType } from '@/types/productAttribute'
import { toast } from 'sonner'
import { Loader2 } from 'lucide-react'

const ATTRIBUTE_TYPES: AttributeType[] = [
  'Select',
  'MultiSelect',
  'Text',
  'TextArea',
  'Number',
  'Decimal',
  'Boolean',
  'Date',
  'DateTime',
  'Color',
  'Range',
  'Url',
  'File',
]

const createAttributeSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    code: z.string().min(1, t('validation.required')).max(50, t('validation.maxLength', { count: 50 }))
      .regex(/^[a-z0-9_]+$/, t('validation.identifierFormat')),
    name: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 })),
    type: z.string().min(1, t('validation.required')),
    isFilterable: z.boolean(),
    isSearchable: z.boolean(),
    isRequired: z.boolean(),
    isVariantAttribute: z.boolean(),
    showInProductCard: z.boolean(),
    showInSpecifications: z.boolean(),
    isGlobal: z.boolean(),
    isActive: z.boolean(),
    sortOrder: z.number().int().min(0).default(0),
    unit: z.string().max(20, t('validation.maxLength', { count: 20 })).optional().nullable(),
    validationRegex: z.string().max(500, t('validation.maxLength', { count: 500 })).optional().nullable(),
    minValue: z.number().optional().nullable(),
    maxValue: z.number().optional().nullable(),
    maxLength: z.number().int().min(1).optional().nullable(),
    defaultValue: z.string().max(500, t('validation.maxLength', { count: 500 })).optional().nullable(),
    placeholder: z.string().max(200, t('validation.maxLength', { count: 200 })).optional().nullable(),
    helpText: z.string().max(500, t('validation.maxLength', { count: 500 })).optional().nullable(),
  })

type AttributeFormData = z.infer<ReturnType<typeof createAttributeSchema>>

interface ProductAttributeDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  attribute?: ProductAttributeListItem | null
  onSuccess?: () => void
}

export function ProductAttributeDialog({
  open,
  onOpenChange,
  attribute,
  onSuccess,
}: ProductAttributeDialogProps) {
  const { t } = useTranslation('common')
  const isEditing = !!attribute
  const createAttributeHook = useCreateProductAttribute()
  const updateAttributeHook = useUpdateProductAttribute()

  const form = useForm<AttributeFormData>({
    resolver: zodResolver(createAttributeSchema(t)),
    mode: 'onBlur',
    defaultValues: {
      code: '',
      name: '',
      type: 'Text',
      isFilterable: false,
      isSearchable: false,
      isRequired: false,
      isVariantAttribute: false,
      showInProductCard: false,
      showInSpecifications: true,
      isGlobal: false,
      isActive: true,
      sortOrder: 0,
      unit: '',
      validationRegex: '',
      minValue: null,
      maxValue: null,
      maxLength: null,
      defaultValue: '',
      placeholder: '',
      helpText: '',
    },
  })

  // Reset form when dialog opens/closes or attribute changes
  // Note: ProductAttributeListItem only has basic fields (code, name, type, isFilterable,
  // isVariantAttribute, isGlobal, isActive, valueCount). Other fields use defaults when editing.
  // For a full edit experience, consider fetching the complete ProductAttribute.
  useEffect(() => {
    if (open) {
      if (attribute) {
        form.reset({
          code: attribute.code,
          name: attribute.name,
          type: attribute.type,
          isFilterable: attribute.isFilterable ?? false,
          isVariantAttribute: attribute.isVariantAttribute ?? false,
          isGlobal: attribute.isGlobal ?? false,
          isActive: attribute.isActive ?? true,
          // Fields not in ProductAttributeListItem - use defaults
          isSearchable: false,
          isRequired: false,
          showInProductCard: false,
          showInSpecifications: true,
          sortOrder: 0,
          unit: '',
          validationRegex: '',
          minValue: null,
          maxValue: null,
          maxLength: null,
          defaultValue: '',
          placeholder: '',
          helpText: '',
        })
      } else {
        form.reset({
          code: '',
          name: '',
          type: 'Text',
          isFilterable: false,
          isSearchable: false,
          isRequired: false,
          isVariantAttribute: false,
          showInProductCard: false,
          showInSpecifications: true,
          isGlobal: false,
          isActive: true,
          sortOrder: 0,
          unit: '',
          validationRegex: '',
          minValue: null,
          maxValue: null,
          maxLength: null,
          defaultValue: '',
          placeholder: '',
          helpText: '',
        })
      }
    }
  }, [open, attribute, form])

  // Auto-generate code from name
  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const name = e.target.value
    form.setValue('name', name)
    // Only auto-generate code if it's empty or matches the previous auto-generated value
    const currentCode = form.getValues('code')
    const previousName = form.getValues('name')
    const autoCode = previousName.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '')
    if (!currentCode || currentCode === autoCode) {
      const newCode = name.toLowerCase().replace(/[^a-z0-9]+/g, '_').replace(/^_|_$/g, '')
      form.setValue('code', newCode)
    }
  }

  const onSubmit = async (data: AttributeFormData) => {
    // Clean up empty strings to null
    const cleanedData = {
      ...data,
      unit: data.unit || null,
      validationRegex: data.validationRegex || null,
      defaultValue: data.defaultValue || null,
      placeholder: data.placeholder || null,
      helpText: data.helpText || null,
    }

    if (isEditing && attribute) {
      const result = await updateAttributeHook.updateProductAttribute(attribute.id, cleanedData)
      if (result.success) {
        toast.success(t('productAttributes.updateSuccess', 'Product attribute updated successfully'))
        onSuccess?.()
        onOpenChange(false)
      } else {
        toast.error(result.error || t('productAttributes.updateError', 'Failed to update product attribute'))
      }
    } else {
      const result = await createAttributeHook.createProductAttribute(cleanedData)
      if (result.success) {
        toast.success(t('productAttributes.createSuccess', 'Product attribute created successfully'))
        onSuccess?.()
        onOpenChange(false)
      } else {
        toast.error(result.error || t('productAttributes.createError', 'Failed to create product attribute'))
      }
    }
  }

  const isSubmitting = createAttributeHook.isPending || updateAttributeHook.isPending

  // Get type label for display
  const getTypeLabel = (type: string) => {
    const typeLabels: Record<string, string> = {
      Select: t('productAttributes.types.select', 'Select'),
      MultiSelect: t('productAttributes.types.multiSelect', 'Multi-Select'),
      Text: t('productAttributes.types.text', 'Text'),
      TextArea: t('productAttributes.types.textArea', 'Text Area'),
      Number: t('productAttributes.types.number', 'Number'),
      Decimal: t('productAttributes.types.decimal', 'Decimal'),
      Boolean: t('productAttributes.types.boolean', 'Boolean'),
      Date: t('productAttributes.types.date', 'Date'),
      DateTime: t('productAttributes.types.dateTime', 'Date Time'),
      Color: t('productAttributes.types.color', 'Color'),
      Range: t('productAttributes.types.range', 'Range'),
      Url: t('productAttributes.types.url', 'URL'),
      File: t('productAttributes.types.file', 'File'),
    }
    return typeLabels[type] || type
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[600px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>
            {isEditing
              ? t('productAttributes.editAttribute', 'Edit Product Attribute')
              : t('productAttributes.createAttribute', 'Create Product Attribute')}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? t('productAttributes.editAttributeDescription', 'Update the product attribute details below.')
              : t('productAttributes.createAttributeDescription', 'Fill in the details to create a new product attribute.')}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <Tabs defaultValue="basic" className="w-full">
              <TabsList className="grid w-full grid-cols-3">
                <TabsTrigger value="basic" className="cursor-pointer">{t('productAttributes.tabs.basic', 'Basic')}</TabsTrigger>
                <TabsTrigger value="display" className="cursor-pointer">{t('productAttributes.tabs.display', 'Display')}</TabsTrigger>
                <TabsTrigger value="validation" className="cursor-pointer">{t('productAttributes.tabs.validation', 'Validation')}</TabsTrigger>
              </TabsList>

              <TabsContent value="basic" className="space-y-4 mt-4">
                <FormField
                  control={form.control}
                  name="name"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('labels.name', 'Name')}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          onChange={handleNameChange}
                          placeholder={t('productAttributes.namePlaceholder', 'Enter attribute name')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="code"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('labels.code', 'Code')}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          placeholder={t('productAttributes.codePlaceholder', 'attribute_code')}
                          disabled={isEditing}
                        />
                      </FormControl>
                      <FormDescription>
                        {t('productAttributes.codeDescription', 'Unique identifier (auto-generated from name)')}
                      </FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="type"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('labels.type', 'Type')}</FormLabel>
                      <FormControl>
                        <Combobox
                          options={ATTRIBUTE_TYPES.map((type) => ({
                            value: type,
                            label: getTypeLabel(type),
                          }))}
                          value={field.value}
                          onValueChange={field.onChange}
                          placeholder={t('productAttributes.selectType', 'Select type')}
                          searchPlaceholder={t('labels.searchType', 'Search type...')}
                          emptyText={t('labels.noTypeFound', 'No type found')}
                          disabled={isEditing}
                          countLabel={t('labels.types', 'types')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="unit"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('productAttributes.unit', 'Unit')}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          value={field.value ?? ''}
                          placeholder={t('productAttributes.unitPlaceholder', 'e.g., kg, cm, ml')}
                        />
                      </FormControl>
                      <FormDescription>
                        {t('productAttributes.unitDescription', 'Unit of measurement (optional)')}
                      </FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="sortOrder"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('labels.sortOrder', 'Sort Order')}</FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          {...field}
                          onChange={(e) => field.onChange(parseInt(e.target.value) || 0)}
                          min={0}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="isActive"
                  render={({ field }) => (
                    <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                      <div className="space-y-0.5">
                        <FormLabel>{t('labels.active', 'Active')}</FormLabel>
                        <FormDescription className="text-xs">
                          {t('productAttributes.activeDescription', 'Show this attribute in product forms')}
                        </FormDescription>
                      </div>
                      <FormControl>
                        <Switch
                          checked={field.value}
                          onCheckedChange={field.onChange}
                          className="cursor-pointer"
                        />
                      </FormControl>
                    </FormItem>
                  )}
                />
              </TabsContent>

              <TabsContent value="display" className="space-y-4 mt-4">
                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="isFilterable"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                        <div className="space-y-0.5">
                          <FormLabel>{t('productAttributes.filterable', 'Filterable')}</FormLabel>
                          <FormDescription className="text-xs">
                            {t('productAttributes.filterableDescription', 'Show in product filters')}
                          </FormDescription>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="cursor-pointer"
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="isSearchable"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                        <div className="space-y-0.5">
                          <FormLabel>{t('productAttributes.searchable', 'Searchable')}</FormLabel>
                          <FormDescription className="text-xs">
                            {t('productAttributes.searchableDescription', 'Include in search index')}
                          </FormDescription>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="cursor-pointer"
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="isRequired"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                        <div className="space-y-0.5">
                          <FormLabel>{t('productAttributes.required', 'Required')}</FormLabel>
                          <FormDescription className="text-xs">
                            {t('productAttributes.requiredDescription', 'Must be filled for products')}
                          </FormDescription>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="cursor-pointer"
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="isVariantAttribute"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                        <div className="space-y-0.5">
                          <FormLabel>{t('productAttributes.variantAttribute', 'Variant Attribute')}</FormLabel>
                          <FormDescription className="text-xs">
                            {t('productAttributes.variantAttributeDescription', 'Used for product variants')}
                          </FormDescription>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="cursor-pointer"
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="showInProductCard"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                        <div className="space-y-0.5">
                          <FormLabel>{t('productAttributes.showInCard', 'Show in Card')}</FormLabel>
                          <FormDescription className="text-xs">
                            {t('productAttributes.showInCardDescription', 'Display on product card')}
                          </FormDescription>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="cursor-pointer"
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="showInSpecifications"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                        <div className="space-y-0.5">
                          <FormLabel>{t('productAttributes.showInSpecs', 'Show in Specs')}</FormLabel>
                          <FormDescription className="text-xs">
                            {t('productAttributes.showInSpecsDescription', 'Display in specifications')}
                          </FormDescription>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="cursor-pointer"
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="isGlobal"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                        <div className="space-y-0.5">
                          <FormLabel>{t('productAttributes.global', 'Global')}</FormLabel>
                          <FormDescription className="text-xs">
                            {t('productAttributes.globalDescription', 'Available for all categories')}
                          </FormDescription>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="cursor-pointer"
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                </div>

                <FormField
                  control={form.control}
                  name="placeholder"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('productAttributes.placeholder', 'Placeholder')}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          value={field.value ?? ''}
                          placeholder={t('productAttributes.placeholderPlaceholder', 'Enter placeholder text')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="helpText"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('productAttributes.helpText', 'Help Text')}</FormLabel>
                      <FormControl>
                        <Textarea
                          {...field}
                          value={field.value ?? ''}
                          placeholder={t('productAttributes.helpTextPlaceholder', 'Help text shown to users')}
                          rows={2}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </TabsContent>

              <TabsContent value="validation" className="space-y-4 mt-4">
                <FormField
                  control={form.control}
                  name="validationRegex"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('productAttributes.validationRegex', 'Validation Pattern')}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          value={field.value ?? ''}
                          placeholder={t('productAttributes.validationRegexPlaceholder', '^[A-Za-z0-9]+$')}
                        />
                      </FormControl>
                      <FormDescription>
                        {t('productAttributes.validationRegexDescription', 'Regular expression for validation (optional)')}
                      </FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="minValue"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('productAttributes.minValue', 'Min Value')}</FormLabel>
                        <FormControl>
                          <Input
                            type="number"
                            step="any"
                            {...field}
                            value={field.value ?? ''}
                            onChange={(e) => field.onChange(e.target.value ? parseFloat(e.target.value) : null)}
                          />
                        </FormControl>
                        <FormDescription className="text-xs">
                          {t('productAttributes.minValueDescription', 'For Number/Decimal types')}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="maxValue"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('productAttributes.maxValue', 'Max Value')}</FormLabel>
                        <FormControl>
                          <Input
                            type="number"
                            step="any"
                            {...field}
                            value={field.value ?? ''}
                            onChange={(e) => field.onChange(e.target.value ? parseFloat(e.target.value) : null)}
                          />
                        </FormControl>
                        <FormDescription className="text-xs">
                          {t('productAttributes.maxValueDescription', 'For Number/Decimal types')}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <FormField
                  control={form.control}
                  name="maxLength"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('productAttributes.maxLength', 'Max Length')}</FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          {...field}
                          value={field.value ?? ''}
                          onChange={(e) => field.onChange(e.target.value ? parseInt(e.target.value) : null)}
                          min={1}
                        />
                      </FormControl>
                      <FormDescription>
                        {t('productAttributes.maxLengthDescription', 'Maximum character length for text types')}
                      </FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="defaultValue"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('productAttributes.defaultValue', 'Default Value')}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          value={field.value ?? ''}
                          placeholder={t('productAttributes.defaultValuePlaceholder', 'Default value for new products')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </TabsContent>
            </Tabs>

            <DialogFooter className="pt-4">
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
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
