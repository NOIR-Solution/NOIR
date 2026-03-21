import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Save, RotateCcw } from 'lucide-react'
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  ColorPicker,
  ImageUploadField,
  Label,
  Skeleton,
  Switch,
} from '@uikit'

import { ApiError } from '@/services/apiClient'
import { useBranding } from '@/contexts/BrandingContext'
import {
  useBrandingSettingsQuery,
  useUpdateBrandingSettings,
} from '@/portal-app/settings/queries'

export interface BrandingSettingsTabProps {
  canEdit: boolean
}

export const BrandingSettingsTab = ({ canEdit }: BrandingSettingsTabProps) => {
  const { t } = useTranslation('common')
  const { reloadBranding } = useBranding()
  const { data, isLoading } = useBrandingSettingsQuery()
  const updateMutation = useUpdateBrandingSettings()

  // Form state
  const [logoUrl, setLogoUrl] = useState('')
  const [faviconUrl, setFaviconUrl] = useState('')
  const [primaryColor, setPrimaryColor] = useState('')
  const [secondaryColor, setSecondaryColor] = useState('')
  const [darkModeDefault, setDarkModeDefault] = useState(false)

  useEffect(() => {
    if (data) {
      setLogoUrl(data.logoUrl || '')
      setFaviconUrl(data.faviconUrl || '')
      setPrimaryColor(data.primaryColor || '')
      setSecondaryColor(data.secondaryColor || '')
      setDarkModeDefault(data.darkModeDefault)
    }
  }, [data])

  const handleSave = () => {
    updateMutation.mutate(
      {
        logoUrl: logoUrl.trim() || null,
        faviconUrl: faviconUrl.trim() || null,
        primaryColor: primaryColor.trim() || null,
        secondaryColor: secondaryColor.trim() || null,
        darkModeDefault,
      },
      {
        onSuccess: async () => {
          await reloadBranding()
          toast.success(t('tenantSettings.saved'))
        },
        onError: (error) => {
          if (error instanceof ApiError) {
            toast.error(error.message)
          } else {
            toast.error(t('messages.operationFailed'))
          }
        },
      },
    )
  }

  const hasChanges = data && (
    (logoUrl.trim() || '') !== (data.logoUrl || '') ||
    (faviconUrl.trim() || '') !== (data.faviconUrl || '') ||
    (primaryColor.trim() || '') !== (data.primaryColor || '') ||
    (secondaryColor.trim() || '') !== (data.secondaryColor || '') ||
    darkModeDefault !== data.darkModeDefault
  )

  if (isLoading) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardContent className="py-8">
          <div className="space-y-4">
            <Skeleton className="h-4 w-48" />
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-4 w-48" />
            <Skeleton className="h-24 w-full" />
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader>
        <CardTitle className="text-lg">{t('tenantSettings.branding.title')}</CardTitle>
        <CardDescription>{t('tenantSettings.branding.description')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="grid gap-6 sm:grid-cols-2">
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>{t('tenantSettings.branding.logoUrl')}</Label>
              {canEdit && logoUrl && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-6 px-2 text-xs text-muted-foreground cursor-pointer"
                  onClick={() => setLogoUrl('')}
                >
                  <RotateCcw className="h-3 w-3 mr-1" />
                  {t('buttons.resetToDefault', 'Reset to default')}
                </Button>
              )}
            </div>
            <ImageUploadField
              value={logoUrl}
              onChange={setLogoUrl}
              folder="branding"
              placeholder={t('tenantSettings.branding.clickToUpload')}
              hint={t('tenantSettings.branding.logoHint', 'PNG, SVG, WebP up to 2MB')}
              aspectClass="aspect-video"
              disabled={!canEdit}
            />
          </div>
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>{t('tenantSettings.branding.faviconUrl')}</Label>
              {canEdit && faviconUrl && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-6 px-2 text-xs text-muted-foreground cursor-pointer"
                  onClick={() => setFaviconUrl('')}
                >
                  <RotateCcw className="h-3 w-3 mr-1" />
                  {t('buttons.resetToDefault', 'Reset to default')}
                </Button>
              )}
            </div>
            <ImageUploadField
              value={faviconUrl}
              onChange={setFaviconUrl}
              folder="branding"
              placeholder={t('tenantSettings.branding.upload')}
              hint={t('tenantSettings.branding.faviconHint', 'ICO, PNG, SVG up to 2MB')}
              aspectClass="aspect-video"
              accept="image/jpeg,image/png,image/gif,image/webp,image/x-icon,image/svg+xml"
              disabled={!canEdit}
            />
          </div>
        </div>

        <div className="grid gap-6 sm:grid-cols-2">
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>{t('tenantSettings.branding.primaryColor')}</Label>
              {canEdit && primaryColor && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-6 px-2 text-xs text-muted-foreground cursor-pointer"
                  onClick={() => setPrimaryColor('')}
                >
                  <RotateCcw className="h-3 w-3 mr-1" />
                  {t('buttons.resetToDefault', 'Reset to default')}
                </Button>
              )}
            </div>
            <ColorPicker
              value={primaryColor || '#3B82F6'}
              onChange={setPrimaryColor}
              showCustomInput={canEdit}
            />
          </div>
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>{t('tenantSettings.branding.secondaryColor')}</Label>
              {canEdit && secondaryColor && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="h-6 px-2 text-xs text-muted-foreground cursor-pointer"
                  onClick={() => setSecondaryColor('')}
                >
                  <RotateCcw className="h-3 w-3 mr-1" />
                  {t('buttons.resetToDefault', 'Reset to default')}
                </Button>
              )}
            </div>
            <ColorPicker
              value={secondaryColor || '#2563EB'}
              onChange={setSecondaryColor}
              showCustomInput={canEdit}
            />
          </div>
        </div>

        <div className="flex items-center justify-between rounded-lg border p-4">
          <div className="space-y-0.5">
            <Label>{t('tenantSettings.branding.darkModeDefault')}</Label>
            <p className="text-sm text-muted-foreground">
              {t('tenantSettings.branding.darkModeDescription')}
            </p>
          </div>
          <Switch
            checked={darkModeDefault}
            onCheckedChange={setDarkModeDefault}
            disabled={!canEdit}
            className="cursor-pointer"
            aria-label={t('tenantSettings.branding.darkModeDefault')}
          />
        </div>

        {canEdit && (
          <div className="flex justify-end">
            <Button onClick={handleSave} disabled={updateMutation.isPending || !hasChanges}>
              <Save className="h-4 w-4 mr-2" />
              {updateMutation.isPending ? t('buttons.saving') : t('buttons.save')}
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
