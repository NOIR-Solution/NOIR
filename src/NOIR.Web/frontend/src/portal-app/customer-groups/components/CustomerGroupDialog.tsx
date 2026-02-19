import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Switch,
  Textarea,
} from '@uikit'

import { useCreateCustomerGroupMutation, useUpdateCustomerGroupMutation } from '@/portal-app/customer-groups/queries'
import type { CustomerGroupListItem } from '@/types/customerGroup'
import { toast } from 'sonner'
import { Loader2 } from 'lucide-react'

const createCustomerGroupSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { count: 200 })),
    description: z.string().max(1000, t('validation.maxLength', { count: 1000 })).optional().nullable(),
    isActive: z.boolean().default(true),
  })

type CustomerGroupFormData = z.infer<ReturnType<typeof createCustomerGroupSchema>>

interface CustomerGroupDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  group?: CustomerGroupListItem | null
  onSuccess?: () => void
}

export const CustomerGroupDialog = ({ open, onOpenChange, group, onSuccess }: CustomerGroupDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!group
  const createMutation = useCreateCustomerGroupMutation()
  const updateMutation = useUpdateCustomerGroupMutation()

  const form = useForm<CustomerGroupFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createCustomerGroupSchema(t)) as unknown as Resolver<CustomerGroupFormData>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      description: '',
      isActive: true,
    },
  })

  // Reset form when dialog opens/closes or group changes
  useEffect(() => {
    if (open) {
      if (group) {
        form.reset({
          name: group.name,
          description: group.description ?? '',
          isActive: group.isActive,
        })
      } else {
        form.reset({
          name: '',
          description: '',
          isActive: true,
        })
      }
    }
  }, [open, group, form])

  const onSubmit = async (data: CustomerGroupFormData) => {
    try {
      if (isEditing && group) {
        await updateMutation.mutateAsync({
          id: group.id,
          request: {
            name: data.name,
            description: data.description || null,
            isActive: data.isActive,
          },
        })
        toast.success(t('customerGroups.updateSuccess', 'Customer group updated successfully'))
      } else {
        await createMutation.mutateAsync({
          name: data.name,
          description: data.description || null,
        })
        toast.success(t('customerGroups.createSuccess', 'Customer group created successfully'))
      }
      onSuccess?.()
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : isEditing
        ? t('customerGroups.updateError', 'Failed to update customer group')
        : t('customerGroups.createError', 'Failed to create customer group')
      toast.error(message)
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? t('customerGroups.editGroup', 'Edit Customer Group') : t('customerGroups.createGroup', 'Create Customer Group')}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? t('customerGroups.editGroupDescription', 'Update the customer group details below.')
              : t('customerGroups.createGroupDescription', 'Fill in the details to create a new customer group.')}
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('labels.name', 'Name')}</FormLabel>
                  <FormControl>
                    <Input
                      {...field}
                      placeholder={t('customerGroups.namePlaceholder', 'Enter group name')}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

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
                      placeholder={t('customerGroups.descriptionPlaceholder', 'Brief description of the customer group')}
                      rows={3}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            {isEditing && (
              <FormField
                control={form.control}
                name="isActive"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3">
                    <div className="space-y-0.5">
                      <FormLabel>{t('labels.active', 'Active')}</FormLabel>
                      <FormDescription className="text-xs">
                        {t('customerGroups.activeDescription', 'Active groups can be used for customer segmentation')}
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
            )}

            <DialogFooter>
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
