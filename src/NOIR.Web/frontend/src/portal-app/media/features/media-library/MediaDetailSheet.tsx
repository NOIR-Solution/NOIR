import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Copy, Pencil, Trash2, Check, X, Loader2, FileImage } from 'lucide-react'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  Button,
  Input,
  Credenza,
  CredenzaContent,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaDescription,
  CredenzaBody,
  CredenzaFooter,
  ThumbHashImage,
} from '@uikit'
import { toast } from 'sonner'
import type { MediaFileListItem } from '@/types'
import { formatFileSize } from './media-utils'

interface MediaDetailSheetProps {
  file: MediaFileListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onRename: (id: string, newFileName: string) => void
  onDelete: (file: MediaFileListItem) => void
  isRenaming: boolean
}

export const MediaDetailSheet = ({
  file,
  open,
  onOpenChange,
  onRename,
  onDelete,
  isRenaming,
}: MediaDetailSheetProps) => {
  const { t } = useTranslation('common')
  const { formatRelativeTime } = useRegionalSettings()
  const [isEditingName, setIsEditingName] = useState(false)
  const [editName, setEditName] = useState('')

  const startRename = () => {
    if (!file) return
    setEditName(file.originalFileName)
    setIsEditingName(true)
  }

  const confirmRename = () => {
    if (!file || !editName.trim()) return
    onRename(file.id, editName.trim())
    setIsEditingName(false)
  }

  const cancelRename = () => {
    setIsEditingName(false)
    setEditName('')
  }

  const copyUrl = () => {
    if (!file) return
    navigator.clipboard.writeText(new URL(file.defaultUrl, window.location.origin).href)
    toast.success(t('media.urlCopied', 'URL copied to clipboard'))
  }

  if (!file) return null

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent>
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
              <FileImage className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>{t('media.fileDetails', 'File Details')}</CredenzaTitle>
              <CredenzaDescription>{file.originalFileName}</CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <CredenzaBody>
          <div className="space-y-4">
            {/* Preview */}
            <div className="rounded-xl overflow-hidden border border-border/50">
              <ThumbHashImage
                src={file.defaultUrl}
                thumbHash={file.thumbHash}
                dominantColor={file.dominantColor}
                alt={file.altText || file.originalFileName}
                className="w-full aspect-video"
                objectFit="contain"
              />
            </div>

            {/* Filename with inline rename */}
            <div className="space-y-1.5">
              <label className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
                {t('labels.name', 'Name')}
              </label>
              {isEditingName ? (
                <div className="flex items-center gap-2">
                  <Input
                    value={editName}
                    onChange={(e) => setEditName(e.target.value)}
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') confirmRename()
                      if (e.key === 'Escape') cancelRename()
                    }}
                    className="h-8 flex-1"
                    autoFocus
                  />
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={confirmRename}
                    disabled={isRenaming || !editName.trim()}
                    className="cursor-pointer h-8 w-8 p-0 text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50"
                    aria-label={t('buttons.save', 'Save')}
                  >
                    {isRenaming ? <Loader2 className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}
                  </Button>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={cancelRename}
                    disabled={isRenaming}
                    className="cursor-pointer h-8 w-8 p-0 text-destructive hover:text-destructive hover:bg-destructive/10"
                    aria-label={t('labels.cancel', 'Cancel')}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              ) : (
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium truncate flex-1">{file.originalFileName}</span>
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={startRename}
                    className="cursor-pointer h-8 w-8 p-0 shrink-0"
                    aria-label={t('media.renameFile', { name: file.originalFileName, defaultValue: `Rename ${file.originalFileName}` })}
                  >
                    <Pencil className="h-3.5 w-3.5" />
                  </Button>
                </div>
              )}
            </div>

            {/* Metadata grid */}
            <div className="grid grid-cols-2 gap-4">
              <MetadataItem label={t('media.folder', 'Folder')} value={file.folder || '\u2014'} />
              <MetadataItem label={t('media.format', 'Format')} value={file.format.toUpperCase()} />
              <MetadataItem label={t('media.dimensions', 'Dimensions')} value={file.width > 0 ? `${file.width} x ${file.height}` : '\u2014'} />
              <MetadataItem label={t('media.size', 'Size')} value={formatFileSize(file.sizeBytes)} />
              <MetadataItem label={t('media.mimeType', 'MIME Type')} value={file.mimeType} />
              <MetadataItem label={t('labels.uploaded', 'Uploaded')} value={formatRelativeTime(file.createdAt)} />
              <MetadataItem label={t('media.shortId', 'Short ID')} value={file.shortId} />
              <MetadataItem label={t('media.slug', 'Slug')} value={file.slug} />
            </div>
          </div>
        </CredenzaBody>

        <CredenzaFooter>
          <Button
            variant="outline"
            onClick={copyUrl}
            className="cursor-pointer"
          >
            <Copy className="h-4 w-4 mr-2" />
            {t('media.copyUrl', 'Copy URL')}
          </Button>
          <Button
            variant="destructive"
            onClick={() => onDelete(file)}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            <Trash2 className="h-4 w-4 mr-2" />
            {t('labels.delete', 'Delete')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}

const MetadataItem = ({ label, value }: { label: string; value: string }) => (
  <div className="space-y-1">
    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">{label}</span>
    <p className="text-sm truncate">{value}</p>
  </div>
)
