import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Mail, Edit, Eye, Search, Globe, RefreshCw } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
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

  // State
  const [templates, setTemplates] = useState<EmailTemplateListDto[]>([])
  const [loading, setLoading] = useState(true)
  const [searchQuery, setSearchQuery] = useState('')
  const [languageFilter, setLanguageFilter] = useState<string | undefined>(undefined)

  // Preview dialog state
  const [previewOpen, setPreviewOpen] = useState(false)
  const [previewData, setPreviewData] = useState<EmailPreviewResponse | null>(null)
  const [previewLoading, setPreviewLoading] = useState(false)

  // Language options
  const languages = [
    { code: undefined, label: t('labels.all') },
    { code: 'en', label: 'English' },
    { code: 'vi', label: 'Tieng Viet' },
  ]

  // Load templates
  const loadTemplates = async () => {
    setLoading(true)
    try {
      const data = await getEmailTemplates(languageFilter, searchQuery || undefined)
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

  // Track if initial load has happened
  const isInitialMount = useRef(true)

  // Load templates on mount and when filters change
  useEffect(() => {
    loadTemplates()
  }, [languageFilter])

  // Debounced search - skip initial mount since languageFilter effect handles it
  useEffect(() => {
    if (isInitialMount.current) {
      isInitialMount.current = false
      return
    }
    const timer = setTimeout(() => {
      loadTemplates()
    }, 300)
    return () => clearTimeout(timer)
  }, [searchQuery])

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

  // Get language badge color
  const getLanguageBadgeVariant = (language: string): 'default' | 'secondary' | 'outline' => {
    return language === 'en' ? 'default' : 'secondary'
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Email Templates</h1>
          <p className="text-muted-foreground">
            Manage your system email templates for notifications and communications.
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={loadTemplates} disabled={loading}>
          <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
          {t('buttons.refresh')}
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-col gap-4 sm:flex-row">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder={`${t('buttons.search')}...`}
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10"
          />
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" className="w-full sm:w-auto">
              <Globe className="h-4 w-4 mr-2" />
              {languageFilter
                ? languages.find((l) => l.code === languageFilter)?.label
                : t('labels.all')}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {languages.map((lang) => (
              <DropdownMenuItem
                key={lang.code ?? 'all'}
                onClick={() => setLanguageFilter(lang.code)}
              >
                {lang.label}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
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
                        <Badge variant={getLanguageBadgeVariant(template.language)}>
                          {template.language.toUpperCase()}
                        </Badge>
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
                    className="flex-1"
                    onClick={() => handlePreview(template)}
                  >
                    <Eye className="h-4 w-4 mr-2" />
                    Preview
                  </Button>
                  <Button size="sm" className="flex-1" onClick={() => handleEdit(template.id)}>
                    <Edit className="h-4 w-4 mr-2" />
                    {t('buttons.edit')}
                  </Button>
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
              {searchQuery || languageFilter
                ? 'Try adjusting your filters to find what you are looking for.'
                : 'No email templates have been created yet.'}
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
