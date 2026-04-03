import { useState, useEffect, useRef, useCallback } from 'react'
import { useParams, useNavigate, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { toast } from 'sonner'
import { useEntityUpdateSignal } from '@/hooks/useEntityUpdateSignal'
import { OfflineBanner } from '@/components/OfflineBanner'
import { EntityConflictDialog } from '@/components/EntityConflictDialog'
import { EntityDeletedDialog } from '@/components/EntityDeletedDialog'
import { FileText, ArrowLeft, RotateCcw, Save, Info, Loader2 } from 'lucide-react'
import { type Editor as TiptapEditor } from '@tiptap/react'
import {
  useLegalPageQuery,
  useUpdateLegalPage,
  useRevertLegalPage,
} from '@/portal-app/settings/queries'

import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  Input,
  Label,
  RichTextEditor,
  Skeleton,
  Switch,
  Textarea,
} from '@uikit'

import { usePageContext } from '@/hooks/usePageContext'
import { type LegalPageDto } from '@/services/legalPages'
import { ApiError } from '@/services/apiClient'

/**
 * Legal Page Edit Page
 * Admin page for editing legal page content with rich text editor.
 */
export const LegalPageEditPage = () => {
  usePageContext('Legal Pages')
  const { id } = useParams<{ id: string }>()
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const fromContext = searchParams.get('from')
  const settingsBackUrl = fromContext === 'platform'
    ? '/portal/admin/platform-settings?tab=legalPages'
    : '/portal/admin/tenant-settings?tab=legalPages'
  const { formatDate } = useRegionalSettings()
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.LegalPagesUpdate)
  const editorRef = useRef<TiptapEditor | null>(null)

  // TanStack Query + Mutations
  const { data: queryPage, isLoading: loading } = useLegalPageQuery(id)
  const updateMutation = useUpdateLegalPage()
  const revertMutation = useRevertLegalPage()

  // State
  const [page, setPage] = useState<LegalPageDto | null>(null)
  const [revertDialogOpen, setRevertDialogOpen] = useState(false)

  // Form state
  const [title, setTitle] = useState('')
  const [htmlContent, setHtmlContent] = useState('')
  const [metaTitle, setMetaTitle] = useState('')
  const [metaDescription, setMetaDescription] = useState('')
  const [canonicalUrl, setCanonicalUrl] = useState('')
  const [allowIndexing, setAllowIndexing] = useState(true)

  // Sync form state when query data arrives
  useEffect(() => {
    if (queryPage) {
      setPage(queryPage)
      setTitle(queryPage.title)
      setHtmlContent(queryPage.htmlContent)
      setMetaTitle(queryPage.metaTitle || '')
      setMetaDescription(queryPage.metaDescription || '')
      setCanonicalUrl(queryPage.canonicalUrl || '')
      setAllowIndexing(queryPage.allowIndexing)
    }
  }, [queryPage])

  // Handle save
  const handleSave = () => {
    if (!id || !canEdit) return

    if (!title.trim()) {
      toast.error(t('legalPages.titleRequired'))
      return
    }
    if (!htmlContent.trim()) {
      toast.error(t('legalPages.contentRequired'))
      return
    }
    if (metaTitle.length > 60) {
      toast.error(t('legalPages.metaTitleMaxLength'))
      return
    }
    if (metaDescription.length > 160) {
      toast.error(t('legalPages.metaDescriptionMaxLength'))
      return
    }
    if (canonicalUrl && !isValidUrl(canonicalUrl)) {
      toast.error(t('legalPages.canonicalUrlInvalid'))
      return
    }

    updateMutation.mutate(
      {
        id,
        request: {
          title: title.trim(),
          htmlContent: htmlContent.trim(),
          metaTitle: metaTitle.trim() || null,
          metaDescription: metaDescription.trim() || null,
          canonicalUrl: canonicalUrl.trim() || null,
          allowIndexing,
        },
      },
      {
        onSuccess: (updated) => {
          setPage(updated)
          toast.success(t('legalPages.savedSuccess'))
          // If COW created a new page, navigate to the new ID (preserve from param)
          if (updated.id !== id) {
            const fromParam = fromContext ? `?from=${fromContext}` : ''
            navigate(`/portal/legal-pages/${updated.id}${fromParam}`, { replace: true })
          }
        },
        onError: (error) => {
          if (error instanceof ApiError) {
            toast.error(error.message)
          } else {
            toast.error(t('messages.operationFailed'))
          }
        },
      },
    )
  }

  // URL validation helper
  const isValidUrl = (url: string): boolean => {
    try {
      new URL(url)
      return true
    } catch {
      return false
    }
  }

  // Handle revert
  const handleRevert = () => {
    if (!id) return
    revertMutation.mutate(id, {
      onSuccess: (reverted) => {
        setPage(reverted)
        setTitle(reverted.title)
        setHtmlContent(reverted.htmlContent)
        setMetaTitle(reverted.metaTitle || '')
        setMetaDescription(reverted.metaDescription || '')
        setCanonicalUrl(reverted.canonicalUrl || '')
        setAllowIndexing(reverted.allowIndexing)
        toast.success(t('legalPages.revertedSuccess'))
        setRevertDialogOpen(false)
        // Navigate to the platform page ID (preserve from param)
        if (reverted.id !== id) {
          const fromParam = fromContext ? `?from=${fromContext}` : ''
          navigate(`/portal/legal-pages/${reverted.id}${fromParam}`, { replace: true })
        }
      },
      onError: (error) => {
        if (error instanceof ApiError) {
          toast.error(error.message)
        } else {
          toast.error(t('messages.operationFailed'))
        }
        setRevertDialogOpen(false)
      },
    })
  }

  const refreshPage = useCallback(() => {
    if (id) {
      import('@/services/legalPages').then(({ getLegalPageById }) => {
        getLegalPageById(id).then((data) => {
          setPage(data)
          setTitle(data.title)
          setHtmlContent(data.htmlContent)
          setMetaTitle(data.metaTitle || '')
          setMetaDescription(data.metaDescription || '')
          setCanonicalUrl(data.canonicalUrl || '')
          setAllowIndexing(data.allowIndexing)
        }).catch(() => {})
      })
    }
  }, [id])

  // Check if form has changes
  const hasChanges = page && (
    title !== page.title ||
    htmlContent !== page.htmlContent ||
    (metaTitle || '') !== (page.metaTitle || '') ||
    (metaDescription || '') !== (page.metaDescription || '') ||
    (canonicalUrl || '') !== (page.canonicalUrl || '') ||
    allowIndexing !== page.allowIndexing
  )

  const { conflictSignal, deletedSignal, dismissConflict, reloadAndRestart, isReconnecting } = useEntityUpdateSignal({
    entityType: 'LegalPage',
    entityId: id,
    isDirty: !!hasChanges,
    onAutoReload: refreshPage,
    onNavigateAway: () => navigate(settingsBackUrl),
  })

  if (loading) {
    return (
      <div className="py-6 space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded" />
          <div className="space-y-2">
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-32" />
          </div>
        </div>
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-2 space-y-4">
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6">
                <Skeleton className="h-4 w-24 mb-3" />
                <Skeleton className="h-10 w-full mb-4" />
                <Skeleton className="h-[400px] w-full rounded" />
              </CardContent>
            </Card>
          </div>
          <div className="space-y-4">
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6 space-y-3">
                <Skeleton className="h-4 w-20 mb-3" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </CardContent>
            </Card>
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6 space-y-3">
                <Skeleton className="h-4 w-16 mb-3" />
                <Skeleton className="h-3 w-full" />
                <Skeleton className="h-3 w-full" />
                <Skeleton className="h-3 w-2/3" />
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    )
  }

  if (!page) {
    return null
  }

  return (
    <div className="py-6 space-y-6">
      <OfflineBanner visible={isReconnecting} />
      <EntityConflictDialog signal={conflictSignal} onContinueEditing={dismissConflict} onReloadAndRestart={reloadAndRestart} />
      <EntityDeletedDialog signal={deletedSignal} onGoBack={() => navigate(settingsBackUrl)} />

      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="icon" className="cursor-pointer" onClick={() => navigate(settingsBackUrl)} aria-label={t('buttons.back')}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div className="p-2 bg-primary/10 rounded-lg">
            <FileText className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight">{page.title}</h1>
            <div className="flex items-center gap-2 mt-1">
              <span className="text-sm text-muted-foreground">/{page.slug}</span>
              {page.isInherited && (
                <Badge variant="outline" className="text-purple-600 border-purple-600/30">
                  {t('legalPages.platformDefault')}
                </Badge>
              )}
              {!page.isInherited && page.version > 1 && (
                <Badge variant="outline" className="text-blue-600 border-blue-600/30">
                  {t('legalPages.customized', { version: page.version })}
                </Badge>
              )}
            </div>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {/* Revert button - only shown for non-inherited (tenant-owned) pages */}
          {!page.isInherited && canEdit && (
            <>
              <Button variant="outline" disabled={revertMutation.isPending} className="cursor-pointer" onClick={() => setRevertDialogOpen(true)}>
                <RotateCcw className="h-4 w-4 mr-2" />
                {t('buttons.revert')}
              </Button>
              <Credenza open={revertDialogOpen} onOpenChange={setRevertDialogOpen}>
                <CredenzaContent className="border-destructive/30">
                  <CredenzaHeader>
                    <div className="flex items-center gap-3">
                      <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                        <RotateCcw className="h-5 w-5 text-destructive" />
                      </div>
                      <div>
                        <CredenzaTitle>{t('legalPages.revertTitle')}</CredenzaTitle>
                        <CredenzaDescription>
                          {t('legalPages.revertDescription')}
                        </CredenzaDescription>
                      </div>
                    </div>
                  </CredenzaHeader>
                  <CredenzaBody />
                  <CredenzaFooter>
                    <Button variant="outline" onClick={() => setRevertDialogOpen(false)} disabled={revertMutation.isPending} className="cursor-pointer">
                      {t('buttons.cancel')}
                    </Button>
                    <Button
                      variant="destructive"
                      onClick={handleRevert}
                      disabled={revertMutation.isPending}
                      className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
                    >
                      {revertMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                      {revertMutation.isPending ? t('labels.reverting', 'Reverting...') : t('buttons.revert')}
                    </Button>
                  </CredenzaFooter>
                </CredenzaContent>
              </Credenza>
            </>
          )}
          {canEdit && (
            <Button onClick={handleSave} disabled={updateMutation.isPending || !hasChanges} className="cursor-pointer">
              <Save className="h-4 w-4 mr-2" />
              {updateMutation.isPending ? t('buttons.saving') : t('buttons.save')}
            </Button>
          )}
        </div>
      </div>

      {/* Copy-on-Write notice for platform templates */}
      {page.isInherited && (
        <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg p-3 text-sm text-purple-800 dark:text-purple-200 flex items-start gap-3">
          <Info className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-medium">{t('legalPages.customizingPlatform')}</p>
            <p className="text-purple-600 dark:text-purple-300 mt-1">
              {t('legalPages.customizingPlatformDescription')}
            </p>
          </div>
        </div>
      )}

      {/* Edit Form */}
      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-4">
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader>
              <CardTitle className="text-base">{t('legalPages.content')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="title">{t('legalPages.title')}</Label>
                <Input
                  id="title"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder={t('legalPages.pageTitlePlaceholder')}
                  aria-label={t('legalPages.pageTitle', 'Page title')}
                  disabled={!canEdit}
                />
              </div>
              <div className="space-y-2">
                <Label>{t('legalPages.htmlContent')}</Label>
                <RichTextEditor
                  preset="full"
                  height={500}
                  value={htmlContent}
                  onChange={(content) => setHtmlContent(content)}
                  readOnly={!canEdit}
                  onImageUpload={async (file) => {
                    const formData = new FormData()
                    formData.append('file', file)
                    const response = await fetch('/api/media/upload?folder=legal', {
                      method: 'POST',
                      body: formData,
                      credentials: 'include',
                    })
                    if (!response.ok) {
                      throw new Error(t('errors.uploadFailed', 'Upload failed'))
                    }
                    const result = await response.json()
                    return result.defaultUrl || result.location || ''
                  }}
                  onReady={(editor) => { editorRef.current = editor }}
                />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader>
              <CardTitle className="text-base">{t('legalPages.seo')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="metaTitle">{t('legalPages.metaTitle')}</Label>
                <Input
                  id="metaTitle"
                  value={metaTitle}
                  onChange={(e) => setMetaTitle(e.target.value)}
                  placeholder={t('legalPages.seoTitlePlaceholder')}
                  aria-label={t('legalPages.seoMetaTitle', 'SEO meta title')}
                  maxLength={60}
                  disabled={!canEdit}
                />
                <p className="text-xs text-muted-foreground">
                  {metaTitle.length}/60 {t('legalPages.characters')}
                </p>
              </div>
              <div className="space-y-2">
                <Label htmlFor="metaDescription">{t('legalPages.metaDescription')}</Label>
                <Textarea
                  id="metaDescription"
                  value={metaDescription}
                  onChange={(e) => setMetaDescription(e.target.value)}
                  placeholder={t('legalPages.seoDescriptionPlaceholder')}
                  aria-label={t('legalPages.seoMetaDescription', 'SEO meta description')}
                  className="min-h-[80px]"
                  maxLength={160}
                  disabled={!canEdit}
                />
                <p className="text-xs text-muted-foreground">
                  {metaDescription.length}/160 {t('legalPages.characters')}
                </p>
              </div>
              <div className="space-y-2">
                <Label htmlFor="canonicalUrl">{t('legalPages.canonicalUrlLabel')}</Label>
                <Input
                  id="canonicalUrl"
                  value={canonicalUrl}
                  onChange={(e) => setCanonicalUrl(e.target.value)}
                  placeholder="https://example.com/page"
                  aria-label={t('legalPages.canonicalUrl', 'Canonical URL')}
                  disabled={!canEdit}
                />
                <p className="text-xs text-muted-foreground">
                  {t('legalPages.canonicalUrlHint')}
                </p>
              </div>
              <div className="flex items-center justify-between pt-2">
                <div className="space-y-0.5">
                  <Label htmlFor="allowIndexing">{t('legalPages.allowIndexing')}</Label>
                  <p className="text-xs text-muted-foreground">
                    {t('legalPages.allowIndexingHint')}
                  </p>
                </div>
                <Switch
                  id="allowIndexing"
                  checked={allowIndexing}
                  onCheckedChange={setAllowIndexing}
                  disabled={!canEdit}
                />
              </div>
            </CardContent>
          </Card>

          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader>
              <CardTitle className="text-base">{t('legalPages.info')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('legalPages.slug')}:</span>
                <span className="font-mono">{page.slug}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('legalPages.status')}:</span>
                <span>{page.isActive ? t('legalPages.active') : t('legalPages.inactive')}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('legalPages.version')}:</span>
                <span>{page.version}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('legalPages.lastModified')}:</span>
                <span>{formatDate(page.lastModified)}</span>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

export default LegalPageEditPage
