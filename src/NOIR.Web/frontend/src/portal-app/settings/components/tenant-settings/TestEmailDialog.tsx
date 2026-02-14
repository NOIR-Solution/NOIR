import { useState, useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Send, Mail, Loader2 } from 'lucide-react'
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
} from '@uikit'

import { useAuthContext } from '@/contexts/AuthContext'
import { sendTestEmail, getDefaultSampleData } from '@/services/emailTemplates'
import { ApiError } from '@/services/apiClient'
import { useValidatedForm } from '@/hooks/useValidatedForm'
import { sendTestEmailSchema } from '@/validation/schemas.generated'
import { createValidationTranslator } from '@/lib/validation-i18n'
import { z } from 'zod'

// Extended schema factory to include dynamic sample data fields
const createTestEmailFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  sendTestEmailSchema.omit({ templateId: true, sampleData: true }).extend({
    recipientEmail: z.string().min(1, { message: t('validation.required') }).email({ message: t('validation.invalidEmail') }),
  })

type TestEmailFormData = z.infer<ReturnType<typeof createTestEmailFormSchema>>

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

  // Memoized translation function for validation errors
  const translateError = useMemo(() => createValidationTranslator(t), [t])

  // Sample data state (dynamic fields not in schema)
  const [sampleData, setSampleData] = useState<Record<string, string>>({})

  // Use validated form with Zod schema
  const { form, handleSubmit, isSubmitting, serverError } = useValidatedForm<TestEmailFormData>({
    schema: createTestEmailFormSchema(t),
    defaultValues: {
      recipientEmail: '',
    },
    onSubmit: async (data) => {
      await sendTestEmail(templateId, {
        recipientEmail: data.recipientEmail,
        sampleData,
      })
      toast.success(t('emailTemplates.testEmailSent'))
      onOpenChange(false)
    },
    onError: (error) => {
      if (!(error instanceof ApiError)) {
        toast.error(t('messages.operationFailed'))
      }
    },
  })

  // Initialize with user's email and default sample data when dialog opens
  useEffect(() => {
    if (open) {
      form.reset({ recipientEmail: user?.email || '' })
      setSampleData(getDefaultSampleData(availableVariables))
    }
  }, [open, user?.email, availableVariables, form])

  // Update sample data value
  const updateSampleData = (key: string, value: string) => {
    setSampleData((prev) => ({ ...prev, [key]: value }))
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <form onSubmit={handleSubmit}>
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
                {...form.register('recipientEmail')}
                placeholder="Enter email address..."
                aria-label={t('emailTemplates.recipientEmail', 'Recipient email address')}
                aria-invalid={!!form.formState.errors.recipientEmail}
              />
              {form.formState.errors.recipientEmail && (
                <p className="text-sm font-medium text-destructive">{translateError(form.formState.errors.recipientEmail.message)}</p>
              )}
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
                        aria-label={`Sample data for ${variable}`}
                        className="h-8 text-sm"
                      />
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Server Error */}
            {serverError && (
              <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20">
                <p className="text-sm font-medium text-destructive">{serverError}</p>
              </div>
            )}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSubmitting}>
              {t('buttons.cancel')}
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
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
