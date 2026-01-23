import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { FileText, Edit, GitFork, Pencil, Clock } from 'lucide-react'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { usePageContext } from '@/hooks/usePageContext'
import { getLegalPages, type LegalPageListDto } from '@/services/legalPages'
import { ApiError } from '@/services/apiClient'

// Map slugs to friendly display names
const SLUG_DISPLAY_NAMES: Record<string, string> = {
  'terms-of-service': 'Terms of Service',
  'privacy-policy': 'Privacy Policy',
}

// Map slugs to descriptions
const SLUG_DESCRIPTIONS: Record<string, string> = {
  'terms-of-service': 'Legal terms and conditions for using this platform.',
  'privacy-policy': 'How we collect, use, and protect user data.',
}

/**
 * Legal Pages List Page
 * Admin page for viewing and managing legal pages (Terms, Privacy, etc.)
 */
export default function LegalPagesPage() {
  usePageContext('Legal Pages')
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.LegalPagesUpdate)

  // State
  const [pages, setPages] = useState<LegalPageListDto[]>([])
  const [loading, setLoading] = useState(true)

  // Load pages
  const loadPages = async () => {
    setLoading(true)
    try {
      const data = await getLegalPages()
      setPages(data)
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

  // Load pages on mount
  useEffect(() => {
    loadPages()
  }, [])

  // Handle edit navigation
  const handleEdit = (id: string) => {
    navigate(`/portal/legal-pages/${id}`)
  }

  // Get display name for slug
  const getDisplayName = (slug: string): string => {
    return SLUG_DISPLAY_NAMES[slug] || slug.replace(/-/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase())
  }

  // Get description for slug
  const getDescription = (slug: string): string => {
    return SLUG_DESCRIPTIONS[slug] || 'Legal page content'
  }

  // Format date
  const formatDate = (dateStr: string): string => {
    return new Date(dateStr).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center gap-3">
        <div className="p-2 bg-primary/10 rounded-lg">
          <FileText className="h-6 w-6 text-primary" />
        </div>
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Legal Pages</h1>
          <p className="text-muted-foreground">
            Manage your Terms of Service, Privacy Policy, and other legal content.
          </p>
        </div>
      </div>

      {/* Loading State */}
      {loading && (
        <div className="grid gap-4 md:grid-cols-2">
          {[1, 2].map((i) => (
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

      {/* Pages Grid */}
      {!loading && pages.length > 0 && (
        <div className="grid gap-4 md:grid-cols-2">
          {pages.map((page) => (
            <Card
              key={page.id}
              className="group hover:shadow-md transition-shadow border-border hover:border-primary/30"
            >
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-3">
                    <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-primary/10">
                      <FileText className="h-5 w-5 text-primary" />
                    </div>
                    <div className="min-w-0">
                      <CardTitle className="text-base truncate">
                        {getDisplayName(page.slug)}
                      </CardTitle>
                      <div className="flex items-center gap-2 mt-1">
                        {page.isActive ? (
                          <Badge variant="outline" className="text-green-600 border-green-600/30">
                            Active
                          </Badge>
                        ) : (
                          <Badge variant="outline" className="text-muted-foreground">
                            Inactive
                          </Badge>
                        )}
                        {page.isInherited && (
                          <Badge variant="outline" className="text-purple-600 border-purple-600/30">
                            <GitFork className="h-3 w-3 mr-1" />
                            Platform
                          </Badge>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <CardDescription className="line-clamp-2">
                  {getDescription(page.slug)}
                </CardDescription>

                {/* Last Modified */}
                <div className="flex items-center gap-2 text-xs text-muted-foreground">
                  <Clock className="h-3 w-3" />
                  <span>Last modified: {formatDate(page.lastModified)}</span>
                </div>

                {/* Version */}
                <p className="text-xs text-muted-foreground">Version: {page.version}</p>

                {/* Actions */}
                <div className="flex gap-2 pt-2 border-t border-border">
                  {canEdit && (
                    <Button
                      size="sm"
                      className="w-full"
                      variant={page.isInherited ? 'outline' : 'default'}
                      onClick={() => handleEdit(page.id)}
                    >
                      {page.isInherited ? (
                        <>
                          <Pencil className="h-4 w-4 mr-2" />
                          Customize
                        </>
                      ) : (
                        <>
                          <Edit className="h-4 w-4 mr-2" />
                          {t('buttons.edit')}
                        </>
                      )}
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Empty State */}
      {!loading && pages.length === 0 && (
        <Card className="p-12">
          <div className="text-center">
            <FileText className="mx-auto h-12 w-12 text-muted-foreground/50" />
            <h3 className="mt-4 text-lg font-semibold text-foreground">{t('labels.noResults')}</h3>
            <p className="mt-2 text-muted-foreground">
              No legal pages have been created yet.
            </p>
          </div>
        </Card>
      )}
    </div>
  )
}
