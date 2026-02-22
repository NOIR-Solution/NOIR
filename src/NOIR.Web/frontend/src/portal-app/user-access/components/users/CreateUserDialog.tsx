import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import { Loader2, Eye, EyeOff, UserPlus, Check } from 'lucide-react'
import {
  Badge,
  Button,
  Checkbox,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  Input,
  Label,
} from '@uikit'

import { toast } from 'sonner'
import { createUser } from '@/services/users'
import { useAvailableRolesQuery } from '@/portal-app/user-access/queries'
import { RolePermissionInfo } from './RolePermissionInfo'
import { ApiError } from '@/services/apiClient'

/**
 * Creates validation schema for CreateUserCommand
 * Matches backend FluentValidation rules
 * Uses i18n for localized error messages
 */
const createUserSchemaFactory = (t: (key: string, options?: Record<string, unknown>) => string) => z.object({
  email: z
    .string()
    .min(1, { message: t('validation.emailRequired') })
    .max(256, { message: t('validation.maxLength', { count: 256 }) })
    .email({ message: t('validation.pleaseEnterValidEmail') }),
  password: z
    .string()
    .min(1, { message: t('validation.passwordRequired') })
    .min(6, { message: t('validation.passwordMinLength', { count: 6 }) })
    .max(100, { message: t('validation.passwordMaxLength', { count: 100 }) }),
  confirmPassword: z
    .string()
    .min(1, { message: t('validation.passwordConfirm') }),
  firstName: z
    .string()
    .max(50, { message: t('validation.maxLength', { count: 50 }) })
    .optional()
    .or(z.literal('')),
  lastName: z
    .string()
    .max(50, { message: t('validation.maxLength', { count: 50 }) })
    .optional()
    .or(z.literal('')),
  displayName: z
    .string()
    .max(100, { message: t('validation.maxLength', { count: 100 }) })
    .optional()
    .or(z.literal('')),
}).refine((data) => data.password === data.confirmPassword, {
  message: t('validation.passwordMismatch'),
  path: ['confirmPassword'],
})

type FormData = z.infer<ReturnType<typeof createUserSchemaFactory>>

interface CreateUserDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export const CreateUserDialog = ({ open, onOpenChange, onSuccess }: CreateUserDialogProps) => {
  const { t } = useTranslation('common')
  const { t: tAuth } = useTranslation('auth')
  const { data: availableRoles = [], isLoading: loadingRoles } = useAvailableRolesQuery()
  const [loading, setLoading] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [selectedRoles, setSelectedRoles] = useState<Set<string>>(new Set())
  const [permissionsCache] = useState(() => new Map<string, string[]>())

  // Callback to cache loaded permissions
  const handlePermissionsLoaded = useCallback((roleId: string, permissions: string[]) => {
    permissionsCache.set(roleId, permissions)
  }, [permissionsCache])

  const { register, handleSubmit, formState: { errors }, reset } = useForm<FormData>({
    mode: 'onBlur',
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createUserSchemaFactory(t)) as unknown as Resolver<FormData>,
    defaultValues: {
      email: '',
      password: '',
      confirmPassword: '',
      firstName: '',
      lastName: '',
      displayName: '',
    },
  })

  // Reset form when dialog closes
  useEffect(() => {
    if (!open) {
      reset()
      setSelectedRoles(new Set())
      setShowPassword(false)
    }
  }, [open, reset])

  const handleToggleRole = (roleName: string) => {
    setSelectedRoles((prev) => {
      const next = new Set(prev)
      if (next.has(roleName)) {
        next.delete(roleName)
      } else {
        next.add(roleName)
      }
      return next
    })
  }

  const onSubmit = async (data: FormData) => {
    setLoading(true)
    try {
      await createUser({
        email: data.email.trim(),
        password: data.password,
        firstName: data.firstName?.trim() || null,
        lastName: data.lastName?.trim() || null,
        displayName: data.displayName?.trim() || null,
        roleNames: selectedRoles.size > 0 ? Array.from(selectedRoles) : null,
      })
      toast.success(t('users.createSuccess', 'User created successfully'))
      onOpenChange(false)
      onSuccess()
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : t('users.createError', 'Failed to create user')
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[550px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg shadow-sm">
              <UserPlus className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>{t('users.createTitle', 'Create User')}</CredenzaTitle>
              <CredenzaDescription>
                {t('users.createDescription', 'Add a new user to the system')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
          <CredenzaBody>
            <div className="grid gap-4">
              {/* Email */}
              <div className="grid gap-2">
                <Label htmlFor="email" className={errors.email ? 'text-destructive' : ''}>{t('labels.email', 'Email')} *</Label>
                <Input
                  id="email"
                  type="text"
                  {...register('email')}
                  placeholder={t('users.form.emailPlaceholder', 'user@example.com')}
                  autoComplete="off"
                  className={errors.email ? 'border-destructive' : ''}
                />
                {errors.email && (
                  <p className="text-sm font-medium text-destructive">{errors.email.message}</p>
                )}
              </div>

              {/* Password */}
              <div className="grid gap-2">
                <Label htmlFor="password" className={errors.password ? 'text-destructive' : ''}>{t('labels.password', 'Password')} *</Label>
                <div className="relative">
                  <Input
                    id="password"
                    type={showPassword ? 'text' : 'password'}
                    {...register('password')}
                    placeholder={t('users.form.passwordPlaceholder', 'Enter password')}
                    autoComplete="new-password"
                    className={errors.password ? 'border-destructive' : ''}
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                    onClick={() => setShowPassword(!showPassword)}
                    aria-label={showPassword ? tAuth('login.hidePassword') : tAuth('login.showPassword')}
                  >
                    {showPassword ? (
                      <EyeOff className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <Eye className="h-4 w-4 text-muted-foreground" />
                    )}
                  </Button>
                </div>
                {errors.password && (
                  <p className="text-sm font-medium text-destructive">{errors.password.message}</p>
                )}
              </div>

              {/* Confirm Password */}
              <div className="grid gap-2">
                <Label htmlFor="confirmPassword" className={errors.confirmPassword ? 'text-destructive' : ''}>{t('labels.confirmPassword', 'Confirm Password')} *</Label>
                <Input
                  id="confirmPassword"
                  type={showPassword ? 'text' : 'password'}
                  {...register('confirmPassword')}
                  placeholder={t('users.form.confirmPasswordPlaceholder', 'Confirm password')}
                  autoComplete="new-password"
                  className={errors.confirmPassword ? 'border-destructive' : ''}
                />
                {errors.confirmPassword && (
                  <p className="text-sm font-medium text-destructive">{errors.confirmPassword.message}</p>
                )}
              </div>

              {/* First Name & Last Name */}
              <div className="grid grid-cols-2 gap-4">
                <div className="grid gap-2">
                  <Label htmlFor="firstName" className={errors.firstName ? 'text-destructive' : ''}>{t('users.form.firstName', 'First Name')}</Label>
                  <Input
                    id="firstName"
                    {...register('firstName')}
                    placeholder={t('users.form.firstNamePlaceholder', 'John')}
                    className={errors.firstName ? 'border-destructive' : ''}
                  />
                  {errors.firstName && (
                    <p className="text-sm font-medium text-destructive">{errors.firstName.message}</p>
                  )}
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="lastName" className={errors.lastName ? 'text-destructive' : ''}>{t('users.form.lastName', 'Last Name')}</Label>
                  <Input
                    id="lastName"
                    {...register('lastName')}
                    placeholder={t('users.form.lastNamePlaceholder', 'Doe')}
                    className={errors.lastName ? 'border-destructive' : ''}
                  />
                  {errors.lastName && (
                    <p className="text-sm font-medium text-destructive">{errors.lastName.message}</p>
                  )}
                </div>
              </div>

              {/* Display Name */}
              <div className="grid gap-2">
                <Label htmlFor="displayName" className={errors.displayName ? 'text-destructive' : ''}>{t('users.form.displayName', 'Display Name')}</Label>
                <Input
                  id="displayName"
                  {...register('displayName')}
                  placeholder={t('users.form.displayNamePlaceholder', 'John Doe')}
                  className={errors.displayName ? 'border-destructive' : ''}
                />
                <p className="text-xs text-muted-foreground">
                  {t('users.form.displayNameHint', 'Optional. Overrides first/last name display.')}
                </p>
                {errors.displayName && (
                  <p className="text-sm font-medium text-destructive">{errors.displayName.message}</p>
                )}
              </div>

              {/* Roles Section */}
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <Label>{t('users.form.roles', 'Roles')}</Label>
                  <span className="text-xs text-muted-foreground">
                    {t('users.rolesSelected', '{{count}} roles selected', {
                      count: selectedRoles.size,
                    })}
                  </span>
                </div>

                {loadingRoles ? (
                  <div className="flex items-center justify-center py-4 border rounded-md">
                    <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
                  </div>
                ) : availableRoles.length === 0 ? (
                  <div className="text-center py-4 border rounded-md text-muted-foreground text-sm">
                    {t('users.noRolesAvailable', 'No roles available')}
                  </div>
                ) : (
                  <div className="border rounded-md p-3 space-y-2 max-h-[150px] overflow-y-auto">
                    {availableRoles.map((role) => (
                      <label
                        key={role.id}
                        htmlFor={`create-role-${role.id}`}
                        className="flex items-center space-x-3 rounded-lg p-2 hover:bg-accent/50 transition-colors cursor-pointer"
                      >
                        <Checkbox
                          id={`create-role-${role.id}`}
                          checked={selectedRoles.has(role.name)}
                          onCheckedChange={(checked) => {
                            if (checked !== 'indeterminate') {
                              handleToggleRole(role.name)
                            }
                          }}
                          className="cursor-pointer"
                        />
                        <div className="flex-1 flex items-center gap-2">
                          <span className="font-medium text-sm">
                            {role.name}
                          </span>
                          {role.isSystemRole && (
                            <Badge variant="secondary" className="text-xs">
                              {t('roles.system', 'System')}
                            </Badge>
                          )}
                          <RolePermissionInfo
                            role={role}
                            permissionsCache={permissionsCache}
                            onPermissionsLoaded={handlePermissionsLoaded}
                          />
                          {selectedRoles.has(role.name) && (
                            <Check className="h-4 w-4 text-green-600 ml-auto" />
                          )}
                        </div>
                      </label>
                    ))}
                  </div>
                )}
                <p className="text-xs text-muted-foreground">
                  {t('users.form.rolesHint', 'Select roles to assign to this user. You can also assign roles later.')}
                </p>
              </div>
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
              {t('buttons.create', 'Create')}
            </Button>
          </CredenzaFooter>
        </form>
      </CredenzaContent>
    </Credenza>
  )
}
