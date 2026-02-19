import { useState, useEffect, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { toast } from 'sonner'
import { FileText, ArrowLeft, RotateCcw, Save, Info } from 'lucide-react'
import { Editor } from '@tinymce/tinymce-react'
import type { Editor as TinyMCEEditor } from 'tinymce'

// Import TinyMCE 6 for self-hosted usage
/* eslint-disable import/no-unresolved */
import 'tinymce/tinymce'
import 'tinymce/models/dom'
import 'tinymce/themes/silver'
import 'tinymce/icons/default'
import 'tinymce/plugins/advlist'
import 'tinymce/plugins/autolink'
import 'tinymce/plugins/lists'
import 'tinymce/plugins/link'
import 'tinymce/plugins/image'
import 'tinymce/plugins/charmap'
import 'tinymce/plugins/preview'
import 'tinymce/plugins/anchor'
import 'tinymce/plugins/searchreplace'
import 'tinymce/plugins/visualblocks'
import 'tinymce/plugins/code'
import 'tinymce/plugins/fullscreen'
import 'tinymce/plugins/insertdatetime'
import 'tinymce/plugins/media'
import 'tinymce/plugins/table'
import 'tinymce/plugins/wordcount'
/* eslint-enable import/no-unresolved */

import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Input,
  Label,
  Switch,
  Textarea,
} from '@uikit'

import { usePageContext } from '@/hooks/usePageContext'
import {
  getLegalPageById,
  updateLegalPage,
  revertLegalPageToDefault,
  type LegalPageDto,
} from '@/services/legalPages'
import { ApiError } from '@/services/apiClient'

/**
 * Legal Page Edit Page
 * Admin page for editing legal page content with TinyMCE editor.
 */
export const LegalPageEditPage = () => {
  usePageContext('Legal Pages')
  const { id } = useParams<{ id: string }>()
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { formatDate } = useRegionalSettings()
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.LegalPagesUpdate)
  const editorRef = useRef<TinyMCEEditor | null>(null)

  // State
  const [page, setPage] = useState<LegalPageDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [reverting, setReverting] = useState(false)

  // Form state
  const [title, setTitle] = useState('')
  const [htmlContent, setHtmlContent] = useState('')
  const [metaTitle, setMetaTitle] = useState('')
  const [metaDescription, setMetaDescription] = useState('')
  const [canonicalUrl, setCanonicalUrl] = useState('')
  const [allowIndexing, setAllowIndexing] = useState(true)

  // Load page
  const loadPage = async () => {
    if (!id) return
    setLoading(true)
    try {
      const data = await getLegalPageById(id)
      setPage(data)
      setTitle(data.title)
      setHtmlContent(data.htmlContent)
      setMetaTitle(data.metaTitle || '')
      setMetaDescription(data.metaDescription || '')
      setCanonicalUrl(data.canonicalUrl || '')
      setAllowIndexing(data.allowIndexing)
    } catch (error) {
      if (error instanceof ApiError) {
        toast.error(error.message)
      } else {
        toast.error(t('messages.operationFailed'))
      }
      navigate('/portal/admin/tenant-settings?tab=legalPages')
    } finally {
      setLoading(false)
    }
  }

  // Load page on mount
  useEffect(() => {
    loadPage()
  }, [id])

  // Handle save
  const handleSave = async () => {
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

    setSaving(true)
    try {
      const updated = await updateLegalPage(id, {
        title: title.trim(),
        htmlContent: htmlContent.trim(),
        metaTitle: metaTitle.trim() || null,
        metaDescription: metaDescription.trim() || null,
        canonicalUrl: canonicalUrl.trim() || null,
        allowIndexing,
      })
      setPage(updated)
      toast.success(t('legalPages.savedSuccess'))
      // If COW created a new page, navigate to the new ID
      if (updated.id !== id) {
        navigate(`/portal/legal-pages/${updated.id}`, { replace: true })
      }
    } catch (error) {
      if (error instanceof ApiError) {
        toast.error(error.message)
      } else {
        toast.error(t('messages.operationFailed'))
      }
    } finally {
      setSaving(false)
    }
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
  const handleRevert = async () => {
    if (!id) return
    setReverting(true)
    try {
      const reverted = await revertLegalPageToDefault(id)
      setPage(reverted)
      setTitle(reverted.title)
      setHtmlContent(reverted.htmlContent)
      setMetaTitle(reverted.metaTitle || '')
      setMetaDescription(reverted.metaDescription || '')
      setCanonicalUrl(reverted.canonicalUrl || '')
      setAllowIndexing(reverted.allowIndexing)
      toast.success(t('legalPages.revertedSuccess'))
      // Navigate to the platform page ID
      if (reverted.id !== id) {
        navigate(`/portal/legal-pages/${reverted.id}`, { replace: true })
      }
    } catch (error) {
      if (error instanceof ApiError) {
        toast.error(error.message)
      } else {
        toast.error(t('messages.operationFailed'))
      }
    } finally {
      setReverting(false)
    }
  }

  // Check if form has changes
  const hasChanges = page && (
    title !== page.title ||
    htmlContent !== page.htmlContent ||
    (metaTitle || '') !== (page.metaTitle || '') ||
    (metaDescription || '') !== (page.metaDescription || '') ||
    (canonicalUrl || '') !== (page.canonicalUrl || '') ||
    allowIndexing !== page.allowIndexing
  )

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="animate-pulse space-y-4">
          <div className="h-8 w-48 bg-muted rounded" />
          <div className="h-64 bg-muted rounded" />
        </div>
      </div>
    )
  }

  if (!page) {
    return null
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="icon" onClick={() => navigate('/portal/admin/tenant-settings?tab=legalPages')}>
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
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <Button variant="outline" disabled={reverting}>
                  <RotateCcw className="h-4 w-4 mr-2" />
                  {t('buttons.revert')}
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>{t('legalPages.revertTitle')}</AlertDialogTitle>
                  <AlertDialogDescription>
                    {t('legalPages.revertDescription')}
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>{t('buttons.cancel')}</AlertDialogCancel>
                  <AlertDialogAction onClick={handleRevert}>
                    {t('buttons.revert')}
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          )}
          {canEdit && (
            <Button onClick={handleSave} disabled={saving || !hasChanges}>
              <Save className="h-4 w-4 mr-2" />
              {saving ? t('buttons.saving') : t('buttons.save')}
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
            <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
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
                <Editor
                  onInit={(_evt, editor) => {
                    editorRef.current = editor
                  }}
                  value={htmlContent}
                  onEditorChange={(content) => setHtmlContent(content)}
                  disabled={!canEdit}
                  init={{
                    height: 500,
                    menubar: true,
                    skin_url: '/tinymce/skins/ui/oxide',
                    content_css: '/tinymce/skins/content/default/content.min.css',
                    plugins: [
                      'advlist',
                      'autolink',
                      'lists',
                      'link',
                      'image',
                      'charmap',
                      'preview',
                      'anchor',
                      'searchreplace',
                      'visualblocks',
                      'code',
                      'fullscreen',
                      'insertdatetime',
                      'media',
                      'table',
                      'wordcount',
                    ],
                    toolbar:
                      'undo redo | blocks | ' +
                      'bold italic forecolor backcolor | alignleft aligncenter ' +
                      'alignright alignjustify | bullist numlist outdent indent | ' +
                      'link image media table | code fullscreen preview',
                    content_style: `
                      body {
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
                        font-size: 16px;
                        line-height: 1.7;
                        color: #333;
                        padding: 15px;
                        max-width: 100%;
                        margin: 0;
                      }
                      body > *:first-child {
                        margin-top: 0;
                      }
                      h1, h2, h3, h4, h5, h6 {
                        margin-top: 1.5em;
                        margin-bottom: 0.5em;
                        font-weight: 600;
                      }
                      p {
                        margin: 1em 0;
                      }
                      img {
                        max-width: 100%;
                        height: auto;
                      }
                      pre {
                        background: #f4f4f5;
                        padding: 1em;
                        border-radius: 4px;
                        overflow-x: auto;
                      }
                      code {
                        background: #f4f4f5;
                        padding: 0.2em 0.4em;
                        border-radius: 3px;
                        font-size: 0.9em;
                      }
                      blockquote {
                        border-left: 4px solid #e5e7eb;
                        padding-left: 1em;
                        margin: 1em 0;
                        color: #6b7280;
                      }
                      ul, ol {
                        margin: 1em 0;
                        padding-left: 1.5em;
                      }
                      li {
                        margin: 0.5em 0;
                      }
                    `,
                    branding: false,
                    promotion: false,
                    // Security: Convert unsafe embed/object elements to safer alternatives (CVE-2024-29881)
                    convert_unsafe_embeds: true,
                    // Image upload handler - uses unified media endpoint
                    images_upload_handler: async (blobInfo) => {
                      const formData = new FormData()
                      formData.append('file', blobInfo.blob(), blobInfo.filename())

                      const response = await fetch('/api/media/upload?folder=legal', {
                        method: 'POST',
                        body: formData,
                        credentials: 'include',
                      })

                      if (!response.ok) {
                        throw new Error('Upload failed')
                      }

                      const { location } = await response.json()
                      return location
                    },
                    automatic_uploads: true,
                    file_picker_types: 'image',
                  }}
                />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
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
            <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
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
