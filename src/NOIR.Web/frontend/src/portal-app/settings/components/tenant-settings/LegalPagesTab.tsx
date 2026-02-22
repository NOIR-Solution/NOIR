import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { toast } from 'sonner'
import { FileText, Pencil, Eye, GitFork } from 'lucide-react'
import { Badge, Button, Card, CardContent, CardDescription, CardHeader, CardTitle, EmptyState, Skeleton } from '@uikit'
import { getStatusBadgeClasses } from '@/utils/statusBadge'

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
        const message = err instanceof ApiError ? err.message : t('legalPages.failedToLoad')
        toast.error(message)
      } finally {
        setLoading(false)
      }
    }
    loadPages()
  }, [])

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
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader>
        <CardTitle className="text-lg">{t('legalPages.title')}</CardTitle>
        <CardDescription>{t('legalPages.description')}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-6 sm:grid-cols-2">
          {pages.map((page) => (
            <Card key={page.id} className="overflow-hidden shadow-sm hover:shadow-md transition-all duration-300">
              <CardContent className="p-4">
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <h4 className="font-medium">{page.title}</h4>
                    <p className="text-sm text-muted-foreground">/{page.slug}</p>
                    <div className="flex items-center gap-2 pt-2">
                      <Badge variant="outline" className={getStatusBadgeClasses(page.isActive ? 'green' : 'gray')}>
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
                    <Button variant="ghost" size="icon" onClick={() => onEdit(page.id)} className="cursor-pointer" aria-label={`${t('buttons.edit')} ${page.title}`}>
                      <Pencil className="h-4 w-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => window.open(`/${page.slug === 'terms-of-service' ? 'terms' : 'privacy'}`, '_blank')}
                      className="cursor-pointer"
                      aria-label={`${t('buttons.preview')} ${page.title}`}
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
          <EmptyState
            icon={FileText}
            title={t('legalPages.noPagesFound')}
            size="sm"
          />
        )}
      </CardContent>
    </Card>
  )
}
