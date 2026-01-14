/**
 * Profile Form Component
 *
 * Main profile editing form with:
 * - Avatar upload/delete
 * - Personal info (first name, last name, display name)
 * - Email change (via dialog with OTP)
 * - Phone number
 */
import { useState, useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { User, Mail, Phone, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { ProfileAvatar } from './ProfileAvatar'
import { EmailChangeDialog } from './EmailChangeDialog'
import { useAuthContext } from '@/contexts/AuthContext'
import {
  updateProfile,
  uploadAvatar,
  deleteAvatar,
  ApiError,
} from '@/services/profile'
import { useValidatedForm } from '@/hooks/useValidatedForm'
import { updateUserProfileSchema } from '@/validation/schemas.generated'
import { createValidationTranslator } from '@/lib/validation-i18n'
import { z } from 'zod'

type ProfileFormData = z.infer<typeof updateUserProfileSchema>

export function ProfileForm() {
  const { t } = useTranslation('auth')
  const { t: tCommon } = useTranslation('common')
  const { user, refreshUser } = useAuthContext()

  // Memoized translation function for validation errors
  const translateError = useMemo(() => createValidationTranslator(tCommon), [tCommon])

  // Loading states for avatar
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false)
  const [isRemovingAvatar, setIsRemovingAvatar] = useState(false)

  // Use validated form with Zod schema
  const { form, handleSubmit, isSubmitting, serverError } = useValidatedForm<ProfileFormData>({
    schema: updateUserProfileSchema,
    defaultValues: {
      firstName: '',
      lastName: '',
      displayName: '',
      phoneNumber: '',
    },
    onSubmit: async (data) => {
      await updateProfile({
        firstName: data.firstName || null,
        lastName: data.lastName || null,
        displayName: data.displayName || null,
        phoneNumber: data.phoneNumber || null,
      })

      // Refresh user data
      await refreshUser()
      toast.success(t('profile.saved'))
    },
    onError: (error) => {
      if (!(error instanceof ApiError)) {
        toast.error(t('profile.saveFailed'))
      }
    },
  })

  // Initialize form with user data when user loads
  useEffect(() => {
    if (user) {
      form.reset({
        firstName: user.firstName ?? '',
        lastName: user.lastName ?? '',
        displayName: user.displayName ?? '',
        phoneNumber: user.phoneNumber ?? '',
      })
    }
  }, [user, form])

  const handleAvatarUpload = async (file: File) => {
    setIsUploadingAvatar(true)
    try {
      await uploadAvatar(file)
      await refreshUser()
      // Notify other components (like Sidebar) about avatar change
      window.dispatchEvent(new Event('avatar-updated'))
      toast.success(t('profile.avatar.uploadSuccess'))
    } catch (err) {
      if (err instanceof ApiError) {
        toast.error(err.message)
      } else {
        toast.error(t('profile.avatar.uploadFailed'))
      }
    } finally {
      setIsUploadingAvatar(false)
    }
  }

  const handleAvatarRemove = async () => {
    setIsRemovingAvatar(true)
    try {
      await deleteAvatar()
      await refreshUser()
      // Notify other components (like Sidebar) about avatar change
      window.dispatchEvent(new Event('avatar-updated'))
      toast.success(t('profile.avatar.deleteSuccess'))
    } catch (err) {
      if (err instanceof ApiError) {
        toast.error(err.message)
      } else {
        toast.error(t('profile.avatar.deleteFailed'))
      }
    } finally {
      setIsRemovingAvatar(false)
    }
  }

  const handleEmailChangeSuccess = async () => {
    await refreshUser()
    toast.success(t('profile.email.success'))
  }

  if (!user) {
    return null
  }

  return (
    <Card className="max-w-2xl">
      <CardHeader>
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-blue-600/10">
            <User className="h-5 w-5 text-blue-600" />
          </div>
          <div>
            <CardTitle>{t('profile.title')}</CardTitle>
            <CardDescription>{t('profile.description')}</CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-8">
          {/* Avatar Section */}
          <div className="flex flex-col items-center py-4 border-b border-border">
            <ProfileAvatar
              email={user.email}
              firstName={user.firstName}
              lastName={user.lastName}
              avatarUrl={user.avatarUrl}
              onUpload={handleAvatarUpload}
              onRemove={handleAvatarRemove}
              isUploading={isUploadingAvatar}
              isRemoving={isRemovingAvatar}
            />
          </div>

          {/* Personal Info Section */}
          <div className="space-y-6">
            {/* Name Fields - Side by Side */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="firstName">{t('profile.firstName')}</Label>
                <Input
                  id="firstName"
                  type="text"
                  {...form.register('firstName')}
                  placeholder={t('profile.firstNamePlaceholder')}
                  className="focus:border-blue-600 focus:ring-blue-600/20"
                  disabled={isSubmitting}
                  aria-invalid={!!form.formState.errors.firstName}
                />
                {form.formState.errors.firstName && (
                  <p className="text-sm text-destructive">{translateError(form.formState.errors.firstName.message)}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="lastName">{t('profile.lastName')}</Label>
                <Input
                  id="lastName"
                  type="text"
                  {...form.register('lastName')}
                  placeholder={t('profile.lastNamePlaceholder')}
                  className="focus:border-blue-600 focus:ring-blue-600/20"
                  disabled={isSubmitting}
                  aria-invalid={!!form.formState.errors.lastName}
                />
                {form.formState.errors.lastName && (
                  <p className="text-sm text-destructive">{translateError(form.formState.errors.lastName.message)}</p>
                )}
              </div>
            </div>

            {/* Display Name */}
            <div className="space-y-2">
              <Label htmlFor="displayName">{t('profile.displayName')}</Label>
              <Input
                id="displayName"
                type="text"
                {...form.register('displayName')}
                placeholder={t('profile.displayNamePlaceholder')}
                className="focus:border-blue-600 focus:ring-blue-600/20"
                disabled={isSubmitting}
                aria-invalid={!!form.formState.errors.displayName}
              />
              {form.formState.errors.displayName && (
                <p className="text-sm text-destructive">{translateError(form.formState.errors.displayName.message)}</p>
              )}
              <p className="text-xs text-muted-foreground">
                {t('profile.displayNameHelp')}
              </p>
            </div>

            {/* Email with Change Button */}
            <div className="space-y-2">
              <Label htmlFor="email">{t('profile.email.label')}</Label>
              <div className="flex gap-2">
                <div className="relative flex-1 group">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    id="email"
                    type="email"
                    value={user.email}
                    disabled
                    className="pl-10 bg-muted"
                  />
                </div>
                <EmailChangeDialog
                  currentEmail={user.email}
                  onSuccess={handleEmailChangeSuccess}
                />
              </div>
            </div>

            {/* Phone Number */}
            <div className="space-y-2">
              <Label htmlFor="phoneNumber">{t('profile.phone')}</Label>
              <div className="relative group">
                <Phone className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-blue-600 transition-colors" />
                <Input
                  id="phoneNumber"
                  type="tel"
                  {...form.register('phoneNumber')}
                  placeholder={t('profile.phonePlaceholder')}
                  className="pl-10 focus:border-blue-600 focus:ring-blue-600/20"
                  disabled={isSubmitting}
                  aria-invalid={!!form.formState.errors.phoneNumber}
                />
              </div>
              {form.formState.errors.phoneNumber && (
                <p className="text-sm text-destructive">{translateError(form.formState.errors.phoneNumber.message)}</p>
              )}
            </div>
          </div>

          {/* Error */}
          {serverError && (
            <div className="p-4 rounded-lg bg-destructive/10 border border-destructive/20">
              <p className="text-sm text-destructive font-medium">{serverError}</p>
            </div>
          )}

          {/* Submit */}
          <Button
            type="submit"
            disabled={isSubmitting || !form.formState.isDirty}
            className="w-full bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white shadow-lg disabled:opacity-50"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                {t('profile.saving')}
              </>
            ) : (
              t('profile.saveChanges')
            )}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
