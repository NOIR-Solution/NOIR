import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
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
  Textarea,
} from '@uikit'
import {
  useCreateCustomerMutation,
  useUpdateCustomerMutation,
} from '@/portal-app/customers/queries'
import type { CustomerDto, CustomerSummaryDto } from '@/types/customer'

const createCustomerSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    firstName: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 })),
    lastName: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 })),
    email: z.string().min(1, t('validation.required')).email(t('validation.invalidEmail')),
    phone: z.string().max(20, t('validation.maxLength', { count: 20 })).optional().nullable(),
    tags: z.string().max(500, t('validation.maxLength', { count: 500 })).optional().nullable(),
    notes: z.string().max(2000, t('validation.maxLength', { count: 2000 })).optional().nullable(),
  })

type CustomerFormData = z.infer<ReturnType<typeof createCustomerSchema>>

interface CustomerFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  customer?: CustomerDto | CustomerSummaryDto | null
  onSuccess?: () => void
}

export const CustomerFormDialog = ({ open, onOpenChange, customer, onSuccess }: CustomerFormDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!customer
  const createMutation = useCreateCustomerMutation()
  const updateMutation = useUpdateCustomerMutation()

  // Check if we have the full DTO (with tags/notes) or summary
  const fullCustomer = customer && 'tags' in customer ? customer as CustomerDto : null

  const form = useForm<CustomerFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createCustomerSchema(t)) as unknown as Resolver<CustomerFormData>,
    mode: 'onBlur',
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      tags: '',
      notes: '',
    },
  })

  useEffect(() => {
    if (open) {
      if (customer) {
        form.reset({
          firstName: customer.firstName,
          lastName: customer.lastName,
          email: customer.email,
          phone: customer.phone ?? '',
          tags: fullCustomer?.tags ?? '',
          notes: fullCustomer?.notes ?? '',
        })
      } else {
        form.reset({
          firstName: '',
          lastName: '',
          email: '',
          phone: '',
          tags: '',
          notes: '',
        })
      }
    }
  }, [open, customer, fullCustomer, form])

  const onSubmit = async (data: CustomerFormData) => {
    const cleanedData = {
      ...data,
      phone: data.phone || null,
      tags: data.tags || null,
      notes: data.notes || null,
    }

    try {
      if (isEditing && customer) {
        await updateMutation.mutateAsync({
          id: customer.id,
          request: cleanedData,
        })
        toast.success(t('customers.updateSuccess', 'Customer updated successfully'))
      } else {
        await createMutation.mutateAsync(cleanedData)
        toast.success(t('customers.createSuccess', 'Customer created successfully'))
      }
      onSuccess?.()
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : isEditing
        ? t('customers.updateError', 'Failed to update customer')
        : t('customers.createError', 'Failed to create customer')
      toast.error(message)
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[500px]">
        <CredenzaHeader>
          <CredenzaTitle>
            {isEditing ? t('customers.editCustomer', 'Edit Customer') : t('customers.createCustomer', 'Create Customer')}
          </CredenzaTitle>
          <CredenzaDescription>
            {isEditing
              ? t('customers.editCustomerDescription', 'Update the customer details below.')
              : t('customers.createCustomerDescription', 'Fill in the details to create a new customer.')}
          </CredenzaDescription>
        </CredenzaHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <CredenzaBody>
              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="firstName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('customers.firstName', 'First Name')}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          placeholder={t('customers.firstNamePlaceholder', 'Enter first name')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="lastName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('customers.lastName', 'Last Name')}</FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          placeholder={t('customers.lastNamePlaceholder', 'Enter last name')}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('labels.email', 'Email')}</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        type="email"
                        placeholder={t('customers.emailPlaceholder', 'customer@example.com')}
                      />
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
                      <Input
                        {...field}
                        value={field.value ?? ''}
                        placeholder={t('customers.phonePlaceholder', '+84 xxx xxx xxx')}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="tags"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('labels.tags', 'Tags')}</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        value={field.value ?? ''}
                        placeholder={t('customers.tagsPlaceholder', 'tag1, tag2, tag3')}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="notes"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('labels.notes', 'Notes')}</FormLabel>
                    <FormControl>
                      <Textarea
                        {...field}
                        value={field.value ?? ''}
                        placeholder={t('customers.notesPlaceholder', 'Internal notes about this customer...')}
                        rows={3}
                      />
                    </FormControl>
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
