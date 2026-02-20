import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Loader2, Plus, Shield } from 'lucide-react'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import {
  Button,
  ColorPicker,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
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

import { toast } from 'sonner'
import { createRole, getRoles } from '@/services/roles'
import { ApiError } from '@/services/apiClient'
import type { RoleListItem } from '@/types'
import { useEffect } from 'react'

const createFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(2, t('validation.minLength', { count: 2 })).max(50, t('validation.maxLength', { count: 50 })),
    description: z.string().max(500, t('validation.maxLength', { count: 500 })).optional(),
    parentRoleId: z.string().optional(),
    color: z.string().optional(),
    iconName: z.string().optional(),
  })

type FormValues = z.infer<ReturnType<typeof createFormSchema>>

interface CreateRoleDialogProps {
  onSuccess: () => void
}

export const CreateRoleDialog = ({ onSuccess }: CreateRoleDialogProps) => {
  const { t } = useTranslation('common')
  const [open, setOpen] = useState(false)
  const [loading, setLoading] = useState(false)
  const [existingRoles, setExistingRoles] = useState<RoleListItem[]>([])

  const form = useForm<FormValues>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createFormSchema(t)) as unknown as Resolver<FormValues>,
    mode: 'onBlur',
    defaultValues: {
      name: '',
      description: '',
      parentRoleId: '',
      color: '#6b7280',
      iconName: '',
    },
  })

  useEffect(() => {
    if (open) {
      // Fetch existing roles for parent selection
      getRoles({ pageSize: 100 })
        .then(result => setExistingRoles(result.items))
        .catch(() => setExistingRoles([]))
    }
  }, [open])

  const onSubmit = async (values: FormValues) => {
    setLoading(true)
    try {
      await createRole({
        name: values.name,
        description: values.description || undefined,
        parentRoleId: values.parentRoleId || undefined,
        color: values.color || undefined,
        iconName: values.iconName || undefined,
      })

      toast.success(t('roles.createSuccess', 'Role created'))

      form.reset()
      setOpen(false)
      onSuccess()
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : t('roles.createError', 'Failed to create role')
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button className="group shadow-lg hover:shadow-xl transition-all duration-300">
          <Plus className="mr-2 h-4 w-4 transition-transform group-hover:rotate-90 duration-300" />
          {t('roles.create', 'Create Role')}
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <Shield className="h-5 w-5 text-primary" />
            </div>
            <div>
              <DialogTitle>{t('roles.createTitle', 'Create New Role')}</DialogTitle>
              <DialogDescription>
                {t('roles.createDescription', 'Add a new role with custom permissions.')}
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
                    <Input placeholder={t('roles.fields.namePlaceholder', 'e.g., Editor, Viewer')} {...field} />
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
                      {existingRoles.map((role) => (
                        <SelectItem key={role.id} value={role.id}>
                          {role.name}
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
              <Button type="button" variant="outline" onClick={() => setOpen(false)} className="cursor-pointer">
                {t('buttons.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={loading} className="cursor-pointer">
                {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {loading ? t('labels.creating', 'Creating...') : t('buttons.create', 'Create')}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
