import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Shield } from 'lucide-react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
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
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { ColorPicker } from '@/components/ui/color-picker'
import { toast } from 'sonner'
import { updateRole, getRoles } from '@/services/roles'
import { ApiError } from '@/services/apiClient'
import type { RoleListItem } from '@/types'

const formSchema = z.object({
  name: z.string().min(2, 'Role name must be at least 2 characters').max(50, 'Role name cannot exceed 50 characters'),
  description: z.string().max(500, 'Description cannot exceed 500 characters').optional(),
  parentRoleId: z.string().optional(),
  color: z.string().optional(),
  iconName: z.string().optional(),
  sortOrder: z.number().optional(),
})

type FormValues = z.infer<typeof formSchema>

interface EditRoleDialogProps {
  role: RoleListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export function EditRoleDialog({ role, open, onOpenChange, onSuccess }: EditRoleDialogProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)
  const [existingRoles, setExistingRoles] = useState<RoleListItem[]>([])

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: '',
      description: '',
      parentRoleId: '',
      color: '#6b7280',
      iconName: '',
      sortOrder: 0,
    },
  })

  useEffect(() => {
    if (role) {
      form.reset({
        name: role.name,
        description: role.description || '',
        parentRoleId: role.parentRoleId || '',
        color: role.color || '#6b7280',
        iconName: role.iconName || '',
        sortOrder: role.sortOrder,
      })
    }
  }, [role, form])

  useEffect(() => {
    if (open) {
      // Fetch existing roles for parent selection (exclude current role)
      getRoles({ pageSize: 100 })
        .then(result => setExistingRoles(result.items.filter((r: RoleListItem) => r.id !== role?.id)))
        .catch(() => setExistingRoles([]))
    }
  }, [open, role?.id])

  const onSubmit = async (values: FormValues) => {
    if (!role) return

    setLoading(true)
    try {
      await updateRole({
        roleId: role.id,
        name: values.name,
        description: values.description || undefined,
        parentRoleId: values.parentRoleId || undefined,
        color: values.color || undefined,
        iconName: values.iconName || undefined,
        sortOrder: values.sortOrder,
      })

      toast.success(t('roles.updateSuccess', 'Role updated'))

      onOpenChange(false)
      onSuccess()
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : t('roles.updateError', 'Failed to update role')
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <Shield className="h-5 w-5 text-primary" />
            </div>
            <div>
              <DialogTitle>{t('roles.editTitle', 'Edit Role')}</DialogTitle>
              <DialogDescription>
                {t('roles.editDescription', 'Update role details and configuration.')}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('roles.fields.name', 'Role Name')}</FormLabel>
                  <FormControl>
                    <Input
                      placeholder={t('roles.fields.namePlaceholder', 'e.g., Editor, Viewer')}
                      disabled={role?.isSystemRole}
                      {...field}
                    />
                  </FormControl>
                  {role?.isSystemRole && (
                    <FormDescription>
                      {t('roles.systemRoleWarning', 'System role names cannot be changed.')}
                    </FormDescription>
                  )}
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('roles.fields.description', 'Description')}</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder={t('roles.fields.descriptionPlaceholder', 'Describe what this role can do...')}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="parentRoleId"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('roles.fields.parentRole', 'Parent Role')}</FormLabel>
                  <Select
                    onValueChange={(value) => field.onChange(value === '__none__' ? '' : value)}
                    value={field.value || '__none__'}
                  >
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder={t('roles.fields.parentRolePlaceholder', 'Select parent role (optional)')} />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="__none__">{t('roles.fields.noParent', 'No parent role')}</SelectItem>
                      {existingRoles.map((r) => (
                        <SelectItem key={r.id} value={r.id}>
                          {r.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormDescription>
                    {t('roles.fields.parentRoleDescription', 'Child roles inherit permissions from their parent.')}
                  </FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="color"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('roles.fields.color', 'Color')}</FormLabel>
                  <FormControl>
                    <ColorPicker
                      value={field.value}
                      onChange={field.onChange}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
                {t('buttons.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={loading}>
                {loading ? t('labels.saving', 'Saving...') : t('buttons.save', 'Save')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
