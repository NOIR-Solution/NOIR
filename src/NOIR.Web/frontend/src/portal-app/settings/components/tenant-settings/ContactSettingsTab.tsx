import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Save } from 'lucide-react'
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Input,
  Label,
  Textarea,
} from '@uikit'

import { ApiError } from '@/services/apiClient'
import {
  getContactSettings,
  updateContactSettings,
  type ContactSettingsDto,
} from '@/services/tenantSettings'

export interface ContactSettingsTabProps {
  canEdit: boolean
}

export const ContactSettingsTab = ({ canEdit }: ContactSettingsTabProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [data, setData] = useState<ContactSettingsDto | null>(null)

  // Form state
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [address, setAddress] = useState('')

  useEffect(() => {
    const loadSettings = async () => {
      try {
        const settings = await getContactSettings()
        setData(settings)
        setEmail(settings.email || '')
        setPhone(settings.phone || '')
        setAddress(settings.address || '')
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
      const updated = await updateContactSettings({
        email: email.trim() || null,
        phone: phone.trim() || null,
        address: address.trim() || null,
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
    (email.trim() || '') !== (data.email || '') ||
    (phone.trim() || '') !== (data.phone || '') ||
    (address.trim() || '') !== (data.address || '')
  )

  if (loading) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
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
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
        <CardTitle>{t('tenantSettings.contact.title')}</CardTitle>
        <CardDescription>{t('tenantSettings.contact.description')}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="contactEmail">{t('tenantSettings.contact.email')}</Label>
          <Input
            id="contactEmail"
            type="text"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="contact@example.com"
            disabled={!canEdit}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="contactPhone">{t('tenantSettings.contact.phone')}</Label>
          <Input
            id="contactPhone"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            placeholder="+1 (555) 123-4567"
            disabled={!canEdit}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="contactAddress">{t('tenantSettings.contact.address')}</Label>
          <Textarea
            id="contactAddress"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            placeholder="123 Business St, Suite 100, City, State 12345"
            className="min-h-[80px]"
            disabled={!canEdit}
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
