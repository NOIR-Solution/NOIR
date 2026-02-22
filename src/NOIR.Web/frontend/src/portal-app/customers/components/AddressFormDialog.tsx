import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, MapPin } from 'lucide-react'
import { toast } from 'sonner'
import {
  Button,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  Form,
  FormControl,
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
} from '@uikit'
import {
  useAddCustomerAddressMutation,
  useUpdateCustomerAddressMutation,
} from '@/portal-app/customers/queries'
import type { AddressType, CustomerAddressDto } from '@/types/customer'

const ADDRESS_TYPES: AddressType[] = ['Shipping', 'Billing', 'Both']

const createAddressSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    addressType: z.enum(['Shipping', 'Billing', 'Both'] as const, {
      message: t('validation.required'),
    }),
    fullName: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { count: 200 })),
    phone: z.string().min(1, t('validation.required')).max(20, t('validation.maxLength', { count: 20 })),
    addressLine1: z.string().min(1, t('validation.required')).max(500, t('validation.maxLength', { count: 500 })),
    addressLine2: z.string().max(500, t('validation.maxLength', { count: 500 })).optional().nullable(),
    ward: z.string().max(200, t('validation.maxLength', { count: 200 })).optional().nullable(),
    district: z.string().max(200, t('validation.maxLength', { count: 200 })).optional().nullable(),
    province: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { count: 200 })),
    postalCode: z.string().max(20, t('validation.maxLength', { count: 20 })).optional().nullable(),
    isDefault: z.boolean().default(false),
  })

type AddressFormData = z.infer<ReturnType<typeof createAddressSchema>>

interface AddressFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  customerId: string
  address?: CustomerAddressDto | null
  onSuccess?: () => void
}

export const AddressFormDialog = ({ open, onOpenChange, customerId, address, onSuccess }: AddressFormDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!address
  const addMutation = useAddCustomerAddressMutation()
  const updateMutation = useUpdateCustomerAddressMutation()

  const form = useForm<AddressFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createAddressSchema(t)) as unknown as Resolver<AddressFormData>,
    mode: 'onBlur',
    defaultValues: {
      addressType: 'Shipping' as AddressType,
      fullName: '',
      phone: '',
      addressLine1: '',
      addressLine2: '',
      ward: '',
      district: '',
      province: '',
      postalCode: '',
      isDefault: false,
    },
  })

  useEffect(() => {
    if (open) {
      if (address) {
        form.reset({
          addressType: address.addressType,
          fullName: address.fullName,
          phone: address.phone,
          addressLine1: address.addressLine1,
          addressLine2: address.addressLine2 ?? '',
          ward: address.ward ?? '',
          district: address.district ?? '',
          province: address.province,
          postalCode: address.postalCode ?? '',
          isDefault: address.isDefault,
        })
      } else {
        form.reset({
          addressType: 'Shipping',
          fullName: '',
          phone: '',
          addressLine1: '',
          addressLine2: '',
          ward: '',
          district: '',
          province: '',
          postalCode: '',
          isDefault: false,
        })
      }
    }
  }, [open, address, form])

  const onSubmit = async (data: AddressFormData) => {
    const cleanedData = {
      ...data,
      addressLine2: data.addressLine2 || null,
      ward: data.ward || null,
      district: data.district || null,
      postalCode: data.postalCode || null,
    }

    try {
      if (isEditing && address) {
        await updateMutation.mutateAsync({
          customerId,
          addressId: address.id,
          request: cleanedData,
        })
        toast.success(t('customers.addressUpdateSuccess', 'Address updated successfully'))
      } else {
        await addMutation.mutateAsync({
          customerId,
          request: cleanedData,
        })
        toast.success(t('customers.addressCreateSuccess', 'Address added successfully'))
      }
      onSuccess?.()
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : isEditing
        ? t('customers.addressUpdateError', 'Failed to update address')
        : t('customers.addressCreateError', 'Failed to add address')
      toast.error(message)
    }
  }

  const isSubmitting = addMutation.isPending || updateMutation.isPending

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[550px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <MapPin className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>
                {isEditing ? t('customers.editAddress', 'Edit Address') : t('customers.addAddress', 'Add Address')}
              </CredenzaTitle>
              <CredenzaDescription>
                {isEditing
                  ? t('customers.editAddressDescription', 'Update the address details below.')
                  : t('customers.addAddressDescription', 'Fill in the address details.')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <CredenzaBody className="space-y-4">
              <FormField
                control={form.control}
                name="addressType"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('customers.addressTypeLabel', 'Address Type')}</FormLabel>
                    <Select onValueChange={field.onChange} defaultValue={field.value}>
                      <FormControl>
                        <SelectTrigger className="cursor-pointer">
                          <SelectValue placeholder={t('customers.selectAddressType', 'Select type')} />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {ADDRESS_TYPES.map((type) => (
                          <SelectItem key={type} value={type} className="cursor-pointer">
                            {t(`customers.addressType.${type.toLowerCase()}`, type)}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="fullName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('customers.recipientName', 'Recipient Name')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder={t('customers.recipientNamePlaceholder', 'Full name')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="phone"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('labels.phone', 'Phone')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder={t('customers.phonePlaceholder', '+84 xxx xxx xxx')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="addressLine1"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('customers.addressLine1', 'Address Line 1')}</FormLabel>
                    <FormControl>
                      <Input {...field} placeholder={t('customers.addressLine1Placeholder', 'Street address')} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="addressLine2"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('customers.addressLine2', 'Address Line 2')}</FormLabel>
                    <FormControl>
                      <Input {...field} value={field.value ?? ''} placeholder={t('customers.addressLine2Placeholder', 'Apartment, suite, etc.')} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="grid grid-cols-3 gap-4">
                <FormField
                  control={form.control}
                  name="ward"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('customers.ward', 'Ward')}</FormLabel>
                      <FormControl>
                        <Input {...field} value={field.value ?? ''} placeholder={t('customers.wardPlaceholder', 'Ward')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="district"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('customers.district', 'District')}</FormLabel>
                      <FormControl>
                        <Input {...field} value={field.value ?? ''} placeholder={t('customers.districtPlaceholder', 'District')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="province"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('customers.province', 'Province')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder={t('customers.provincePlaceholder', 'Province/City')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="postalCode"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('customers.postalCode', 'Postal Code')}</FormLabel>
                    <FormControl>
                      <Input {...field} value={field.value ?? ''} placeholder={t('customers.postalCodePlaceholder', 'Postal code')} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="isDefault"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                    <div className="space-y-0.5">
                      <FormLabel>{t('customers.defaultAddress', 'Default Address')}</FormLabel>
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
                {isEditing ? t('labels.save', 'Save') : t('customers.addAddress', 'Add Address')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
