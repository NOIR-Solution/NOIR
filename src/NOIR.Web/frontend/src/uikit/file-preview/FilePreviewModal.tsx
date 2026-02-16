import { useCallback, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import Lightbox from 'yet-another-react-lightbox'
import type { Slide, RenderSlideProps } from 'yet-another-react-lightbox'
import Zoom from 'yet-another-react-lightbox/plugins/zoom'
import Video from 'yet-another-react-lightbox/plugins/video'
import Counter from 'yet-another-react-lightbox/plugins/counter'
import Download from 'yet-another-react-lightbox/plugins/download'
import 'yet-another-react-lightbox/styles.css'
import 'yet-another-react-lightbox/plugins/counter.css'
import { Download as DownloadIcon } from 'lucide-react'
import { Button } from '../button/Button'
import { getFileCategory, getFileIcon, downloadFile, type PreviewFile, type FileCategory } from './file-preview.utils'

interface FilePreviewModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  files: PreviewFile[]
  initialIndex?: number
}

interface CustomSlideData {
  _fileCategory?: FileCategory
  _file?: PreviewFile
}

type CustomSlide = Slide & CustomSlideData

const getVideoMimeType = (url: string): string => {
  const ext = url.split('.').pop()?.toLowerCase()
  switch (ext) {
    case 'mp4': return 'video/mp4'
    case 'webm': return 'video/webm'
    case 'ogg': return 'video/ogg'
    case 'mov': return 'video/quicktime'
    default: return 'video/mp4'
  }
}

const mapFileToSlide = (file: PreviewFile): CustomSlide => {
  const category = getFileCategory(file.url, file.mimeType)

  switch (category) {
    case 'image':
      return {
        src: file.url,
        alt: file.name,
        download: { url: file.url, filename: file.name },
        _fileCategory: category,
        _file: file,
      }
    case 'video':
      return {
        type: 'video' as const,
        sources: [{ src: file.url, type: file.mimeType || getVideoMimeType(file.url) }],
        download: { url: file.url, filename: file.name },
        _fileCategory: category,
        _file: file,
      } as CustomSlide
    case 'pdf':
    case 'audio':
    case 'unknown':
    default:
      return {
        src: file.url,
        download: { url: file.url, filename: file.name },
        _fileCategory: category,
        _file: file,
      }
  }
}

const PdfSlide = ({ file }: { file: PreviewFile }) => (
  <div className="flex items-center justify-center w-full h-full p-4">
    <iframe
      src={file.url}
      title={file.name}
      className="w-full h-full max-w-4xl max-h-[80vh] rounded-lg bg-white"
      style={{ border: 'none' }}
    />
  </div>
)

const AudioSlide = ({ file }: { file: PreviewFile }) => {
  const Icon = getFileIcon('audio')
  return (
    <div className="flex flex-col items-center justify-center gap-6 p-8">
      <div className="w-24 h-24 rounded-2xl bg-white/10 flex items-center justify-center">
        <Icon className="w-12 h-12 text-white/80" />
      </div>
      <p className="text-white/90 text-lg font-medium text-center max-w-md truncate">
        {file.name}
      </p>
      {/* eslint-disable-next-line jsx-a11y/media-has-caption */}
      <audio controls className="w-full max-w-md" src={file.url}>
        Your browser does not support the audio element.
      </audio>
    </div>
  )
}

const UnknownSlide = ({ file, downloadLabel }: { file: PreviewFile; downloadLabel: string }) => {
  const Icon = getFileIcon('unknown')
  const { t } = useTranslation('common')
  return (
    <div className="flex flex-col items-center justify-center gap-4 p-8">
      <div className="w-24 h-24 rounded-2xl bg-white/10 flex items-center justify-center">
        <Icon className="w-12 h-12 text-white/80" />
      </div>
      <p className="text-white/90 text-lg font-medium text-center max-w-md truncate">
        {file.name}
      </p>
      <p className="text-white/50 text-sm">
        {t('filePreview.previewNotAvailable', 'Preview not available for this file type')}
      </p>
      <Button
        variant="outline"
        className="mt-2 cursor-pointer bg-white/10 border-white/20 text-white hover:bg-white/20"
        onClick={() => downloadFile(file.url, file.name)}
      >
        <DownloadIcon className="w-4 h-4 mr-2" />
        {downloadLabel}
      </Button>
    </div>
  )
}

export const FilePreviewModal = ({
  open,
  onOpenChange,
  files,
  initialIndex = 0,
}: FilePreviewModalProps) => {
  const { t } = useTranslation('common')
  const slides = useMemo(() => files.map(mapFileToSlide), [files])
  const isSingle = files.length <= 1

  const downloadLabel = t('filePreview.downloadFile', 'Download file')

  const renderCustomSlide = useCallback(({ slide }: RenderSlideProps) => {
    const customSlide = slide as CustomSlide
    const category = customSlide._fileCategory
    const file = customSlide._file

    if (!file || !category) return undefined

    switch (category) {
      case 'pdf':
        return <PdfSlide file={file} />
      case 'audio':
        return <AudioSlide file={file} />
      case 'unknown':
        return <UnknownSlide file={file} downloadLabel={downloadLabel} />
      default:
        return undefined // Let the library handle image/video
    }
  }, [downloadLabel])

  const renderSlideFooter = useCallback(({ slide }: { slide: Slide }) => {
    const customSlide = slide as CustomSlide
    const file = customSlide._file
    if (!file) return null
    return (
      <div className="absolute bottom-0 left-0 right-0 text-center pb-2 pointer-events-none">
        <span className="text-white/70 text-sm bg-black/40 px-3 py-1 rounded-full">
          {file.name}
        </span>
      </div>
    )
  }, [])

  const handleDownload = useCallback(({ slide }: { slide: Slide }) => {
    const customSlide = slide as CustomSlide
    const file = customSlide._file
    if (file) {
      downloadFile(file.url, file.name)
    }
  }, [])

  if (files.length === 0) return null

  return (
    <Lightbox
      open={open}
      close={() => onOpenChange(false)}
      slides={slides}
      index={initialIndex}
      plugins={[Zoom, Video, Counter, Download]}
      controller={{
        closeOnBackdropClick: true,
      }}
      zoom={{
        maxZoomPixelRatio: 3,
        zoomInMultiplier: 2,
        doubleTapDelay: 300,
        doubleClickDelay: 300,
        doubleClickMaxStops: 2,
        keyboardMoveDistance: 50,
        wheelZoomDistanceFactor: 100,
        pinchZoomDistanceFactor: 100,
        scrollToZoom: true,
      }}
      video={{
        controls: true,
        playsInline: true,
      }}
      download={{
        download: handleDownload,
      }}
      carousel={{
        finite: isSingle,
      }}
      render={{
        ...(isSingle ? { buttonPrev: () => null, buttonNext: () => null } : {}),
        slide: renderCustomSlide,
        slideFooter: renderSlideFooter,
      }}
      styles={{
        container: { backgroundColor: 'rgba(0, 0, 0, 0.92)' },
      }}
    />
  )
}
