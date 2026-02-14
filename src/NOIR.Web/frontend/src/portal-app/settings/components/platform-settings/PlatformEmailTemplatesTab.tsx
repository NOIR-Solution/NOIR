import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Loader2, Pencil, GitFork } from 'lucide-react'
import { Badge, Button, Card, CardContent, CardDescription, CardHeader, CardTitle } from '@uikit'

import { ApiError } from '@/services/apiClient'
import { getEmailTemplates, type EmailTemplateListDto } from '@/services/emailTemplates'
import { formatDisplayName } from '@/lib/utils'

export interface PlatformEmailTemplatesTabProps {
  onEdit: (id: string) => void
}

export const PlatformEmailTemplatesTab = ({ onEdit }: PlatformEmailTemplatesTabProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(true)
  const [templates, setTemplates] = useState<EmailTemplateListDto[]>([])

  useEffect(() => {
    const loadTemplates = async () => {
      try {
        const data = await getEmailTemplates()
        // Filter to only platform templates (isInherited = true means it's a platform default)
        setTemplates(data.filter(t => t.isInherited))
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
                        {template.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                      <Badge variant="outline" className="text-purple-600 border-purple-600/30 text-xs">
                        <GitFork className="h-3 w-3 mr-1" />
                        Platform
                      </Badge>
                    </div>
                  </div>
                  <Button variant="ghost" size="icon" onClick={() => onEdit(template.id)}>
                    <Pencil className="h-4 w-4" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
        {templates.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            No platform email templates found.
          </div>
        )}
      </CardContent>
    </Card>
  )
}
