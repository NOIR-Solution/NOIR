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
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { toast } from 'sonner'
import { getPostById, createPost, updatePost, publishPost, unpublishPost } from '@/services/blog'
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group'
import { Label } from '@/components/ui/label'
import { DatePicker } from '@/components/ui/date-picker'
import { TimePicker } from '@/components/ui/time-picker'
import { uploadMedia } from '@/services/media'
import { useCategories, useTags } from '@/hooks/useBlog'
import { ApiError } from '@/services/apiClient'
import type { Post, CreatePostRequest } from '@/types'

const formSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200, 'Title cannot exceed 200 characters'),
  slug: z.string().min(1, 'Slug is required').max(200, 'Slug cannot exceed 200 characters').regex(/^[a-z0-9-]+$/, 'Slug can only contain lowercase letters, numbers, and hyphens'),
  excerpt: z.string().max(500, 'Excerpt cannot exceed 500 characters').optional(),
  categoryId: z.string().optional(),
  tagIds: z.array(z.string()).optional(),
  metaTitle: z.string().max(60, 'Meta title should be under 60 characters').optional(),
  metaDescription: z.string().max(160, 'Meta description should be under 160 characters').optional(),
  canonicalUrl: z.string().url('Must be a valid URL').optional().or(z.literal('')),
  allowIndexing: z.boolean().default(true),
  featuredImageId: z.string().optional(),
  featuredImageUrl: z.string().optional(),
  featuredImageAlt: z.string().max(200, 'Alt text cannot exceed 200 characters').optional(),
})

type FormValues = z.output<typeof formSchema>

export default function PostEditorPage() {
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

  const { data: categories } = useCategories()
  const { data: tags } = useTags()

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema) as unknown as Resolver<FormValues>,
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
            setScheduledDate(scheduleDate.toISOString().split('T')[0])
            setScheduledTime(scheduleDate.toTimeString().slice(0, 5))
          } else {
            setPublishOption('draft')
          }
        })
        .catch((err) => {
          const message = err instanceof ApiError ? err.message : 'Failed to load post'
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
        toast.error('Please select a date for scheduling')
        return
      }
      const [hours, minutes] = scheduledTime.split(':').map(Number)
      const scheduledDateTime = new Date(scheduledDate)
      scheduledDateTime.setHours(hours, minutes, 0, 0)
      if (scheduledDateTime <= new Date()) {
        toast.error('Scheduled date must be in the future')
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
          toast.success('Post saved as draft')
        } else {
          toast.success(isEdit ? 'Post saved' : 'Post created')
        }
      } else if (publishOption === 'publish') {
        // Publish immediately
        if (savedPost.status !== 'Published') {
          await publishPost(savedPost.id)
          toast.success('Post published')
        } else {
          toast.success('Post saved')
        }
      } else if (publishOption === 'schedule') {
        // Schedule for future
        const [hours, minutes] = scheduledTime.split(':').map(Number)
        const scheduledDateTime = new Date(scheduledDate!)
        scheduledDateTime.setHours(hours, minutes, 0, 0)
        await publishPost(savedPost.id, { scheduledPublishAt: scheduledDateTime.toISOString() })
        toast.success(`Post scheduled for ${formatDateTime(scheduledDateTime)}`)
      }

      navigate('/portal/blog/posts')
    } catch (err) {
      const message = err instanceof ApiError ? err.message : 'Failed to save post'
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
      toast.error('Please select an image file')
      return
    }

    // Validate file size (max 10MB)
    if (file.size > 10 * 1024 * 1024) {
      toast.error('Image must be less than 10MB')
      return
    }

    setUploadingImage(true)
    try {
      const result = await uploadMedia(file, 'blog')
      if (result.success && result.mediaFileId) {
        form.setValue('featuredImageId', result.mediaFileId)
        form.setValue('featuredImageUrl', result.defaultUrl || result.location || '')
        toast.success('Image uploaded successfully')
      } else {
        toast.error(result.error || 'Failed to upload image')
      }
    } catch (err) {
      toast.error('Failed to upload image')
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
        <p className="text-muted-foreground">Loading...</p>
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
              {isEdit ? 'Edit Post' : 'New Post'}
            </h1>
            <p className="text-muted-foreground">
              {isEdit ? `Editing: ${post?.title || 'Loading...'}` : 'Create a new blog post'}
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
                ? `Scheduled: ${formatDate(post.scheduledPublishAt!)}`
                : post.status}
            </Badge>
          )}
          <Button onClick={() => form.handleSubmit(onSubmit)()} disabled={saving}>
            {saving ? (
              <>
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                Saving...
              </>
            ) : (
              <>
                <Save className="h-4 w-4 mr-2" />
                Save
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
                <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
                  <CardTitle>Content</CardTitle>
                  <CardDescription>Write your blog post content</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="title"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Title</FormLabel>
                        <FormControl>
                          <Input placeholder="Enter post title" className="text-lg" {...field} />
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
                        <FormLabel>Slug</FormLabel>
                        <FormControl>
                          <Input placeholder="post-url-slug" {...field} />
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
                        <FormLabel>Excerpt</FormLabel>
                        <FormControl>
                          <Textarea
                            placeholder="A brief summary of your post..."
                            rows={3}
                            {...field}
                          />
                        </FormControl>
                        <FormDescription>
                          Shown in post lists and search results
                        </FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div>
                    <FormLabel className="mb-2 block">Content</FormLabel>
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
                <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
                  <CardTitle className="flex items-center gap-2">
                    <Calendar className="h-4 w-4" />
                    Publishing
                  </CardTitle>
                  <CardDescription>Choose when to publish your post</CardDescription>
                </CardHeader>
                <CardContent className="space-y-3">
                  <RadioGroup value={publishOption} onValueChange={(v) => setPublishOption(v as PublishOption)} className="space-y-2">
                    <label
                      htmlFor="draft"
                      className="flex items-center gap-3 rounded-lg border p-3 hover:bg-accent/50 cursor-pointer transition-colors"
                    >
                      <RadioGroupItem value="draft" id="draft" />
                      <div className="space-y-0.5">
                        <span className="font-medium text-sm">Save as Draft</span>
                        <p className="text-xs text-muted-foreground">
                          Post won't be visible to public
                        </p>
                      </div>
                    </label>

                    <label
                      htmlFor="publish"
                      className="flex items-center gap-3 rounded-lg border p-3 hover:bg-accent/50 cursor-pointer transition-colors"
                    >
                      <RadioGroupItem value="publish" id="publish" />
                      <div className="space-y-0.5">
                        <span className="font-medium text-sm">Publish Now</span>
                        <p className="text-xs text-muted-foreground">
                          Post will be visible immediately
                        </p>
                      </div>
                    </label>

                    <label
                      htmlFor="schedule"
                      className="flex items-center gap-3 rounded-lg border p-3 hover:bg-accent/50 cursor-pointer transition-colors"
                    >
                      <RadioGroupItem value="schedule" id="schedule" />
                      <div className="space-y-0.5">
                        <span className="font-medium text-sm">Schedule</span>
                        <p className="text-xs text-muted-foreground">
                          Post will auto-publish at the set time
                        </p>
                      </div>
                    </label>
                  </RadioGroup>

                  {/* Schedule date/time picker */}
                  {publishOption === 'schedule' && (
                    <div className="pt-4 mt-2 border-t space-y-4">
                      <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <Label className="text-sm font-medium">Date</Label>
                          <DatePicker
                            value={scheduledDate}
                            onChange={setScheduledDate}
                            minDate={new Date()}
                            placeholder="Select date"
                          />
                        </div>
                        <div className="space-y-2">
                          <Label className="text-sm font-medium">Time</Label>
                          <TimePicker
                            value={scheduledTime}
                            onChange={(time) => setScheduledTime(time)}
                            placeholder="Select time"
                            interval={30}
                          />
                        </div>
                      </div>
                      <p className="text-xs text-muted-foreground flex items-center gap-1.5">
                        <Info className="h-3.5 w-3.5 flex-shrink-0" />
                        Uses your local timezone
                      </p>
                    </div>
                  )}

                  {/* Status info for existing posts */}
                  {post && post.status !== 'Draft' && publishOption === 'draft' && (
                    <div className="p-3 rounded-md bg-amber-50 dark:bg-amber-950/20 border border-amber-200 dark:border-amber-800">
                      <p className="text-sm text-amber-800 dark:text-amber-200">
                        This post is currently <strong>{post.status}</strong>.
                        Saving as draft will unpublish it.
                      </p>
                    </div>
                  )}
                </CardContent>
              </Card>

              {/* Organization */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
                  <CardTitle>Organization</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="categoryId"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Category</FormLabel>
                        <Select
                          onValueChange={(value) => field.onChange(value === '__none__' ? '' : value)}
                          value={field.value || '__none__'}
                        >
                          <FormControl>
                            <SelectTrigger className="cursor-pointer">
                              <SelectValue placeholder="Select category" />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value="__none__" className="cursor-pointer">No category</SelectItem>
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
                        <FormLabel>Tags</FormLabel>
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
                            <span className="text-muted-foreground text-sm">No tags available</span>
                          )}
                        </div>
                        <FormDescription>Click to toggle tags</FormDescription>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

              {/* Featured Image */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
                  <CardTitle>Featured Image</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  {/* Upload button and preview */}
                  <div className="space-y-3">
                    {uploadingImage ? (
                      <div className="border-2 border-dashed border-primary/50 rounded-lg p-8 text-center bg-primary/5">
                        <Loader2 className="h-10 w-10 mx-auto text-primary mb-3 animate-spin" />
                        <p className="text-sm font-medium text-primary">Uploading image...</p>
                        <p className="text-xs text-muted-foreground mt-1">Please wait while we process your image</p>
                      </div>
                    ) : form.watch('featuredImageUrl') ? (
                      <div className="relative rounded-md overflow-hidden border">
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
                        <p className="text-sm font-medium">Click to upload featured image</p>
                        <p className="text-xs text-muted-foreground mt-1">JPG, PNG, GIF, WebP up to 10MB</p>
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
                        {uploadingImage ? 'Uploading...' : 'Replace Image'}
                      </Button>
                    )}
                  </div>

                  <FormField
                    control={form.control}
                    name="featuredImageAlt"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Alt Text</FormLabel>
                        <FormControl>
                          <Input placeholder="Describe the image" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

              {/* SEO */}
              <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
                <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg">
                  <CardTitle>SEO</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <FormField
                    control={form.control}
                    name="metaTitle"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Meta Title</FormLabel>
                        <FormControl>
                          <Input placeholder="SEO title" maxLength={60} {...field} />
                        </FormControl>
                        <FormDescription>
                          {field.value?.length || 0}/60 characters
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
                        <FormLabel>Meta Description</FormLabel>
                        <FormControl>
                          <Textarea placeholder="SEO description" maxLength={160} rows={3} {...field} />
                        </FormControl>
                        <FormDescription>
                          {field.value?.length || 0}/160 characters
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
                        <FormLabel>Canonical URL</FormLabel>
                        <FormControl>
                          <Input placeholder="https://..." {...field} />
                        </FormControl>
                        <FormDescription>
                          Leave empty to use default
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
                          <FormLabel>Allow Indexing</FormLabel>
                          <FormDescription>
                            Let search engines index this post
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
