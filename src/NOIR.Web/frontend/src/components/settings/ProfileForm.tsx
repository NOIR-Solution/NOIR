/**
 * Profile Form Component
 *
 * Main profile editing form with:
 * - Avatar upload/delete
 * - Personal info (first name, last name, display name)
 * - Email change (via dialog with OTP)
 * - Phone number
 */
import { useState, useEffect } from 'react'
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

export function ProfileForm() {
  const { t } = useTranslation('auth')
  const { user, checkAuth } = useAuthContext()

  // Form state
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')

  // Loading states
  const [isSaving, setIsSaving] = useState(false)
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false)
  const [isRemovingAvatar, setIsRemovingAvatar] = useState(false)

  const [error, setError] = useState('')
  const [hasChanges, setHasChanges] = useState(false)

  // Initialize form with user data
  useEffect(() => {
    if (user) {
      setFirstName(user.firstName ?? '')
      setLastName(user.lastName ?? '')
      setDisplayName(user.displayName ?? '')
      setPhoneNumber(user.phoneNumber ?? '')
    }
  }, [user])

  // Track changes
  useEffect(() => {
    if (!user) return

    const changed =
      firstName !== (user.firstName ?? '') ||
      lastName !== (user.lastName ?? '') ||
      displayName !== (user.displayName ?? '') ||
      phoneNumber !== (user.phoneNumber ?? '')

    setHasChanges(changed)
  }, [user, firstName, lastName, displayName, phoneNumber])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setIsSaving(true)

    try {
      await updateProfile({
        firstName: firstName || null,
        lastName: lastName || null,
        displayName: displayName || null,
        phoneNumber: phoneNumber || null,
      })

      // Refresh user data
      await checkAuth()
      toast.success(t('profile.saved'))
      setHasChanges(false)
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError(t('profile.saveFailed'))
      }
    } finally {
      setIsSaving(false)
    }
  }

  const handleAvatarUpload = async (file: File) => {
    setIsUploadingAvatar(true)
    try {
      await uploadAvatar(file)
      await checkAuth()
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
      await checkAuth()
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
    await checkAuth()
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
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder={t('profile.firstNamePlaceholder')}
                  className="focus:border-blue-600 focus:ring-blue-600/20"
                  disabled={isSaving}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="lastName">{t('profile.lastName')}</Label>
                <Input
                  id="lastName"
                  type="text"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder={t('profile.lastNamePlaceholder')}
                  className="focus:border-blue-600 focus:ring-blue-600/20"
                  disabled={isSaving}
                />
              </div>
            </div>

            {/* Display Name */}
            <div className="space-y-2">
              <Label htmlFor="displayName">{t('profile.displayName')}</Label>
              <Input
                id="displayName"
                type="text"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                placeholder={t('profile.displayNamePlaceholder')}
                className="focus:border-blue-600 focus:ring-blue-600/20"
                disabled={isSaving}
              />
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
                  value={phoneNumber}
                  onChange={(e) => setPhoneNumber(e.target.value)}
                  placeholder={t('profile.phonePlaceholder')}
                  className="pl-10 focus:border-blue-600 focus:ring-blue-600/20"
                  disabled={isSaving}
                />
              </div>
            </div>
          </div>

          {/* Error */}
          {error && (
            <div className="p-4 rounded-lg bg-destructive/10 border border-destructive/20">
              <p className="text-sm text-destructive font-medium">{error}</p>
            </div>
          )}

          {/* Submit */}
          <Button
            type="submit"
            disabled={isSaving || !hasChanges}
            className="w-full bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white shadow-lg disabled:opacity-50"
          >
            {isSaving ? (
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
