import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Loader2, UserCog } from 'lucide-react'
import {
  Button,
  Checkbox,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Label,
} from '@uikit'

import { toast } from 'sonner'
import { getUserById, updateUser } from '@/services/users'
import type { UserListItem, UserProfile } from '@/types'

const createFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    firstName: z.string().max(100, t('validation.maxLength', { count: 100 })).optional(),
    lastName: z.string().max(100, t('validation.maxLength', { count: 100 })).optional(),
    displayName: z.string().max(200, t('validation.maxLength', { count: 200 })).optional(),
    lockoutEnabled: z.boolean().default(false),
  })

type FormData = z.infer<ReturnType<typeof createFormSchema>>

interface EditUserDialogProps {
  user: UserListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export const EditUserDialog = ({ user, open, onOpenChange, onSuccess }: EditUserDialogProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)
  const [loadingProfile, setLoadingProfile] = useState(false)
  const [profile, setProfile] = useState<UserProfile | null>(null)

  const form = useForm<FormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createFormSchema(t)) as unknown as Resolver<FormData>,
    mode: 'onBlur',
    defaultValues: {
      firstName: '',
      lastName: '',
      displayName: '',
      lockoutEnabled: false,
    },
  })

  // Load full user profile when dialog opens
  useEffect(() => {
    if (user && open) {
      setLoadingProfile(true)
      getUserById(user.id)
        .then((data) => {
          setProfile(data)
          form.reset({
            firstName: data.firstName || '',
            lastName: data.lastName || '',
            displayName: data.displayName || '',
            lockoutEnabled: !data.isActive,
          })
        })
        .catch(() => {
          toast.error(t('users.loadError', 'Failed to load user details'))
        })
        .finally(() => {
          setLoadingProfile(false)
        })
    }
  }, [user, open, t, form])

  const onSubmit = async (values: FormData) => {
    if (!user) return

    setLoading(true)
    try {
      await updateUser({
        userId: user.id,
        firstName: values.firstName?.trim() || null,
        lastName: values.lastName?.trim() || null,
        displayName: values.displayName?.trim() ?? '', // Send empty string to clear, non-empty to update
        lockoutEnabled: values.lockoutEnabled,
      })
      toast.success(t('messages.updateSuccess', 'Updated successfully'))
      onSuccess()
      onOpenChange(false)
    } catch {
      toast.error(t('messages.operationFailed', 'Operation failed. Please try again.'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[500px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <UserCog className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>{t('users.editTitle', 'Edit User')}</CredenzaTitle>
              <CredenzaDescription>
                {t('users.editDescription', 'Update user details and account status')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        {loadingProfile ? (
          <CredenzaBody>
            <div className="flex items-center justify-center py-8">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          </CredenzaBody>
        ) : (
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)}>
              <CredenzaBody>
                <div className="grid gap-4 py-4">
                  <div className="grid gap-2">
                    <Label htmlFor="email">{t('labels.email', 'Email')}</Label>
                    <Input
                      id="email"
                      value={profile?.email || user?.email || ''}
                      disabled
                      className="bg-muted"
                    />
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <FormField
                      control={form.control}
                      name="firstName"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>{t('users.form.firstName', 'First Name')}</FormLabel>
                          <FormControl>
                            <Input
                              placeholder={t('users.form.firstNamePlaceholder', 'John')}
                              {...field}
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
                          <FormLabel>{t('users.form.lastName', 'Last Name')}</FormLabel>
                          <FormControl>
                            <Input
                              placeholder={t('users.form.lastNamePlaceholder', 'Doe')}
                              {...field}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  <FormField
                    control={form.control}
                    name="displayName"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('users.form.displayName', 'Display Name')}</FormLabel>
                        <FormControl>
                          <Input
                            placeholder={t('users.form.displayNamePlaceholder', 'John Doe')}
                            {...field}
                          />
                        </FormControl>
                        <FormDescription>
                          {t('users.form.displayNameHint', 'Optional. Overrides first/last name display.')}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="lockoutEnabled"
                    render={({ field }) => (
                      <FormItem className="flex items-start space-x-3 rounded-lg border p-3">
                        <FormControl>
                          <Checkbox
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="mt-0.5"
                          />
                        </FormControl>
                        <div className="space-y-0.5">
                          <FormLabel className="cursor-pointer">
                            {t('users.form.lockAccount', 'Lock Account')}
                          </FormLabel>
                          <FormDescription>
                            {t('users.form.lockAccountHint', 'Prevent user from signing in')}
                          </FormDescription>
                        </div>
                      </FormItem>
                    )}
                  />
                </div>
              </CredenzaBody>

              <CredenzaFooter>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => onOpenChange(false)}
                  disabled={loading}
                  className="cursor-pointer"
                >
                  {t('buttons.cancel', 'Cancel')}
                </Button>
                <Button type="submit" disabled={loading} className="cursor-pointer">
                  {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  {t('buttons.save', 'Save')}
                </Button>
              </CredenzaFooter>
            </form>
          </Form>
        )}
      </CredenzaContent>
    </Credenza>
  )
}
