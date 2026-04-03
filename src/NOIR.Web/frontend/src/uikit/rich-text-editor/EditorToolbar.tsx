import { useCallback, useRef, useState } from 'react'
import type { Editor } from '@tiptap/react'
import {
  Bold,
  Italic,
  Underline as UnderlineIcon,
  Strikethrough,
  AlignLeft,
  AlignCenter,
  AlignRight,
  AlignJustify,
  List,
  ListOrdered,
  Indent,
  Outdent,
  Link as LinkIcon,
  Image as ImageIcon,
  Table as TableIcon,
  Code,
  Maximize,
  Undo2,
  Redo2,
  Paintbrush,
  Highlighter,
  Variable,
} from 'lucide-react'
import { Button, Tooltip, TooltipContent, TooltipTrigger, Separator } from '@uikit'
import { cn } from '@/lib/utils'

interface EditorToolbarProps {
  editor: Editor | null
  preset: 'full' | 'minimal'
  onImageUpload?: (file: File) => Promise<string>
  variables?: string[]
}

const ToolbarButton = ({
  onClick,
  isActive = false,
  disabled = false,
  tooltip,
  children,
}: {
  onClick: () => void
  isActive?: boolean
  disabled?: boolean
  tooltip: string
  children: React.ReactNode
}) => (
  <Tooltip>
    <TooltipTrigger asChild>
      <Button
        type="button"
        variant="ghost"
        size="sm"
        className={cn(
          'h-8 w-8 p-0 cursor-pointer',
          isActive && 'bg-accent text-accent-foreground',
        )}
        onClick={onClick}
        disabled={disabled}
        aria-label={tooltip}
      >
        {children}
      </Button>
    </TooltipTrigger>
    <TooltipContent side="bottom" className="text-xs">
      {tooltip}
    </TooltipContent>
  </Tooltip>
)

export const EditorToolbar = ({
  editor,
  preset,
  onImageUpload,
  variables,
}: EditorToolbarProps) => {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [isFullscreen, setIsFullscreen] = useState(false)

  const handleImageUpload = useCallback(() => {
    fileInputRef.current?.click()
  }, [])

  const handleFileChange = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0]
      if (!file || !onImageUpload || !editor) return

      const url = await onImageUpload(file)
      if (url) {
        editor.chain().focus().setImage({ src: url }).run()
      }
      e.target.value = ''
    },
    [editor, onImageUpload],
  )

  const handleLink = useCallback(() => {
    if (!editor) return
    if (editor.isActive('link')) {
      editor.chain().focus().unsetLink().run()
      return
    }
    const url = window.prompt('Enter URL')
    if (url) {
      editor.chain().focus().extendMarkRange('link').setLink({ href: url }).run()
    }
  }, [editor])

  const handleTable = useCallback(() => {
    if (!editor) return
    editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()
  }, [editor])

  const handleColor = useCallback(() => {
    if (!editor) return
    const color = window.prompt('Enter color (hex)', '#000000')
    if (color) {
      editor.chain().focus().setColor(color).run()
    }
  }, [editor])

  const handleHighlight = useCallback(() => {
    if (!editor) return
    editor.chain().focus().toggleHighlight({ color: '#fef08a' }).run()
  }, [editor])

  const handleFullscreen = useCallback(() => {
    const editorEl = document.querySelector('.rich-text-editor')
    if (!editorEl) return

    if (!isFullscreen) {
      editorEl.classList.add('fixed', 'inset-0', 'z-50', 'bg-background')
      setIsFullscreen(true)
    } else {
      editorEl.classList.remove('fixed', 'inset-0', 'z-50', 'bg-background')
      setIsFullscreen(false)
    }
  }, [isFullscreen])

  const insertVariable = useCallback(
    (variable: string) => {
      if (!editor) return
      editor.chain().focus().insertContent(`{{${variable}}}`).run()
    },
    [editor],
  )

  if (!editor) return null

  return (
    <div className="flex flex-wrap items-center gap-0.5 border-b px-2 py-1.5 bg-muted/30">
      {/* Undo / Redo */}
      <ToolbarButton
        onClick={() => editor.chain().focus().undo().run()}
        disabled={!editor.can().undo()}
        tooltip="Undo"
      >
        <Undo2 className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton
        onClick={() => editor.chain().focus().redo().run()}
        disabled={!editor.can().redo()}
        tooltip="Redo"
      >
        <Redo2 className="h-4 w-4" />
      </ToolbarButton>

      <Separator orientation="vertical" className="mx-1 h-6" />

      {/* Headings */}
      <select
        className="h-8 rounded-md border border-input bg-background px-2 text-sm cursor-pointer"
        value={
          editor.isActive('heading', { level: 1 })
            ? '1'
            : editor.isActive('heading', { level: 2 })
              ? '2'
              : editor.isActive('heading', { level: 3 })
                ? '3'
                : editor.isActive('heading', { level: 4 })
                  ? '4'
                  : '0'
        }
        onChange={(e) => {
          const level = parseInt(e.target.value)
          if (level === 0) {
            editor.chain().focus().setParagraph().run()
          } else {
            editor
              .chain()
              .focus()
              .toggleHeading({ level: level as 1 | 2 | 3 | 4 })
              .run()
          }
        }}
      >
        <option value="0">Paragraph</option>
        <option value="1">Heading 1</option>
        <option value="2">Heading 2</option>
        <option value="3">Heading 3</option>
        <option value="4">Heading 4</option>
      </select>

      <Separator orientation="vertical" className="mx-1 h-6" />

      {/* Text formatting */}
      <ToolbarButton
        onClick={() => editor.chain().focus().toggleBold().run()}
        isActive={editor.isActive('bold')}
        tooltip="Bold"
      >
        <Bold className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton
        onClick={() => editor.chain().focus().toggleItalic().run()}
        isActive={editor.isActive('italic')}
        tooltip="Italic"
      >
        <Italic className="h-4 w-4" />
      </ToolbarButton>
      {preset === 'full' && (
        <>
          <ToolbarButton
            onClick={() => editor.chain().focus().toggleUnderline().run()}
            isActive={editor.isActive('underline')}
            tooltip="Underline"
          >
            <UnderlineIcon className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton
            onClick={() => editor.chain().focus().toggleStrike().run()}
            isActive={editor.isActive('strike')}
            tooltip="Strikethrough"
          >
            <Strikethrough className="h-4 w-4" />
          </ToolbarButton>
        </>
      )}

      {preset === 'full' && (
        <>
          <Separator orientation="vertical" className="mx-1 h-6" />

          {/* Colors */}
          <ToolbarButton onClick={handleColor} tooltip="Text Color">
            <Paintbrush className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton
            onClick={handleHighlight}
            isActive={editor.isActive('highlight')}
            tooltip="Highlight"
          >
            <Highlighter className="h-4 w-4" />
          </ToolbarButton>
        </>
      )}

      <Separator orientation="vertical" className="mx-1 h-6" />

      {/* Alignment */}
      <ToolbarButton
        onClick={() => editor.chain().focus().setTextAlign('left').run()}
        isActive={editor.isActive({ textAlign: 'left' })}
        tooltip="Align Left"
      >
        <AlignLeft className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton
        onClick={() => editor.chain().focus().setTextAlign('center').run()}
        isActive={editor.isActive({ textAlign: 'center' })}
        tooltip="Align Center"
      >
        <AlignCenter className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton
        onClick={() => editor.chain().focus().setTextAlign('right').run()}
        isActive={editor.isActive({ textAlign: 'right' })}
        tooltip="Align Right"
      >
        <AlignRight className="h-4 w-4" />
      </ToolbarButton>
      {preset === 'full' && (
        <ToolbarButton
          onClick={() => editor.chain().focus().setTextAlign('justify').run()}
          isActive={editor.isActive({ textAlign: 'justify' })}
          tooltip="Justify"
        >
          <AlignJustify className="h-4 w-4" />
        </ToolbarButton>
      )}

      <Separator orientation="vertical" className="mx-1 h-6" />

      {/* Lists */}
      <ToolbarButton
        onClick={() => editor.chain().focus().toggleBulletList().run()}
        isActive={editor.isActive('bulletList')}
        tooltip="Bullet List"
      >
        <List className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton
        onClick={() => editor.chain().focus().toggleOrderedList().run()}
        isActive={editor.isActive('orderedList')}
        tooltip="Numbered List"
      >
        <ListOrdered className="h-4 w-4" />
      </ToolbarButton>
      {preset === 'full' && (
        <>
          <ToolbarButton
            onClick={() => editor.chain().focus().sinkListItem('listItem').run()}
            disabled={!editor.can().sinkListItem('listItem')}
            tooltip="Increase Indent"
          >
            <Indent className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton
            onClick={() => editor.chain().focus().liftListItem('listItem').run()}
            disabled={!editor.can().liftListItem('listItem')}
            tooltip="Decrease Indent"
          >
            <Outdent className="h-4 w-4" />
          </ToolbarButton>
        </>
      )}

      <Separator orientation="vertical" className="mx-1 h-6" />

      {/* Link */}
      <ToolbarButton
        onClick={handleLink}
        isActive={editor.isActive('link')}
        tooltip="Link"
      >
        <LinkIcon className="h-4 w-4" />
      </ToolbarButton>

      {/* Image (only when upload handler provided) */}
      {onImageUpload && (
        <>
          <ToolbarButton onClick={handleImageUpload} tooltip="Insert Image">
            <ImageIcon className="h-4 w-4" />
          </ToolbarButton>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/*"
            className="hidden"
            onChange={handleFileChange}
          />
        </>
      )}

      {/* Table (full preset only) */}
      {preset === 'full' && (
        <ToolbarButton onClick={handleTable} tooltip="Insert Table">
          <TableIcon className="h-4 w-4" />
        </ToolbarButton>
      )}

      {/* Code block */}
      {preset === 'full' && (
        <>
          <Separator orientation="vertical" className="mx-1 h-6" />
          <ToolbarButton
            onClick={() => editor.chain().focus().toggleCodeBlock().run()}
            isActive={editor.isActive('codeBlock')}
            tooltip="Code Block"
          >
            <Code className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton onClick={handleFullscreen} tooltip="Fullscreen">
            <Maximize className="h-4 w-4" />
          </ToolbarButton>
        </>
      )}

      {/* Variable insertion (email template mode) */}
      {variables && variables.length > 0 && (
        <>
          <Separator orientation="vertical" className="mx-1 h-6" />
          <select
            className="h-8 rounded-md border border-input bg-background px-2 text-sm cursor-pointer"
            value=""
            onChange={(e) => {
              if (e.target.value) {
                insertVariable(e.target.value)
                e.target.value = ''
              }
            }}
          >
            <option value="">
              <Variable className="h-4 w-4" /> Insert Variable
            </option>
            {variables.map((v) => (
              <option key={v} value={v}>
                {`{{${v}}}`}
              </option>
            ))}
          </select>
        </>
      )}
    </div>
  )
}
