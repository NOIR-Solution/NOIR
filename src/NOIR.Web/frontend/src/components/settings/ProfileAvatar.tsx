/**
 * Profile Avatar Component
 *
 * Displays user avatar with upload/remove functionality.
 * Shows: Custom avatar > Gravatar > Initials fallback
 */
import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Camera, Trash2, Loader2 } from 'lucide-react'
import { Button } from '@uikit'
import { useImageUpload } from '@/hooks/useImageUpload'
import { getGravatarUrl, getInitials, getAvatarColor } from '@/lib/gravatar'

interface ProfileAvatarProps {
  email: string
  firstName: string | null
  lastName: string | null
  avatarUrl: string | null
  onUpload: (file: File) => Promise<void>
  onRemove: () => Promise<void>
  isUploading?: boolean
  isRemoving?: boolean
}

export function ProfileAvatar({
  email,
  firstName,
  lastName,
  avatarUrl,
  onUpload,
  onRemove,
  isUploading = false,
  isRemoving = false,
}: ProfileAvatarProps) {
  const { t } = useTranslation('auth')
  const [gravatarUrl, setGravatarUrl] = useState<string | null>(null)
  const [gravatarFailed, setGravatarFailed] = useState(false)

  const {
    previewUrl,
    selectedFile,
    fileInputRef,
    openFilePicker,
    handleFileChange,
    clearSelection,
    setExistingUrl,
    error: uploadError,
    hasChanges,
  } = useImageUpload()

  // Set existing avatar URL when component mounts or avatarUrl changes
  useEffect(() => {
    setExistingUrl(avatarUrl)
  }, [avatarUrl, setExistingUrl])

  // Load Gravatar URL and reset state when email changes
  useEffect(() => {
    if (!avatarUrl && email) {
      setGravatarFailed(false) // Reset for new email
      getGravatarUrl(email, 160).then(setGravatarUrl)
    }
  }, [email, avatarUrl])

  // Handle Gravatar load error
  const handleGravatarError = () => {
    setGravatarFailed(true)
  }

  // Determine what to show
  const initials = getInitials(firstName, lastName, email)
  const avatarColor = getAvatarColor(email)

  // Priority: preview (local selection) > custom avatar > gravatar > initials
  const showPreview = previewUrl && hasChanges
  const showCustomAvatar = !showPreview && avatarUrl
  const showGravatar = !showPreview && !avatarUrl && gravatarUrl && !gravatarFailed
  const showInitials = !showPreview && !avatarUrl && (!gravatarUrl || gravatarFailed)

  const handleUploadClick = async () => {
    if (selectedFile) {
      await onUpload(selectedFile)
      clearSelection()
    }
  }

  const handleRemoveClick = async () => {
    await onRemove()
  }

  const isLoading = isUploading || isRemoving

  return (
    <div className="flex flex-col items-center gap-4">
      {/* Avatar Display */}
      <div className="relative group">
        <div className="w-24 h-24 rounded-full overflow-hidden ring-4 ring-background shadow-xl">
          {showPreview && previewUrl && (
            <img
              src={previewUrl}
              alt={t('profile.avatar.preview')}
              className="w-full h-full object-cover"
              onError={(e) => {
                // Fallback: try to re-read the file if preview fails
                if (selectedFile) {
                  const newUrl = URL.createObjectURL(selectedFile)
                  e.currentTarget.src = newUrl
                }
              }}
            />
          )}
          {showCustomAvatar && (
            <img
              src={avatarUrl!}
              alt={t('profile.avatar.title')}
              className="w-full h-full object-cover"
            />
          )}
          {showGravatar && (
            <img
              src={gravatarUrl!}
              alt={t('profile.avatar.title')}
              className="w-full h-full object-cover"
              onError={handleGravatarError}
            />
          )}
          {showInitials && (
            <div
              className="w-full h-full flex items-center justify-center text-white text-2xl font-semibold"
              style={{ backgroundColor: avatarColor }}
            >
              {initials}
            </div>
          )}
        </div>

        {/* Upload Overlay Button */}
        <button
          type="button"
          onClick={openFilePicker}
          disabled={isLoading}
          className="absolute inset-0 rounded-full bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center cursor-pointer disabled:cursor-not-allowed"
          aria-label={t('profile.avatar.upload')}
        >
          {isLoading ? (
            <Loader2 className="h-6 w-6 text-white animate-spin" />
          ) : (
            <Camera className="h-6 w-6 text-white" />
          )}
        </button>
      </div>

      {/* Hidden File Input */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/jpeg,image/png,image/gif,image/webp"
        onChange={handleFileChange}
        className="hidden"
        aria-hidden="true"
      />

      {/* Error Message */}
      {uploadError && (
        <p className="text-sm font-medium text-destructive">{uploadError}</p>
      )}

      {/* Action Buttons */}
      <div className="flex gap-2">
        {hasChanges ? (
          <>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={clearSelection}
              disabled={isLoading}
            >
              {t('common.cancel')}
            </Button>
            <Button
              type="button"
              size="sm"
              onClick={handleUploadClick}
              disabled={isLoading}
              className="bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white"
            >
              {isUploading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  {t('profile.avatar.uploading')}
                </>
              ) : (
                t('profile.avatar.upload')
              )}
            </Button>
          </>
        ) : (
          <>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={openFilePicker}
              disabled={isLoading}
            >
              <Camera className="mr-2 h-4 w-4" />
              {t('profile.avatar.change')}
            </Button>
            {avatarUrl && (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={handleRemoveClick}
                disabled={isLoading}
                className="text-destructive hover:text-destructive hover:bg-destructive/10"
              >
                {isRemoving ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Trash2 className="mr-2 h-4 w-4" />
                )}
                {t('profile.avatar.remove')}
              </Button>
            )}
          </>
        )}
      </div>

      {/* Help Text */}
      <p className="text-xs text-muted-foreground">
        {t('profile.avatar.maxSize')}
      </p>
    </div>
  )
}
