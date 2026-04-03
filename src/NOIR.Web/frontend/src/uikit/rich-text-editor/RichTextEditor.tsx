import { useCallback, useEffect, useRef } from 'react'
import { useEditor, EditorContent, type Editor } from '@tiptap/react'
import { StarterKit } from '@tiptap/starter-kit'
import { Image } from '@tiptap/extension-image'
import { Link } from '@tiptap/extension-link'
import { Table } from '@tiptap/extension-table'
import { TableRow } from '@tiptap/extension-table-row'
import { TableHeader } from '@tiptap/extension-table-header'
import { TableCell } from '@tiptap/extension-table-cell'
import { TextAlign } from '@tiptap/extension-text-align'
import { Underline } from '@tiptap/extension-underline'
import { TextStyle } from '@tiptap/extension-text-style'
import { Color } from '@tiptap/extension-color'
import { Highlight } from '@tiptap/extension-highlight'
import { Placeholder } from '@tiptap/extension-placeholder'
import { CharacterCount } from '@tiptap/extension-character-count'
import { cn } from '@/lib/utils'
import { EditorToolbar } from './EditorToolbar'

export interface RichTextEditorProps {
  /** HTML content value */
  value: string
  /** Change handler receiving HTML string */
  onChange: (html: string) => void
  /** Toolbar preset */
  preset?: 'full' | 'minimal'
  /** Editor height in pixels */
  height?: number
  /** Enable image upload — pass the upload callback */
  onImageUpload?: (file: File) => Promise<string>
  /** Variables for autocomplete (email templates) */
  variables?: string[]
  /** Called when editor is ready */
  onReady?: (editor: Editor) => void
  /** Whether the editor is read-only */
  readOnly?: boolean
  /** Placeholder text */
  placeholder?: string
  /** Additional class name */
  className?: string
  /** Error state */
  hasError?: boolean
}

export const RichTextEditor = ({
  value,
  onChange,
  preset = 'full',
  height = 500,
  onImageUpload,
  variables,
  onReady,
  readOnly = false,
  placeholder,
  className,
  hasError = false,
}: RichTextEditorProps) => {
  const onReadyRef = useRef(onReady)
  onReadyRef.current = onReady

  const onChangeRef = useRef(onChange)
  onChangeRef.current = onChange

  const onImageUploadRef = useRef(onImageUpload)
  onImageUploadRef.current = onImageUpload

  const handleImageUpload = useCallback(async (file: File): Promise<string> => {
    if (onImageUploadRef.current) {
      return onImageUploadRef.current(file)
    }
    return ''
  }, [])

  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [1, 2, 3, 4] },
      }),
      Underline,
      TextStyle,
      Color,
      Highlight.configure({ multicolor: true }),
      TextAlign.configure({
        types: ['heading', 'paragraph'],
      }),
      Link.configure({
        openOnClick: false,
        HTMLAttributes: { rel: 'noopener noreferrer', target: '_blank' },
      }),
      Image.configure({ inline: false, allowBase64: false }),
      ...(preset === 'full'
        ? [
            Table.configure({ resizable: true }),
            TableRow,
            TableHeader,
            TableCell,
          ]
        : []),
      Placeholder.configure({ placeholder: placeholder ?? '' }),
      CharacterCount,
    ],
    content: value,
    editable: !readOnly,
    onUpdate: ({ editor: ed }) => {
      onChangeRef.current(ed.getHTML())
    },
    onCreate: ({ editor: ed }) => {
      onReadyRef.current?.(ed)
    },
    // Handle image paste/drop
    editorProps: {
      handleDrop: (view, event) => {
        if (!onImageUploadRef.current) return false
        const files = event.dataTransfer?.files
        if (!files?.length) return false

        const imageFiles = Array.from(files).filter((f) => f.type.startsWith('image/'))
        if (!imageFiles.length) return false

        event.preventDefault()
        imageFiles.forEach((file) => {
          handleImageUpload(file).then((url) => {
            if (url) {
              const { schema } = view.state
              const node = schema.nodes.image?.create({ src: url })
              if (node) {
                const tr = view.state.tr.replaceSelectionWith(node)
                view.dispatch(tr)
              }
            }
          })
        })
        return true
      },
      handlePaste: (view, event) => {
        if (!onImageUploadRef.current) return false
        const items = event.clipboardData?.items
        if (!items) return false

        const imageItems = Array.from(items).filter((item) => item.type.startsWith('image/'))
        if (!imageItems.length) return false

        event.preventDefault()
        imageItems.forEach((item) => {
          const file = item.getAsFile()
          if (file) {
            handleImageUpload(file).then((url) => {
              if (url) {
                const { schema } = view.state
                const node = schema.nodes.image?.create({ src: url })
                if (node) {
                  const tr = view.state.tr.replaceSelectionWith(node)
                  view.dispatch(tr)
                }
              }
            })
          }
        })
        return true
      },
    },
  })

  // Sync external value changes (e.g. form reset, loading data)
  useEffect(() => {
    if (!editor) return
    const currentHtml = editor.getHTML()
    // Avoid resetting if content is the same (prevents cursor jump)
    if (value !== currentHtml) {
      editor.commands.setContent(value, { emitUpdate: false })
    }
  }, [value, editor])

  // Sync readOnly
  useEffect(() => {
    if (editor) {
      editor.setEditable(!readOnly)
    }
  }, [readOnly, editor])

  return (
    <div
      className={cn(
        'rich-text-editor rounded-md border',
        hasError ? 'border-destructive' : 'border-input',
        'focus-within:ring-1 focus-within:ring-ring',
        className,
      )}
    >
      {!readOnly && (
        <EditorToolbar
          editor={editor}
          preset={preset}
          onImageUpload={onImageUpload ? handleImageUpload : undefined}
          variables={variables}
        />
      )}
      <EditorContent
        editor={editor}
        className="tiptap-content"
        style={{ minHeight: height, maxHeight: height * 1.5, overflowY: 'auto' }}
      />
    </div>
  )
}
