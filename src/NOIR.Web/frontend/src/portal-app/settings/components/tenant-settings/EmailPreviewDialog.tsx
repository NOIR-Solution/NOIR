import { useTranslation } from 'react-i18next'
import { Mail, FileText, Loader2 } from 'lucide-react'
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@uikit'

import type { EmailPreviewResponse } from '@/services/emailTemplates'

interface EmailPreviewDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  preview: EmailPreviewResponse | null
  loading?: boolean
}

/**
 * Dialog for previewing email template with rendered content.
 */
export const EmailPreviewDialog = ({ open, onOpenChange, preview, loading }: EmailPreviewDialogProps) => {
  const { t } = useTranslation('common')

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-hidden flex flex-col">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Mail className="h-5 w-5 text-blue-600" />
            {t('emailTemplates.emailPreviewTitle')}
          </DialogTitle>
          <DialogDescription>
            {t('emailTemplates.emailPreviewDescription')}
          </DialogDescription>
        </DialogHeader>

        {loading && (
          <div className="flex-1 flex items-center justify-center py-12">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
          </div>
        )}

        {!loading && preview && (
          <div className="flex-1 overflow-y-auto space-y-4">
            {/* Subject Line */}
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <Badge variant="outline">{t('emailTemplates.subjectBadge')}</Badge>
              </div>
              <p className="text-sm font-medium text-foreground bg-muted/50 rounded-lg p-3">
                {preview.subject}
              </p>
            </div>

            {/* HTML Preview */}
            <div className="space-y-2">
              <div className="flex items-center gap-2">
                <Badge variant="outline">{t('emailTemplates.htmlBodyBadge')}</Badge>
              </div>
              <div className="border rounded-lg overflow-hidden bg-white">
                <iframe
                  srcDoc={preview.htmlBody}
                  title={t('emailTemplates.emailPreviewIframe')}
                  className="w-full h-[400px] border-0"
                  sandbox="allow-same-origin"
                />
              </div>
            </div>

            {/* Plain Text Preview (if available) */}
            {preview.plainTextBody && (
              <div className="space-y-2">
                <div className="flex items-center gap-2">
                  <Badge variant="outline">
                    <FileText className="h-3 w-3 mr-1" />
                    {t('emailTemplates.plainTextBadge')}
                  </Badge>
                </div>
                <pre className="text-sm text-muted-foreground bg-muted/50 rounded-lg p-3 whitespace-pre-wrap font-mono">
                  {preview.plainTextBody}
                </pre>
              </div>
            )}
          </div>
        )}

        <div className="flex justify-end pt-4 border-t border-border">
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t('buttons.close')}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
