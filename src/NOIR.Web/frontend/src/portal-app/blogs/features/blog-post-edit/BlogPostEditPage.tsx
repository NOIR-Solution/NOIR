import { useState, useEffect, useRef } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { FileText, ArrowLeft, Save, Upload, X, Image as ImageIcon, Loader2, Calendar, Info } from 'lucide-react'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
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

import { usePageContext } from '@/hooks/usePageContext'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  DatePicker,
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Label,
  RadioGroup,
  RadioGroupItem,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
  Textarea,
  TimePicker,
} from '@uikit'

import { toast } from 'sonner'
import { useTranslation } from 'react-i18next'
import { getPostById, createPost, updatePost, publishPost, unpublishPost } from '@/services/blog'

import { uploadMedia } from '@/services/media'
import { useBlogCategoriesQuery, useBlogTagsQuery } from '@/portal-app/blogs/queries'
import { ApiError } from '@/services/apiClient'
import type { Post, CreatePostRequest } from '@/types'

const createFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    title: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { count: 200 })),
    slug: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { count: 200 })).regex(/^[a-z0-9-]+$/, t('validation.identifierFormat')),
    excerpt: z.string().max(500, t('validation.maxLength', { count: 500 })).optional(),
    categoryId: z.string().optional(),
    tagIds: z.array(z.string()).optional(),
    metaTitle: z.string().max(60, t('validation.maxLength', { count: 60 })).optional(),
    metaDescription: z.string().max(160, t('validation.maxLength', { count: 160 })).optional(),
    canonicalUrl: z.string().url(t('validation.invalidFormat')).optional().or(z.literal('')),
    allowIndexing: z.boolean().default(true),
    featuredImageId: z.string().optional(),
    featuredImageUrl: z.string().optional(),
    featuredImageAlt: z.string().max(200, t('validation.maxLength', { count: 200 })).optional(),
  })

type FormValues = z.output<ReturnType<typeof createFormSchema>>

export const BlogPostEditPage = () => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const isEdit = !!id
  usePageContext(isEdit ? 'Edit Post' : 'New Post')
  const { formatDateTime, formatDate } = useRegionalSettings()
  const editorRef = useRef<TinyMCEEditor | null>(null)

  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [uploadingImage, setUploadingImage] = useState(false)
  const [post, setPost] = useState<Post | null>(null)
  const [contentHtml, setContentHtml] = useState('')
  const fileInputRef = useRef<HTMLInputElement>(null)

  // Publishing options state
  type PublishOption = 'draft' | 'publish' | 'schedule'
  const [publishOption, setPublishOption] = useState<PublishOption>('draft')
  const [scheduledDate, setScheduledDate] = useState<Date | undefined>(undefined)
  const [scheduledTime, setScheduledTime] = useState('09:00')

  const { data: categories = [] } = useBlogCategoriesQuery({})
  const { data: tags = [] } = useBlogTagsQuery({})

  const form = useForm<FormValues>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createFormSchema(t)) as unknown as Resolver<FormValues>,
    mode: 'onBlur',
    defaultValues: {
      title: '',
      slug: '',
      excerpt: '',
      categoryId: '',
      tagIds: [],
      metaTitle: '',
      metaDescription: '',
      canonicalUrl: '',
      allowIndexing: true,
      featuredImageId: '',
      featuredImageUrl: '',
      featuredImageAlt: '',
    },
  })

  // Load post data if editing
  useEffect(() => {
    if (isEdit && id) {
      setLoading(true)
      getPostById(id)
        .then((data) => {
          setPost(data)
          form.reset({
            title: data.title,
            slug: data.slug,
            excerpt: data.excerpt || '',
            categoryId: data.categoryId || '',
            tagIds: data.tags?.map((t) => t.id) || [],
            metaTitle: data.metaTitle || '',
            metaDescription: data.metaDescription || '',
            canonicalUrl: data.canonicalUrl || '',
            allowIndexing: data.allowIndexing,
            featuredImageId: data.featuredImageId || '',
            featuredImageUrl: data.featuredImageUrl || '',
            featuredImageAlt: data.featuredImageAlt || '',
          })
          // Load HTML content
          setContentHtml(data.contentHtml || '')

          // Set initial publish option based on post status
          if (data.status === 'Published') {
            setPublishOption('publish')
          } else if (data.status === 'Scheduled' && data.scheduledPublishAt) {
            setPublishOption('schedule')
            const scheduleDate = new Date(data.scheduledPublishAt)
            setScheduledDate(scheduleDate)
            setScheduledTime(scheduleDate.toTimeString().slice(0, 5))
          } else {
            setPublishOption('draft')
          }
        })
        .catch((err) => {
          const message = err instanceof ApiError ? err.message : t('blog.failedToLoad')
          toast.error(message)
          navigate('/portal/blog/posts')
        })
        .finally(() => setLoading(false))
    }
  }, [id, isEdit, form, navigate])

  // Auto-generate slug from title
  const watchTitle = form.watch('title')
  useEffect(() => {
    if (!isEdit && watchTitle) {
      const slug = watchTitle
        .toLowerCase()
        .replace(/[^a-z0-9\s-]/g, '')
        .replace(/\s+/g, '-')
        .replace(/-+/g, '-')
        .slice(0, 200)
      form.setValue('slug', slug)
    }
  }, [watchTitle, isEdit, form])

  const handleSave = async (values: FormValues) => {
    // Validate schedule date if scheduling
    if (publishOption === 'schedule') {
      if (!scheduledDate) {
        toast.error(t('blog.pleaseSelectDate'))
        return
      }
      const [hours, minutes] = scheduledTime.split(':').map(Number)
      const scheduledDateTime = new Date(scheduledDate)
      scheduledDateTime.setHours(hours, minutes, 0, 0)
      if (scheduledDateTime <= new Date()) {
        toast.error(t('blog.scheduleMustBeFuture'))
        return
      }
    }

    setSaving(true)

    try {
      const request: CreatePostRequest = {
        title: values.title,
        slug: values.slug,
        excerpt: values.excerpt || undefined,
        contentJson: undefined, // No longer using BlockNote JSON
        contentHtml: contentHtml || undefined,
        categoryId: values.categoryId || undefined,
        tagIds: values.tagIds?.length ? values.tagIds : undefined,
        metaTitle: values.metaTitle || undefined,
        metaDescription: values.metaDescription || undefined,
        canonicalUrl: values.canonicalUrl || undefined,
        allowIndexing: values.allowIndexing,
        featuredImageId: values.featuredImageId || undefined,
        featuredImageUrl: values.featuredImageUrl || undefined,
        featuredImageAlt: values.featuredImageAlt || undefined,
      }

      let savedPost: Post
      if (isEdit && id) {
        savedPost = await updatePost(id, request)
      } else {
        savedPost = await createPost(request)
      }

      // Handle publish/unpublish based on selected option
      if (publishOption === 'draft') {
        // If post was published or scheduled, unpublish it
        if (savedPost.status === 'Published' || savedPost.status === 'Scheduled') {
          await unpublishPost(savedPost.id)
          toast.success(t('blog.savedAsDraft'))
        } else {
          toast.success(isEdit ? t('blog.postSaved') : t('blog.postCreated'))
        }
      } else if (publishOption === 'publish') {
        // Publish immediately
        if (savedPost.status !== 'Published') {
          await publishPost(savedPost.id)
          toast.success(t('blog.postPublished'))
        } else {
          toast.success(t('blog.postSaved'))
        }
      } else if (publishOption === 'schedule') {
        // Schedule for future
        const [hours, minutes] = scheduledTime.split(':').map(Number)
        const scheduledDateTime = new Date(scheduledDate!)
        scheduledDateTime.setHours(hours, minutes, 0, 0)
        await publishPost(savedPost.id, { scheduledPublishAt: scheduledDateTime.toISOString() })
        toast.success(t('blog.postScheduled', { date: formatDateTime(scheduledDateTime) }))
      }

      navigate('/portal/blog/posts')
    } catch (err) {
      const message = err instanceof ApiError ? err.message : t('blog.failedToSave')
      toast.error(message)
    } finally {
      setSaving(false)
    }
  }

  const onSubmit = (values: FormValues) => handleSave(values)

  // Handle featured image upload
  const handleImageUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    // Validate file type
    if (!file.type.startsWith('image/')) {
      toast.error(t('blog.selectImageFile'))
      return
    }

    // Validate file size (max 10MB)
    if (file.size > 10 * 1024 * 1024) {
      toast.error(t('blog.imageTooLarge'))
      return
    }

    setUploadingImage(true)
    try {
      const result = await uploadMedia(file, 'blog')
      if (result.success && result.mediaFileId) {
        form.setValue('featuredImageId', result.mediaFileId)
        form.setValue('featuredImageUrl', result.defaultUrl || result.location || '')
        toast.success(t('blog.imageUploaded'))
      } else {
        toast.error(result.error || t('blog.failedToUpload'))
      }
    } catch (err) {
      toast.error(t('blog.failedToUpload'))
    } finally {
      setUploadingImage(false)
      // Reset file input
      if (fileInputRef.current) {
        fileInputRef.current.value = ''
      }
    }
  }

  // Clear featured image
  const handleClearImage = () => {
    form.setValue('featuredImageId', '')
    form.setValue('featuredImageUrl', '')
    form.setValue('featuredImageAlt', '')
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-muted-foreground">{t('labels.loading')}</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="icon" onClick={() => navigate('/portal/blog/posts')}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div className="p-2 bg-primary/10 rounded-lg">
            <FileText className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">
              {isEdit ? t('blog.editPost') : t('blog.newPost')}
            </h1>
            <p className="text-muted-foreground">
              {isEdit ? t('blog.editing', { title: post?.title || t('labels.loading') }) : t('blog.createNewPost')}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {post?.status && (
            <Badge variant={
              post.status === 'Published' ? 'default' :
              post.status === 'Scheduled' ? 'secondary' : 'outline'
            }>
              {post.status === 'Scheduled' && post.scheduledPublishAt
                ? t('blog.scheduledLabel', { date: formatDate(post.scheduledPublishAt!) })
                : t(`blog.status.${post.status.toLowerCase()}`)}
            </Badge>
          )}
          <Button onClick={() => form.handleSubmit(onSubmit)()} disabled={saving}>
            {saving ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                {t('buttons.saving')}
              </>
            ) : (
              <>
                <Save className="h-4 w-4 mr-2" />
                {t('buttons.save')}
              </>
            )}
          </Button>
        </div>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Main Content Area */}
            <div className="lg:col-span-2 space-y-6">
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader>
                  <CardTitle>{t('blog.content', 'Content')}</CardTitle>
                  <CardDescription>{t('blog.contentDescription', 'Write your blog post content')}</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="title"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('blog.titleColumn', 'Title')}</FormLabel>
                        <FormControl>
                          <Input placeholder={t('blog.enterPostTitle')} className="text-lg" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="slug"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('labels.slug')}</FormLabel>
                        <FormControl>
                          <Input placeholder={t('blog.slugPlaceholder', 'post-url-slug')} {...field} />
                        </FormControl>
                        <FormDescription>
                          URL: /blog/{field.value || 'post-slug'}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="excerpt"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('blog.excerpt', 'Excerpt')}</FormLabel>
                        <FormControl>
                          <Textarea
                            placeholder={t('blog.postSummary')}
                            rows={3}
                            {...field}
                          />
                        </FormControl>
                        <FormDescription>
                          {t('blog.excerptDescription')}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div>
                    <FormLabel className="mb-2 block">{t('blog.contentLabel')}</FormLabel>
                    <Editor
                      onInit={(_evt, editor) => {
                        editorRef.current = editor
                      }}
                      value={contentHtml}
                      onEditorChange={(content) => setContentHtml(content)}
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
                        `,
                        branding: false,
                        promotion: false,
                        // Security: Convert unsafe embed/object elements to safer alternatives (CVE-2024-29881)
                        convert_unsafe_embeds: true,
                        // Image upload handler - uses unified media endpoint
                        images_upload_handler: async (blobInfo) => {
                          const formData = new FormData()
                          formData.append('file', blobInfo.blob(), blobInfo.filename())

                          const response = await fetch('/api/media/upload?folder=blog', {
                            method: 'POST',
                            body: formData,
                            credentials: 'include',
                          })

                          if (!response.ok) {
                            throw new Error('Upload failed')
                          }

                          // Response includes location (alias for defaultUrl) for TinyMCE compatibility
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
            <div className="space-y-6">
              {/* Publishing Options */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <Calendar className="h-4 w-4" />
                    {t('blog.publishing')}
                  </CardTitle>
                  <CardDescription>{t('blog.publishingDescription')}</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3">
                  <RadioGroup value={publishOption} onValueChange={(v) => setPublishOption(v as PublishOption)} className="space-y-2">
                    <label
                      htmlFor="draft"
                      className="flex items-center gap-3 rounded-lg border p-3 hover:bg-accent/50 cursor-pointer transition-colors"
                    >
                      <RadioGroupItem value="draft" id="draft" />
                      <div className="space-y-0.5">
                        <span className="font-medium text-sm">{t('blog.saveAsDraft')}</span>
                        <p className="text-xs text-muted-foreground">
                          {t('blog.draftDescription')}
                        </p>
                      </div>
                    </label>

                    <label
                      htmlFor="publish"
                      className="flex items-center gap-3 rounded-lg border p-3 hover:bg-accent/50 cursor-pointer transition-colors"
                    >
                      <RadioGroupItem value="publish" id="publish" />
                      <div className="space-y-0.5">
                        <span className="font-medium text-sm">{t('blog.publishNow')}</span>
                        <p className="text-xs text-muted-foreground">
                          {t('blog.publishDescription')}
                        </p>
                      </div>
                    </label>

                    <label
                      htmlFor="schedule"
                      className="flex items-center gap-3 rounded-lg border p-3 hover:bg-accent/50 cursor-pointer transition-colors"
                    >
                      <RadioGroupItem value="schedule" id="schedule" />
                      <div className="space-y-0.5">
                        <span className="font-medium text-sm">{t('blog.schedule')}</span>
                        <p className="text-xs text-muted-foreground">
                          {t('blog.scheduleDescription')}
                        </p>
                      </div>
                    </label>
                  </RadioGroup>

                  {/* Schedule date/time picker */}
                  {publishOption === 'schedule' && (
                    <div className="pt-4 mt-2 border-t space-y-4">
                      <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <Label className="text-sm font-medium">{t('labels.date')}</Label>
                          <DatePicker
                            value={scheduledDate}
                            onChange={setScheduledDate}
                            minDate={new Date()}
                            placeholder={t('blog.selectDate')}
                          />
                        </div>
                        <div className="space-y-2">
                          <Label className="text-sm font-medium">{t('labels.time')}</Label>
                          <TimePicker
                            value={scheduledTime}
                            onChange={(time) => setScheduledTime(time)}
                            placeholder={t('blog.selectTime')}
                            interval={30}
                          />
                        </div>
                      </div>
                      <p className="text-xs text-muted-foreground flex items-center gap-1.5">
                        <Info className="h-3.5 w-3.5 flex-shrink-0" />
                        {t('blog.localTimezone')}
                      </p>
                    </div>
                  )}

                  {/* Status info for existing posts */}
                  {post && post.status !== 'Draft' && publishOption === 'draft' && (
                    <div className="p-3 rounded-md bg-amber-50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800">
                      <p
                        className="text-sm text-amber-800 dark:text-amber-200"
                        dangerouslySetInnerHTML={{ __html: t('blog.currentlyStatus', { status: post.status }) }}
                      />
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Organization */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader>
                  <CardTitle>{t('blog.organization')}</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="categoryId"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('labels.category')}</FormLabel>
                        <Select
                          onValueChange={(value) => field.onChange(value === '__none__' ? '' : value)}
                          value={field.value || '__none__'}
                        >
                          <FormControl>
                            <SelectTrigger className="cursor-pointer" aria-label={t('blog.selectPostCategory', 'Select blog post category')}>
                              <SelectValue placeholder={t('blog.selectCategory')} />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value="__none__" className="cursor-pointer">{t('blog.noCategory')}</SelectItem>
                            {categories.map((cat) => (
                              <SelectItem key={cat.id} value={cat.id} className="cursor-pointer">
                                {cat.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="tagIds"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('labels.tags')}</FormLabel>
                        <div className="flex flex-wrap gap-2 p-2 border rounded-md min-h-[40px]">
                          {tags.map((tag) => {
                            const isSelected = field.value?.includes(tag.id)
                            return (
                              <Badge
                                key={tag.id}
                                variant={isSelected ? 'default' : 'outline'}
                                className="cursor-pointer"
                                style={isSelected && tag.color ? { backgroundColor: tag.color } : undefined}
                                onClick={() => {
                                  const current = field.value || []
                                  if (isSelected) {
                                    field.onChange(current.filter((id) => id !== tag.id))
                                  } else {
                                    field.onChange([...current, tag.id])
                                  }
                                }}
                              >
                                {tag.name}
                              </Badge>
                            )
                          })}
                          {tags.length === 0 && (
                            <span className="text-muted-foreground text-sm">{t('blog.noTagsAvailable')}</span>
                          )}
                        </div>
                        <FormDescription>{t('blog.clickToToggleTags')}</FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

              {/* Featured Image */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader>
                  <CardTitle>{t('blog.featuredImage')}</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  {/* Upload button and preview */}
                  <div className="space-y-3">
                    {uploadingImage ? (
                      <div className="border-2 border-dashed border-primary/50 rounded-lg p-8 text-center bg-primary/5">
                        <Loader2 className="h-10 w-10 mx-auto text-primary mb-3 animate-spin" />
                        <p className="text-sm font-medium text-primary">{t('blog.uploadingImage')}</p>
                        <p className="text-xs text-muted-foreground mt-1">{t('blog.processingImage')}</p>
                      </div>
                    ) : form.watch('featuredImageUrl') ? (
                      <div
                        className="relative rounded-md overflow-hidden border"
                        style={id ? { viewTransitionName: `blog-featured-${id}` } : undefined}
                      >
                        <img
                          src={form.watch('featuredImageUrl')}
                          alt={form.watch('featuredImageAlt') || 'Featured image preview'}
                          className="w-full h-auto"
                          onError={(e) => {
                            e.currentTarget.style.display = 'none'
                          }}
                        />
                        <Button
                          type="button"
                          variant="destructive"
                          size="icon"
                          className="absolute top-2 right-2 h-8 w-8"
                          onClick={handleClearImage}
                        >
                          <X className="h-4 w-4" />
                        </Button>
                      </div>
                    ) : (
                      <div
                        className="border-2 border-dashed rounded-lg p-8 text-center cursor-pointer hover:border-primary/50 transition-colors"
                        onClick={() => fileInputRef.current?.click()}
                      >
                        <ImageIcon className="h-10 w-10 mx-auto text-muted-foreground mb-3" />
                        <p className="text-sm font-medium">{t('blog.clickToUpload')}</p>
                        <p className="text-xs text-muted-foreground mt-1">{t('blog.imageFormats')}</p>
                      </div>
                    )}

                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/*"
                      className="hidden"
                      onChange={handleImageUpload}
                      disabled={uploadingImage}
                    />

                    {form.watch('featuredImageUrl') && (
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="w-full"
                        onClick={() => fileInputRef.current?.click()}
                        disabled={uploadingImage}
                      >
                        <Upload className="h-4 w-4 mr-2" />
                        {uploadingImage ? t('labels.uploading') : t('blog.replaceImage')}
                      </Button>
                    )}
                  </div>

                  <FormField
                    control={form.control}
                    name="featuredImageAlt"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('blog.altText')}</FormLabel>
                        <FormControl>
                          <Input placeholder={t('blog.describeImage')} {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

              {/* SEO */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader>
                  <CardTitle>{t('blog.seo')}</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="metaTitle"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('blog.metaTitle')}</FormLabel>
                        <FormControl>
                          <Input placeholder={t('blog.seoTitle')} maxLength={60} {...field} />
                        </FormControl>
                        <FormDescription>
                          {t('blog.characters', { count: field.value?.length || 0, max: 60 })}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="metaDescription"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('blog.metaDescription')}</FormLabel>
                        <FormControl>
                          <Textarea placeholder={t('blog.seoDescription')} maxLength={160} rows={3} {...field} />
                        </FormControl>
                        <FormDescription>
                          {t('blog.characters', { count: field.value?.length || 0, max: 160 })}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="canonicalUrl"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('blog.canonicalUrl')}</FormLabel>
                        <FormControl>
                          <Input placeholder="https://..." {...field} />
                        </FormControl>
                        <FormDescription>
                          {t('blog.canonicalUrlHint')}
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="allowIndexing"
                    render={({ field }) => (
                      <FormItem className="flex items-center justify-between rounded-lg border p-3">
                        <div className="space-y-0.5">
                          <FormLabel>{t('blog.allowIndexing')}</FormLabel>
                          <FormDescription>
                            {t('blog.allowIndexingDescription')}
                          </FormDescription>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="cursor-pointer"
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>
            </div>
          </div>
        </form>
      </Form>
    </div>
  )
}

export default BlogPostEditPage
