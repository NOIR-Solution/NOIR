import { useState, useEffect, useRef } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
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
} from 'lucide-react'
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
  getEmailTemplate,
  updateEmailTemplate,
  previewEmailTemplate,
  getDefaultSampleData,
  type EmailTemplateDto,
  type EmailPreviewResponse,
} from '@/services/emailTemplates'
import { ApiError } from '@/services/apiClient'
import { PreviewDialog } from './PreviewDialog'
import { TestEmailDialog } from './TestEmailDialog'

/**
 * Email Template Edit Page
 * Full editor with TinyMCE, variable insertion, preview and test email functionality.
 */
export default function EmailTemplateEditPage() {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const editorRef = useRef<TinyMCEEditor | null>(null)

  // Track editor initialization to prevent false "unsaved changes" from TinyMCE normalization
  const editorInitializedRef = useRef(false)
  const initialHtmlBodyRef = useRef<string | null>(null)

  // State
  const [template, setTemplate] = useState<EmailTemplateDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

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

  // Get template display name
  const getDisplayName = (name: string): string => {
    return name
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (str) => str.toUpperCase())
      .trim()
  }

  // Load template
  useEffect(() => {
    async function loadTemplate() {
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
        navigate('/portal/email-templates')
      } finally {
        setLoading(false)
      }
    }

    loadTemplate()
  }, [id, navigate, t])

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

  // Handle save
  const handleSave = async () => {
    if (!id || !template) return

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
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <div className="h-10 w-10 bg-muted rounded animate-pulse" />
          <div className="space-y-2">
            <div className="h-6 w-48 bg-muted rounded animate-pulse" />
            <div className="h-4 w-32 bg-muted rounded animate-pulse" />
          </div>
        </div>
        <div className="h-[600px] bg-muted rounded animate-pulse" />
      </div>
    )
  }

  if (!template) {
    return null
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" onClick={() => navigate('/portal/email-templates')}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-foreground flex items-center gap-2">
              {getDisplayName(template.name)}
              <Badge variant="secondary">{template.language.toUpperCase()}</Badge>
            </h1>
            <p className="text-muted-foreground">
              {template.description || t('emailTemplates.editTemplate')}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={handlePreview}>
            <Eye className="h-4 w-4 mr-2" />
            {t('emailTemplates.preview')}
          </Button>
          <Button variant="outline" onClick={() => setTestEmailOpen(true)}>
            <Send className="h-4 w-4 mr-2" />
            {t('emailTemplates.sendTestEmail')}
          </Button>
          <Button onClick={handleSave} disabled={!hasChanges || saving}>
            <Save className="h-4 w-4 mr-2" />
            {saving ? t('labels.loading') : t('buttons.save')}
          </Button>
        </div>
      </div>

      {/* Unsaved changes warning */}
      {hasChanges && (
        <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-3 text-sm text-yellow-800 dark:text-yellow-200">
          {t('messages.unsavedChanges')} Press Ctrl+S to save.
        </div>
      )}

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main Editor */}
        <div className="lg:col-span-2 space-y-6">
          {/* Subject */}
          <Card>
            <CardHeader className="pb-3">
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
                    onChange={(e) => setSubject(e.target.value)}
                    placeholder="Enter email subject..."
                    className="w-full"
                  />
                </div>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="outline" size="icon">
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
            </CardContent>
          </Card>

          {/* HTML Body Editor */}
          <Card>
            <CardHeader className="pb-3">
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
                onEditorChange={(content) => setHtmlBody(content)}
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
            </CardContent>
          </Card>

          {/* Plain Text Body (Collapsible) */}
          <Card>
            <CardHeader
              className="pb-3 cursor-pointer"
              onClick={() => setShowPlainText(!showPlainText)}
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <FileText className="h-4 w-4" />
                  <CardTitle className="text-base">{t('emailTemplates.plainTextBody')}</CardTitle>
                  <Badge variant="outline" className="text-xs">
                    Optional
                  </Badge>
                </div>
                {showPlainText ? (
                  <ChevronUp className="h-4 w-4" />
                ) : (
                  <ChevronDown className="h-4 w-4" />
                )}
              </div>
              <CardDescription>
                Fallback content for email clients that don't support HTML.
              </CardDescription>
            </CardHeader>
            {showPlainText && (
              <CardContent>
                <textarea
                  value={plainTextBody}
                  onChange={(e) => setPlainTextBody(e.target.value)}
                  placeholder="Enter plain text version of the email..."
                  className="w-full h-48 p-3 border rounded-lg resize-none font-mono text-sm bg-background text-foreground"
                />
              </CardContent>
            )}
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Template Info */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Template Info</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Name:</span>
                <span className="font-medium">{template.name}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Language:</span>
                <Badge variant="secondary">{template.language.toUpperCase()}</Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">{t('emailTemplates.version')}:</span>
                <span className="font-medium">{template.version}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Status:</span>
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
            </CardContent>
          </Card>

          {/* Available Variables */}
          <Card>
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
          <Card>
            <CardHeader>
              <CardTitle className="text-base">{t('labels.description')}</CardTitle>
            </CardHeader>
            <CardContent>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Template description..."
                className="w-full h-24 p-3 border rounded-lg resize-none text-sm bg-background text-foreground"
              />
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Preview Dialog */}
      <PreviewDialog
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
