import { useState, useEffect, useRef } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { Editor } from '@tinymce/tinymce-react'
import type { Editor as TinyMCEEditor } from 'tinymce'
import { z } from 'zod'
import { updateEmailTemplateSchema } from '@/validation/schemas.generated'
import { formatDisplayName } from '@/lib/utils'

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
import {
  ArrowLeft,
  Save,
  Eye,
  Send,
  Variable,
  ChevronDown,
  FileText,
  ChevronUp,
  GripVertical,
  GitFork,
  Info,
} from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Input,
  Label,
  Skeleton,
  Switch,
} from '@uikit'

import {
  getEmailTemplate,
  updateEmailTemplate,
  previewEmailTemplate,
  revertToPlatformDefault,
  toggleEmailTemplateActive,
  getDefaultSampleData,
  type EmailTemplateDto,
  type EmailPreviewResponse,
} from '@/services/emailTemplates'
import { ApiError } from '@/services/apiClient'
import { EmailPreviewDialog } from '../../components/tenant-settings/EmailPreviewDialog'
import { TestEmailDialog } from '../../components/tenant-settings/TestEmailDialog'

// Extended schema for form validation (includes plainTextBody which is optional)
const emailTemplateFormSchema = updateEmailTemplateSchema.omit({ id: true }).extend({
  plainTextBody: z.string().optional().or(z.literal('')),
})

type EmailTemplateFormErrors = {
  subject?: string
  htmlBody?: string
  plainTextBody?: string
  description?: string
}

/**
 * Email Template Edit Page
 * Full editor with TinyMCE, variable insertion, preview and test email functionality.
 */
export const EmailTemplateEditPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const [searchParams, setSearchParams] = useSearchParams()
  const fromContext = searchParams.get('from')
  const settingsBackUrl = fromContext === 'platform'
    ? '/portal/admin/platform-settings?tab=emailTemplates'
    : '/portal/admin/tenant-settings?tab=emailTemplates'
  const editorRef = useRef<TinyMCEEditor | null>(null)

  // Track editor initialization to prevent false "unsaved changes" from TinyMCE normalization
  const editorInitializedRef = useRef(false)
  const initialHtmlBodyRef = useRef<string | null>(null)
  // Ref to handlePreview for use in useEffect without dependency cycle
  const handlePreviewRef = useRef<(() => Promise<void>) | null>(null)

  // State
  const [template, setTemplate] = useState<EmailTemplateDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [toggling, setToggling] = useState(false)

  // Form state
  const [subject, setSubject] = useState('')
  const [htmlBody, setHtmlBody] = useState('')
  const [plainTextBody, setPlainTextBody] = useState('')
  const [description, setDescription] = useState('')
  const [showPlainText, setShowPlainText] = useState(false)

  // Preview dialog state
  const [previewOpen, setPreviewOpen] = useState(false)
  const [previewData, setPreviewData] = useState<EmailPreviewResponse | null>(null)
  const [previewLoading, setPreviewLoading] = useState(false)

  // Test email dialog state
  const [testEmailOpen, setTestEmailOpen] = useState(false)

  // Track unsaved changes
  const [hasChanges, setHasChanges] = useState(false)

  // Validation errors
  const [errors, setErrors] = useState<EmailTemplateFormErrors>({})

  // Load template
  useEffect(() => {
    const loadTemplate = async () => {
      if (!id) return

      // Reset editor tracking when loading new template
      editorInitializedRef.current = false
      initialHtmlBodyRef.current = null

      setLoading(true)
      try {
        const data = await getEmailTemplate(id)
        setTemplate(data)
        setSubject(data.subject)
        setHtmlBody(data.htmlBody)
        setPlainTextBody(data.plainTextBody || '')
        setDescription(data.description || '')
      } catch (error) {
        if (error instanceof ApiError) {
          toast.error(error.message)
        } else {
          toast.error(t('messages.operationFailed'))
        }
        navigate(settingsBackUrl)
      } finally {
        setLoading(false)
      }
    }

    loadTemplate()
  }, [id, navigate, t, settingsBackUrl])

  // Auto-open preview when navigated with ?mode=preview
  useEffect(() => {
    if (!template || loading) return

    const mode = searchParams.get('mode')
    if (mode === 'preview') {
      // Clear the mode parameter from URL to prevent re-triggering
      setSearchParams(prev => {
        prev.delete('mode')
        return prev
      }, { replace: true })

      // Trigger preview
      handlePreviewRef.current?.()
    }
  }, [template, loading, searchParams, setSearchParams])

  // Track changes - compare against normalized initial values after TinyMCE initialization
  useEffect(() => {
    if (!template) return
    // Don't track changes until editor has initialized (to avoid false positives from TinyMCE normalization)
    if (!editorInitializedRef.current) return

    const changed =
      subject !== template.subject ||
      htmlBody !== (initialHtmlBodyRef.current ?? template.htmlBody) ||
      plainTextBody !== (template.plainTextBody || '') ||
      description !== (template.description || '')
    setHasChanges(changed)
  }, [subject, htmlBody, plainTextBody, description, template])

  // Handle save with validation
  const handleSave = async () => {
    if (!id || !template) return

    // Validate form data using Zod schema
    const result = emailTemplateFormSchema.safeParse({
      subject,
      htmlBody,
      plainTextBody,
      description,
    })

    if (!result.success) {
      // Map Zod errors to form errors
      const fieldErrors: EmailTemplateFormErrors = {}
      result.error.issues.forEach((issue) => {
        const field = issue.path[0] as keyof EmailTemplateFormErrors
        if (field) {
          fieldErrors[field] = issue.message
        }
      })
      setErrors(fieldErrors)
      return
    }

    // Clear validation errors
    setErrors({})

    setSaving(true)
    try {
      const updated = await updateEmailTemplate(id, {
        subject,
        htmlBody,
        plainTextBody: plainTextBody || null,
        description: description || null,
      })
      setTemplate(updated)
      // Update the initial reference to the current content after successful save
      initialHtmlBodyRef.current = htmlBody
      setHasChanges(false)
      toast.success(t('messages.updateSuccess'))
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

  // Handle preview
  const handlePreview = async () => {
    if (!id || !template) return

    setPreviewLoading(true)
    setPreviewOpen(true)
    try {
      const sampleData = getDefaultSampleData(template.availableVariables)
      const preview = await previewEmailTemplate(id, { sampleData })
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

  // Assign to ref for use in auto-preview useEffect
  handlePreviewRef.current = handlePreview

  // Handle revert to platform default
  const handleRevert = async () => {
    if (!id || !template) return

    if (!confirm(t('emailTemplates.revertConfirm'))) {
      return
    }

    setSaving(true)
    try {
      const reverted = await revertToPlatformDefault(id)
      setTemplate(reverted)
      setSubject(reverted.subject)
      setHtmlBody(reverted.htmlBody)
      setPlainTextBody(reverted.plainTextBody || '')
      setDescription(reverted.description || '')
      setHasChanges(false)
      toast.success(t('emailTemplates.revertSuccess'))
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

  // Toggle active status
  const handleToggleActive = async (isActive: boolean) => {
    if (!id || !template) return

    setToggling(true)
    try {
      const updated = await toggleEmailTemplateActive(id, isActive)
      setTemplate(updated)
      toast.success(isActive ? t('emailTemplates.templateActivated') : t('emailTemplates.templateDeactivated'))
    } catch (error) {
      if (error instanceof ApiError) {
        toast.error(error.message)
      } else {
        toast.error(t('emailTemplates.toggleStatusFailed'))
      }
    } finally {
      setToggling(false)
    }
  }

  // Insert variable into editor
  const insertVariable = (variable: string) => {
    const variableText = `{{${variable}}}`
    if (editorRef.current) {
      editorRef.current.insertContent(variableText)
    }
  }

  // Insert variable into subject
  const insertVariableIntoSubject = (variable: string) => {
    const variableText = `{{${variable}}}`
    const input = document.getElementById('subject-input') as HTMLInputElement
    if (input) {
      const start = input.selectionStart || 0
      const end = input.selectionEnd || 0
      const newValue = subject.slice(0, start) + variableText + subject.slice(end)
      setSubject(newValue)
      // Restore focus and cursor position
      setTimeout(() => {
        input.focus()
        input.setSelectionRange(start + variableText.length, start + variableText.length)
      }, 0)
    }
  }

  // Keyboard shortcut for save
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault()
        if (hasChanges && !saving) {
          handleSave()
        }
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [hasChanges, saving])

  if (loading) {
    return (
      <div className="container max-w-6xl py-6 space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded" />
          <div className="space-y-2">
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-32" />
          </div>
        </div>
        <div className="grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-2 space-y-6">
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6">
                <Skeleton className="h-4 w-24 mb-3" />
                <Skeleton className="h-10 w-full" />
              </CardContent>
            </Card>
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6">
                <Skeleton className="h-4 w-32 mb-3" />
                <Skeleton className="h-[400px] w-full rounded" />
              </CardContent>
            </Card>
          </div>
          <div className="space-y-6">
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6 space-y-3">
                <Skeleton className="h-4 w-32 mb-3" />
                <Skeleton className="h-3 w-full" />
                <Skeleton className="h-3 w-full" />
                <Skeleton className="h-3 w-2/3" />
              </CardContent>
            </Card>
            <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="pt-6 space-y-3">
                <Skeleton className="h-4 w-36 mb-3" />
                <Skeleton className="h-3 w-full" />
                <Skeleton className="h-3 w-full" />
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    )
  }

  if (!template) {
    return null
  }

  return (
    <div className="container max-w-6xl py-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" className="cursor-pointer" onClick={() => navigate(settingsBackUrl)} aria-label={t('buttons.back')}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
              {formatDisplayName(template.name)}
              <Badge variant="secondary">HTML</Badge>
            </h1>
            <p className="text-muted-foreground">
              {template.description || t('emailTemplates.editTemplate')}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" className="cursor-pointer" onClick={handlePreview}>
            <Eye className="h-4 w-4 mr-2" />
            {t('emailTemplates.preview')}
          </Button>
          <Button variant="outline" className="cursor-pointer" onClick={() => setTestEmailOpen(true)}>
            <Send className="h-4 w-4 mr-2" />
            {t('emailTemplates.sendTestEmail')}
          </Button>
          {template.isInherited === false && (
            <Button variant="outline" className="cursor-pointer" onClick={handleRevert} disabled={saving}>
              <GitFork className="h-4 w-4 mr-2" />
              {t('buttons.revert')}
            </Button>
          )}
          <Button className="cursor-pointer" onClick={handleSave} disabled={!hasChanges || saving}>
            <Save className="h-4 w-4 mr-2" />
            {saving ? t('labels.loading') : t('buttons.save')}
          </Button>
        </div>
      </div>

      {/* Inherited template notice */}
      {template.isInherited && (
        <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg p-3 text-sm text-purple-800 dark:text-purple-200 flex items-start gap-3">
          <Info className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-medium">{t('emailTemplates.customizingPlatformTemplate')}</p>
            <p className="text-purple-600 dark:text-purple-300 mt-1">
              {t('emailTemplates.platformTemplateNotice')}
            </p>
          </div>
        </div>
      )}

      {/* Unsaved changes warning */}
      {hasChanges && (
        <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-3 text-sm text-yellow-800 dark:text-yellow-200">
          {t('messages.unsavedChanges')} {t('emailTemplates.pressCtrlS')}
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main Editor */}
        <div className="lg:col-span-2 space-y-6">
          {/* Subject */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader>
              <CardTitle className="text-base">{t('emailTemplates.subject')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex gap-2">
                <div
                  className="flex-1"
                  onDragOver={(e) => {
                    e.preventDefault()
                    e.dataTransfer.dropEffect = 'copy'
                  }}
                  onDrop={(e) => {
                    e.preventDefault()
                    const variableData = e.dataTransfer.getData('text/variable')
                    if (variableData) {
                      const input = document.getElementById('subject-input') as HTMLInputElement
                      const start = input?.selectionStart || subject.length
                      const newValue = subject.slice(0, start) + `{{${variableData}}}` + subject.slice(start)
                      setSubject(newValue)
                    }
                  }}
                >
                  <Input
                    id="subject-input"
                    value={subject}
                    onChange={(e) => {
                      setSubject(e.target.value)
                      if (errors.subject) setErrors((prev) => ({ ...prev, subject: undefined }))
                    }}
                    placeholder={t('emailTemplates.subjectPlaceholder')}
                    aria-label={t('emailTemplates.emailSubject', 'Email subject')}
                    className={`w-full ${errors.subject ? 'border-destructive' : ''}`}
                    aria-invalid={!!errors.subject}
                  />
                </div>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" size="icon" aria-label={t('emailTemplates.insertVariableIntoSubject', 'Insert variable into subject')}>
                      <Variable className="h-4 w-4" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    {template.availableVariables.map((variable) => (
                      <DropdownMenuItem
                        key={variable}
                        onClick={() => insertVariableIntoSubject(variable)}
                      >
                        <code className="text-xs">{`{{${variable}}}`}</code>
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
              {errors.subject && (
                <p className="text-sm font-medium text-destructive">{errors.subject}</p>
              )}
            </CardContent>
          </Card>

          {/* HTML Body Editor */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="text-base">{t('emailTemplates.htmlBody')}</CardTitle>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" size="sm">
                      <Variable className="h-4 w-4 mr-2" />
                      {t('emailTemplates.insertVariable')}
                      <ChevronDown className="h-4 w-4 ml-2" />
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end">
                    {template.availableVariables.map((variable) => (
                      <DropdownMenuItem key={variable} onClick={() => insertVariable(variable)}>
                        <code className="text-xs">{`{{${variable}}}`}</code>
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </CardHeader>
            <CardContent>
              <Editor
                onInit={(_evt, editor) => {
                  editorRef.current = editor
                  // Capture the normalized HTML after TinyMCE initialization
                  // Use setTimeout to ensure we get the fully normalized content
                  setTimeout(() => {
                    initialHtmlBodyRef.current = editor.getContent()
                    editorInitializedRef.current = true
                  }, 100)
                }}
                value={htmlBody}
                onEditorChange={(content) => {
                  setHtmlBody(content)
                  if (errors.htmlBody) setErrors((prev) => ({ ...prev, htmlBody: undefined }))
                }}
                init={{
                  height: 500,
                  menubar: false,
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
                    'link image table | code fullscreen preview',
                  content_style: `
                    body {
                      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
                      font-size: 14px;
                      line-height: 1.6;
                      color: #333;
                      padding: 10px;
                    }
                  `,
                  branding: false,
                  promotion: false,
                  // Security: Convert unsafe embed/object elements to safer alternatives (CVE-2024-29881)
                  convert_unsafe_embeds: true,
                  // Setup autocomplete for variables when typing {{
                  setup: (editor) => {
                    // Register autocompleter for {{ trigger with CardMenuItem for better UX
                    editor.ui.registry.addAutocompleter('variables', {
                      trigger: '{{',
                      minChars: 0,
                      columns: 1,
                      highlightOn: ['variable_name'],
                      fetch: (pattern) => {
                        const variables = template?.availableVariables || []
                        const filtered = variables.filter((v) =>
                          v.toLowerCase().includes(pattern.toLowerCase())
                        )
                        return Promise.resolve(
                          filtered.map((variable) => ({
                            type: 'cardmenuitem' as const,
                            value: `{{${variable}}}`,
                            label: variable,
                            items: [
                              {
                                type: 'cardcontainer',
                                direction: 'horizontal',
                                align: 'left',
                                valign: 'middle',
                                items: [
                                  {
                                    type: 'cardtext',
                                    text: variable,
                                    name: 'variable_name',
                                    classes: ['tox-collection__item-label'],
                                  },
                                ],
                              },
                            ],
                          }))
                        )
                      },
                      onAction: (autocompleteApi, rng, value) => {
                        editor.selection.setRng(rng)
                        editor.insertContent(value)
                        autocompleteApi.hide()
                      },
                    })

                    // Handle drag & drop of variables
                    editor.on('drop', (e) => {
                      const variableData = e.dataTransfer?.getData('text/variable')
                      if (variableData) {
                        e.preventDefault()
                        editor.insertContent(`{{${variableData}}}`)
                      }
                    })
                  },
                }}
              />
              {errors.htmlBody && (
                <p className="text-sm font-medium text-destructive mt-2">{errors.htmlBody}</p>
              )}
            </CardContent>
          </Card>

          {/* Plain Text Body (Collapsible) */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader
              className="cursor-pointer"
              onClick={() => setShowPlainText(!showPlainText)}
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <FileText className="h-4 w-4" />
                  <CardTitle className="text-base">{t('emailTemplates.plainTextBody')}</CardTitle>
                  <Badge variant="outline" className="text-xs">
                    {t('emailTemplates.optional')}
                  </Badge>
                </div>
                {showPlainText ? (
                  <ChevronUp className="h-4 w-4" />
                ) : (
                  <ChevronDown className="h-4 w-4" />
                )}
              </div>
              <CardDescription>
                {t('emailTemplates.fallbackContent')}
              </CardDescription>
            </CardHeader>
            {showPlainText && (
              <CardContent>
                <textarea
                  value={plainTextBody}
                  onChange={(e) => setPlainTextBody(e.target.value)}
                  placeholder={t('emailTemplates.plainTextPlaceholder')}
                  aria-label={t('emailTemplates.plainTextBodyAriaLabel', 'Plain text email body')}
                  className="w-full h-48 p-3 border rounded-lg resize-none font-mono text-sm bg-background text-foreground"
                />
              </CardContent>
            )}
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Template Info */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader>
              <CardTitle className="text-base">{t('emailTemplates.templateInfo')}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('emailTemplates.name')}:</span>
                <span className="font-medium">{formatDisplayName(template.name)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('emailTemplates.language')}:</span>
                <Badge variant="secondary">HTML</Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('emailTemplates.version')}:</span>
                <span className="font-medium">{template.version}</span>
              </div>
              <div className="flex items-center justify-between">
                <Label htmlFor="template-active" className="text-muted-foreground cursor-pointer">
                  {t('emailTemplates.status')}:
                </Label>
                <div className="flex items-center gap-2">
                  <span className={`text-xs font-medium ${template.isActive ? 'text-green-600' : 'text-muted-foreground'}`}>
                    {template.isActive ? t('emailTemplates.active') : t('emailTemplates.inactive')}
                  </span>
                  <Switch
                    id="template-active"
                    checked={template.isActive}
                    onCheckedChange={handleToggleActive}
                    disabled={toggling}
                    className="cursor-pointer"
                  />
                </div>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('emailTemplates.source')}:</span>
                {template.isInherited ? (
                  <Badge variant="outline" className="text-purple-600 border-purple-600/30">
                    <GitFork className="h-3 w-3 mr-1" />
                    {t('emailTemplates.platform')}
                  </Badge>
                ) : (
                  <Badge variant="outline" className="text-blue-600 border-blue-600/30">
                    {t('emailTemplates.custom')}
                  </Badge>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Available Variables */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader>
              <CardTitle className="text-base">{t('emailTemplates.variables')}</CardTitle>
              <CardDescription>
                {t('emailTemplates.variablesHint', 'Drag to editor or click to copy. Type {{ in editor for autocomplete.')}
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                {template.availableVariables.map((variable) => (
                  <div
                    key={variable}
                    draggable
                    onDragStart={(e) => {
                      e.dataTransfer.setData('text/variable', variable)
                      e.dataTransfer.setData('text/plain', `{{${variable}}}`)
                      e.dataTransfer.effectAllowed = 'copy'
                    }}
                    className="flex items-center gap-1 group"
                  >
                    <div className="p-1 cursor-grab text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity">
                      <GripVertical className="h-4 w-4" />
                    </div>
                    <Button
                      variant="outline"
                      className="flex-1 justify-start font-mono text-xs cursor-grab active:cursor-grabbing"
                      onClick={() => {
                        navigator.clipboard.writeText(`{{${variable}}}`)
                        toast.success(t('messages.copySuccess'))
                      }}
                    >
                      {`{{${variable}}}`}
                    </Button>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* Description */}
          <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
            <CardHeader>
              <CardTitle className="text-base">{t('labels.description')}</CardTitle>
            </CardHeader>
            <CardContent>
              <textarea
                value={description}
                onChange={(e) => {
                  setDescription(e.target.value)
                  if (errors.description) setErrors((prev) => ({ ...prev, description: undefined }))
                }}
                placeholder={t('emailTemplates.descriptionPlaceholder')}
                aria-label={t('emailTemplates.templateDescription', 'Template description')}
                className={`w-full h-24 p-3 border rounded-lg resize-none text-sm bg-background text-foreground ${errors.description ? 'border-destructive' : ''}`}
              />
              {errors.description && (
                <p className="text-sm font-medium text-destructive mt-2">{errors.description}</p>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Preview Dialog */}
      <EmailPreviewDialog
        open={previewOpen}
        onOpenChange={setPreviewOpen}
        preview={previewData}
        loading={previewLoading}
      />

      {/* Test Email Dialog */}
      <TestEmailDialog
        open={testEmailOpen}
        onOpenChange={setTestEmailOpen}
        templateId={id!}
        availableVariables={template.availableVariables}
      />
    </div>
  )
}

export default EmailTemplateEditPage
