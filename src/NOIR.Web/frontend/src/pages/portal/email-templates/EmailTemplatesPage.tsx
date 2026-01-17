import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Mail, Edit, Eye } from 'lucide-react'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  getEmailTemplates,
  previewEmailTemplate,
  getDefaultSampleData,
  type EmailTemplateListDto,
  type EmailPreviewResponse,
} from '@/services/emailTemplates'
import { ApiError } from '@/services/apiClient'
import { PreviewDialog } from './PreviewDialog'

/**
 * Email Templates List Page
 * Admin-only page for viewing and managing email templates.
 */
export default function EmailTemplatesPage() {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.EmailTemplatesUpdate)

  // State
  const [templates, setTemplates] = useState<EmailTemplateListDto[]>([])
  const [loading, setLoading] = useState(true)

  // Preview dialog state
  const [previewOpen, setPreviewOpen] = useState(false)
  const [previewData, setPreviewData] = useState<EmailPreviewResponse | null>(null)
  const [previewLoading, setPreviewLoading] = useState(false)

  // Load templates
  const loadTemplates = async () => {
    setLoading(true)
    try {
      const data = await getEmailTemplates()
      setTemplates(data)
    } catch (error) {
      if (error instanceof ApiError) {
        toast.error(error.message)
      } else {
        toast.error(t('messages.operationFailed'))
      }
    } finally {
      setLoading(false)
    }
  }

  // Load templates on mount
  useEffect(() => {
    loadTemplates()
  }, [])

  // Handle preview
  const handlePreview = async (template: EmailTemplateListDto) => {
    setPreviewLoading(true)
    setPreviewOpen(true)
    try {
      const sampleData = getDefaultSampleData(template.availableVariables)
      const preview = await previewEmailTemplate(template.id, { sampleData })
      setPreviewData(preview)
    } catch (error) {
      if (error instanceof ApiError) {
        toast.error(error.message)
      } else {
        toast.error(t('messages.operationFailed'))
      }
      setPreviewOpen(false)
    } finally {
      setPreviewLoading(false)
    }
  }

  // Handle edit navigation
  const handleEdit = (id: string) => {
    navigate(`/portal/email-templates/${id}`)
  }

  // Get template display name (e.g., "Password Reset OTP" from "PasswordResetOtp")
  const getDisplayName = (name: string): string => {
    return name
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (str) => str.toUpperCase())
      .trim()
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center gap-3">
        <div className="p-2 bg-primary/10 rounded-lg">
          <Mail className="h-6 w-6 text-primary" />
        </div>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Email Templates</h1>
          <p className="text-muted-foreground">
            Manage your system email templates for notifications and communications.
          </p>
        </div>
      </div>

      {/* Loading State */}
      {loading && (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3].map((i) => (
            <Card key={i} className="animate-pulse">
              <CardHeader className="space-y-2">
                <div className="h-4 w-3/4 bg-muted rounded" />
                <div className="h-3 w-1/2 bg-muted rounded" />
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  <div className="h-3 w-full bg-muted rounded" />
                  <div className="h-3 w-2/3 bg-muted rounded" />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Templates Grid */}
      {!loading && templates.length > 0 && (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {templates.map((template) => (
            <Card
              key={template.id}
              className="group hover:shadow-md transition-shadow border-border hover:border-blue-600/30"
            >
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-3">
                    <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-blue-600/10">
                      <Mail className="h-5 w-5 text-blue-600" />
                    </div>
                    <div className="min-w-0">
                      <CardTitle className="text-base truncate">
                        {getDisplayName(template.name)}
                      </CardTitle>
                      <div className="flex items-center gap-2 mt-1">
                        {template.isActive ? (
                          <Badge variant="outline" className="text-green-600 border-green-600/30">
                            Active
                          </Badge>
                        ) : (
                          <Badge variant="outline" className="text-muted-foreground">
                            Inactive
                          </Badge>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <CardDescription className="line-clamp-2">{template.subject}</CardDescription>

                {/* Variables */}
                {template.availableVariables.length > 0 && (
                  <div className="space-y-1">
                    <p className="text-xs font-medium text-muted-foreground">Variables:</p>
                    <div className="flex flex-wrap gap-1">
                      {template.availableVariables.slice(0, 3).map((variable) => (
                        <Badge key={variable} variant="outline" className="text-xs font-mono">
                          {`{{${variable}}}`}
                        </Badge>
                      ))}
                      {template.availableVariables.length > 3 && (
                        <Badge variant="outline" className="text-xs">
                          +{template.availableVariables.length - 3}
                        </Badge>
                      )}
                    </div>
                  </div>
                )}

                {/* Version */}
                <p className="text-xs text-muted-foreground">Version: {template.version}</p>

                {/* Actions */}
                <div className="flex gap-2 pt-2 border-t border-border">
                  <Button
                    variant="outline"
                    size="sm"
                    className={canEdit ? 'flex-1' : 'w-full'}
                    onClick={() => handlePreview(template)}
                  >
                    <Eye className="h-4 w-4 mr-2" />
                    Preview
                  </Button>
                  {canEdit && (
                    <Button size="sm" className="flex-1" onClick={() => handleEdit(template.id)}>
                      <Edit className="h-4 w-4 mr-2" />
                      {t('buttons.edit')}
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Empty State */}
      {!loading && templates.length === 0 && (
        <Card className="p-12">
          <div className="text-center">
            <Mail className="mx-auto h-12 w-12 text-muted-foreground/50" />
            <h3 className="mt-4 text-lg font-semibold text-foreground">{t('labels.noResults')}</h3>
            <p className="mt-2 text-muted-foreground">
              No email templates have been created yet.
            </p>
          </div>
        </Card>
      )}

      {/* Preview Dialog */}
      <PreviewDialog
        open={previewOpen}
        onOpenChange={setPreviewOpen}
        preview={previewData}
        loading={previewLoading}
      />
    </div>
  )
}
