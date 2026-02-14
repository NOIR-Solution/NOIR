import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { toast } from 'sonner'
import { Loader2, Pencil, Eye, GitFork } from 'lucide-react'
import { Badge, Button, Card, CardContent, CardDescription, CardHeader, CardTitle } from '@uikit'

import { ApiError } from '@/services/apiClient'
import { getLegalPages, type LegalPageListDto } from '@/services/legalPages'

export interface LegalPagesTabProps {
  onEdit: (id: string) => void
}

export const LegalPagesTab = ({ onEdit }: LegalPagesTabProps) => {
  const { t } = useTranslation('common')
  const { formatDate } = useRegionalSettings()
  const [loading, setLoading] = useState(true)
  const [pages, setPages] = useState<LegalPageListDto[]>([])

  useEffect(() => {
    const loadPages = async () => {
      try {
        const data = await getLegalPages()
        setPages(data)
      } catch (err) {
        const message = err instanceof ApiError ? err.message : 'Failed to load legal pages'
        toast.error(message)
      } finally {
        setLoading(false)
      }
    }
    loadPages()
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
        <CardTitle className="text-lg">{t('legalPages.title')}</CardTitle>
        <CardDescription>{t('legalPages.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 md:grid-cols-2">
          {pages.map((page) => (
            <Card key={page.id} className="overflow-hidden shadow-sm hover:shadow-md transition-all duration-300">
              <CardContent className="p-4">
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <h4 className="font-medium">{page.title}</h4>
                    <p className="text-sm text-muted-foreground">/{page.slug}</p>
                    <div className="flex items-center gap-2 pt-2">
                      <Badge variant={page.isActive ? 'default' : 'secondary'} className="text-xs">
                        {page.isActive ? t('labels.active') : t('labels.inactive')}
                      </Badge>
                      <Badge
                        variant="outline"
                        className={`text-xs ${
                          page.isInherited
                            ? 'text-purple-600 border-purple-600/30'
                            : 'text-green-600 border-green-600/30'
                        }`}
                      >
                        <GitFork className="h-3 w-3 mr-1" />
                        {page.isInherited ? t('legalPages.platformDefault') : t('legalPages.customized')}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground pt-1">
                      {t('legalPages.lastModified')}: {formatDate(page.lastModified)}
                    </p>
                  </div>
                  <div className="flex flex-col gap-1">
                    <Button variant="ghost" size="icon" onClick={() => onEdit(page.id)} className="cursor-pointer">
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => window.open(`/${page.slug === 'terms-of-service' ? 'terms' : 'privacy'}`, '_blank')}
                      className="cursor-pointer"
                    >
                      <Eye className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
        {pages.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            No legal pages found.
          </div>
        )}
      </CardContent>
    </Card>
  )
}
