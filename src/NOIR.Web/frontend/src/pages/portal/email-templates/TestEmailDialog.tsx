import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Send, Mail } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuthContext } from '@/contexts/AuthContext'
import { sendTestEmail, getDefaultSampleData } from '@/services/emailTemplates'
import { ApiError } from '@/services/apiClient'

interface TestEmailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  templateId: string
  availableVariables: string[]
}

/**
 * Dialog for sending test emails with sample data.
 */
export function TestEmailDialog({
  open,
  onOpenChange,
  templateId,
  availableVariables,
}: TestEmailDialogProps) {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()

  // State
  const [recipientEmail, setRecipientEmail] = useState('')
  const [sampleData, setSampleData] = useState<Record<string, string>>({})
  const [sending, setSending] = useState(false)

  // Initialize with user's email and default sample data
  useEffect(() => {
    if (open) {
      setRecipientEmail(user?.email || '')
      setSampleData(getDefaultSampleData(availableVariables))
    }
  }, [open, user?.email, availableVariables])

  // Update sample data value
  const updateSampleData = (key: string, value: string) => {
    setSampleData((prev) => ({ ...prev, [key]: value }))
  }

  // Handle send
  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!recipientEmail) {
      toast.error('Please enter a recipient email address.')
      return
    }

    setSending(true)
    try {
      await sendTestEmail(templateId, {
        recipientEmail,
        sampleData,
      })
      toast.success(t('emailTemplates.testEmailSent'))
      onOpenChange(false)
    } catch (error) {
      if (error instanceof ApiError) {
        toast.error(error.message)
      } else {
        toast.error(t('messages.operationFailed'))
      }
    } finally {
      setSending(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <form onSubmit={handleSend}>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Send className="h-5 w-5 text-blue-600" />
              {t('emailTemplates.sendTestEmail')}
            </DialogTitle>
            <DialogDescription>
              Send a test email to verify the template rendering with your sample data.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            {/* Recipient Email */}
            <div className="space-y-2">
              <Label htmlFor="recipient-email" className="flex items-center gap-2">
                <Mail className="h-4 w-4" />
                {t('emailTemplates.recipientEmail')}
              </Label>
              <Input
                id="recipient-email"
                type="email"
                value={recipientEmail}
                onChange={(e) => setRecipientEmail(e.target.value)}
                placeholder="Enter email address..."
              />
            </div>

            {/* Sample Data */}
            {availableVariables.length > 0 && (
              <div className="space-y-3">
                <Label>{t('emailTemplates.sampleData')}</Label>
                <div className="space-y-2 max-h-64 overflow-y-auto">
                  {availableVariables.map((variable) => (
                    <div key={variable} className="space-y-1">
                      <Label htmlFor={`sample-${variable}`} className="text-xs font-mono">
                        {`{{${variable}}}`}
                      </Label>
                      <Input
                        id={`sample-${variable}`}
                        value={sampleData[variable] || ''}
                        onChange={(e) => updateSampleData(variable, e.target.value)}
                        placeholder={`Value for ${variable}...`}
                        className="h-8 text-sm"
                      />
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={sending}>
              {t('buttons.cancel')}
            </Button>
            <Button type="submit" disabled={sending}>
              {sending ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                  Sending...
                </>
              ) : (
                <>
                  <Send className="h-4 w-4 mr-2" />
                  Send Test
                </>
              )}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
