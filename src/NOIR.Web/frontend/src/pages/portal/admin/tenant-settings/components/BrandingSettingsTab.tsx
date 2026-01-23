import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Save } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { ColorPicker } from '@/components/ui/color-picker'
import { ApiError } from '@/services/apiClient'
import {
  getBrandingSettings,
  updateBrandingSettings,
  type BrandingSettingsDto,
} from '@/services/tenantSettings'

export interface BrandingSettingsTabProps {
  canEdit: boolean
}

export function BrandingSettingsTab({ canEdit }: BrandingSettingsTabProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [data, setData] = useState<BrandingSettingsDto | null>(null)

  // Form state
  const [logoUrl, setLogoUrl] = useState('')
  const [faviconUrl, setFaviconUrl] = useState('')
  const [primaryColor, setPrimaryColor] = useState('')
  const [secondaryColor, setSecondaryColor] = useState('')
  const [darkModeDefault, setDarkModeDefault] = useState(false)

  useEffect(() => {
    const loadSettings = async () => {
      try {
        const settings = await getBrandingSettings()
        setData(settings)
        setLogoUrl(settings.logoUrl || '')
        setFaviconUrl(settings.faviconUrl || '')
        setPrimaryColor(settings.primaryColor || '')
        setSecondaryColor(settings.secondaryColor || '')
        setDarkModeDefault(settings.darkModeDefault)
      } catch (error) {
        if (error instanceof ApiError) {
          toast.error(error.message)
        }
      } finally {
        setLoading(false)
      }
    }
    loadSettings()
  }, [])

  const handleSave = async () => {
    setSaving(true)
    try {
      const updated = await updateBrandingSettings({
        logoUrl: logoUrl.trim() || null,
        faviconUrl: faviconUrl.trim() || null,
        primaryColor: primaryColor.trim() || null,
        secondaryColor: secondaryColor.trim() || null,
        darkModeDefault,
      })
      setData(updated)
      toast.success(t('tenantSettings.saved'))
    } catch (error) {
      if (error instanceof ApiError) {
        toast.error(error.message)
      } else {
        toast.error(t('messages.operationFailed'))
      }
    } finally {
      setSaving(false)
    }
  }

  const hasChanges = data && (
    (logoUrl.trim() || '') !== (data.logoUrl || '') ||
    (faviconUrl.trim() || '') !== (data.faviconUrl || '') ||
    (primaryColor.trim() || '') !== (data.primaryColor || '') ||
    (secondaryColor.trim() || '') !== (data.secondaryColor || '') ||
    darkModeDefault !== data.darkModeDefault
  )

  if (loading) {
    return (
      <Card>
        <CardContent className="py-8">
          <div className="animate-pulse space-y-4">
            <div className="h-4 w-48 bg-muted rounded" />
            <div className="h-10 w-full bg-muted rounded" />
            <div className="h-4 w-48 bg-muted rounded" />
            <div className="h-10 w-full bg-muted rounded" />
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('tenantSettings.branding.title')}</CardTitle>
        <CardDescription>{t('tenantSettings.branding.description')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="logoUrl">{t('tenantSettings.branding.logoUrl')}</Label>
            <Input
              id="logoUrl"
              value={logoUrl}
              onChange={(e) => setLogoUrl(e.target.value)}
              placeholder="https://example.com/logo.png"
              disabled={!canEdit}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="faviconUrl">{t('tenantSettings.branding.faviconUrl')}</Label>
            <Input
              id="faviconUrl"
              value={faviconUrl}
              onChange={(e) => setFaviconUrl(e.target.value)}
              placeholder="https://example.com/favicon.ico"
              disabled={!canEdit}
            />
          </div>
        </div>

        <div className="grid gap-6 sm:grid-cols-2">
          <div className="space-y-2">
            <Label>{t('tenantSettings.branding.primaryColor')}</Label>
            <ColorPicker
              value={primaryColor || '#3B82F6'}
              onChange={setPrimaryColor}
              showCustomInput={canEdit}
            />
          </div>
          <div className="space-y-2">
            <Label>{t('tenantSettings.branding.secondaryColor')}</Label>
            <ColorPicker
              value={secondaryColor || '#6366F1'}
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
          />
        </div>

        {canEdit && (
          <div className="flex justify-end">
            <Button onClick={handleSave} disabled={saving || !hasChanges}>
              <Save className="h-4 w-4 mr-2" />
              {saving ? t('buttons.saving') : t('buttons.save')}
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
