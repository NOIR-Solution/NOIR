import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Mail, Pencil, Eye, GitFork } from 'lucide-react'
import { Badge, Button, Card, CardContent, CardDescription, CardHeader, CardTitle, EmptyState, Skeleton } from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'

import { ApiError } from '@/services/apiClient'
import {
  getEmailTemplates,
  previewEmailTemplate,
  getDefaultSampleData,
  type EmailTemplateListDto,
  type EmailPreviewResponse,
} from '@/services/emailTemplates'
import { formatDisplayName } from '@/lib/utils'
import { EmailPreviewDialog } from '../tenant-settings/EmailPreviewDialog'

export interface PlatformEmailTemplatesTabProps {
  onEdit: (id: string) => void
}

export const PlatformEmailTemplatesTab = ({ onEdit }: PlatformEmailTemplatesTabProps) => {
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
        const message = err instanceof ApiError ? err.message : t('emailTemplates.failedToLoad')
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
      const message = err instanceof ApiError ? err.message : t('emailTemplates.failedToLoadPreview')
      toast.error(message)
      setPreviewOpen(false)
    } finally {
      setPreviewLoading(false)
    }
  }

  if (loading) {
    return (
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-4 w-64" />
        </CardHeader>
        <CardContent>
          <div className="grid gap-6 sm:grid-cols-2">
            {[1, 2, 3, 4].map(i => (
              <Skeleton key={i} className="h-48 rounded-lg" />
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <>
      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <CardTitle className="text-lg">{t('emailTemplates.title')}</CardTitle>
          <CardDescription>{t('emailTemplates.description')}</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid gap-6 sm:grid-cols-2">
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
                        <Badge variant="outline" className={getStatusBadgeClasses(template.isActive ? 'green' : 'gray')}>
                          {template.isActive ? t('labels.active') : t('labels.inactive')}
                        </Badge>
                        <Badge variant="outline" className="text-purple-600 border-purple-600/30 text-xs">
                          <GitFork className="h-3 w-3 mr-1" />
                          {t('legalPages.platformDefault')}
                        </Badge>
                      </div>
                    </div>
                    <div className="flex flex-col gap-1">
                      <Button variant="ghost" size="icon" onClick={() => onEdit(template.id)} className="cursor-pointer" aria-label={`${t('buttons.edit')} ${formatDisplayName(template.name)}`}>
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="icon" onClick={() => handlePreview(template)} className="cursor-pointer" aria-label={`${t('buttons.preview')} ${formatDisplayName(template.name)}`}>
                        <Eye className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
          {templates.length === 0 && (
            <EmptyState
              icon={Mail}
              title={t('emailTemplates.noPlatformTemplatesFound')}
              description={t('emailTemplates.noPlatformTemplatesDescription', 'No platform email templates have been configured yet.')}
            />
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
