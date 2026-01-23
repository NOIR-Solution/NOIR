import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Loader2, Pencil, Eye, GitFork } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ApiError } from '@/services/apiClient'
import { getEmailTemplates, type EmailTemplateListDto } from '@/services/emailTemplates'
import { formatDisplayName } from '@/lib/utils'

export interface EmailTemplatesTabProps {
  onEdit: (id: string) => void
  onView: (id: string) => void
}

export function EmailTemplatesTab({ onEdit, onView }: EmailTemplatesTabProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(true)
  const [templates, setTemplates] = useState<EmailTemplateListDto[]>([])

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

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">{t('emailTemplates.title')}</CardTitle>
        <CardDescription>{t('emailTemplates.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 md:grid-cols-2">
          {templates.map((template) => (
            <Card key={template.id} className="overflow-hidden">
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
                    <Button variant="ghost" size="icon" onClick={() => onEdit(template.id)}>
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button variant="ghost" size="icon" onClick={() => onView(template.id)}>
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
  )
}
