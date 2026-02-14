import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Loader2, Pencil, Eye, GitFork } from 'lucide-react'
import { Badge, Button, Card, CardContent, CardDescription, CardHeader, CardTitle } from '@uikit'

import { ApiError } from '@/services/apiClient'
import {
  getEmailTemplates,
  previewEmailTemplate,
  getDefaultSampleData,
  type EmailTemplateListDto,
  type EmailPreviewResponse,
} from '@/services/emailTemplates'
import { formatDisplayName } from '@/lib/utils'
import { EmailPreviewDialog } from './EmailPreviewDialog'

export interface EmailTemplatesTabProps {
  onEdit: (id: string) => void
}

export function EmailTemplatesTab({ onEdit }: EmailTemplatesTabProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(true)
  const [templates, setTemplates] = useState<EmailTemplateListDto[]>([])

  // Preview dialog state
  const [previewOpen, setPreviewOpen] = useState(false)
  const [previewData, setPreviewData] = useState<EmailPreviewResponse | null>(null)
  const [previewLoading, setPreviewLoading] = useState(false)

  useEffect(() => {
    const loadTemplates = async () => {
      try {
        const data = await getEmailTemplates()
        setTemplates(data)
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Failed to load templates'
        toast.error(message)
      } finally {
        setLoading(false)
      }
    }
    loadTemplates()
  }, [])

  const handlePreview = async (template: EmailTemplateListDto) => {
    setPreviewData(null)
    setPreviewLoading(true)
    setPreviewOpen(true)

    try {
      const sampleData = getDefaultSampleData(template.availableVariables)
      const preview = await previewEmailTemplate(template.id, { sampleData })
      setPreviewData(preview)
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to load preview'
      toast.error(message)
      setPreviewOpen(false)
    } finally {
      setPreviewLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <>
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
          <CardTitle className="text-lg">{t('emailTemplates.title')}</CardTitle>
          <CardDescription>{t('emailTemplates.description')}</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-2">
            {templates.map((template) => (
              <Card key={template.id} className="overflow-hidden shadow-sm hover:shadow-md transition-all duration-300">
                <CardContent className="p-4">
                  <div className="flex items-start justify-between">
                    <div className="space-y-1">
                      <h4 className="font-medium">{formatDisplayName(template.name)}</h4>
                      <p className="text-sm text-muted-foreground line-clamp-2">
                        {template.description}
                      </p>
                      <div className="flex items-center gap-2 pt-2">
                        <Badge variant={template.isActive ? 'default' : 'secondary'} className="text-xs">
                          {template.isActive ? t('labels.active') : t('labels.inactive')}
                        </Badge>
                        <Badge
                          variant="outline"
                          className={`text-xs ${
                            template.isInherited
                              ? 'text-purple-600 border-purple-600/30'
                              : 'text-green-600 border-green-600/30'
                          }`}
                        >
                          <GitFork className="h-3 w-3 mr-1" />
                          {template.isInherited ? t('legalPages.platformDefault') : t('legalPages.customized')}
                        </Badge>
                      </div>
                    </div>
                    <div className="flex flex-col gap-1">
                      <Button variant="ghost" size="icon" onClick={() => onEdit(template.id)} className="cursor-pointer">
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="icon" onClick={() => handlePreview(template)} className="cursor-pointer">
                        <Eye className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
          {templates.length === 0 && (
            <div className="text-center py-8 text-muted-foreground">
              No email templates found.
            </div>
          )}
        </CardContent>
      </Card>

      {/* Preview Dialog */}
      <EmailPreviewDialog
        open={previewOpen}
        onOpenChange={setPreviewOpen}
        preview={previewData}
        loading={previewLoading}
      />
    </>
  )
}
