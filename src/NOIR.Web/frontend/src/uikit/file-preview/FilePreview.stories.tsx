import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { FilePreviewTrigger } from './FilePreviewTrigger'
import { FilePreviewModal } from './FilePreviewModal'
import { Button } from '../button/Button'
import type { PreviewFile } from './file-preview.utils'

// -- Sample data --

const sampleImage: PreviewFile = {
  url: 'https://picsum.photos/800/600',
  name: 'landscape-photo.jpg',
  thumbnailUrl: 'https://picsum.photos/80/80',
}

const sampleImages: PreviewFile[] = [
  { url: 'https://picsum.photos/800/600?random=1', name: 'photo-1.jpg' },
  { url: 'https://picsum.photos/900/600?random=2', name: 'photo-2.jpg' },
  { url: 'https://picsum.photos/700/700?random=3', name: 'photo-3.jpg' },
  { url: 'https://picsum.photos/1000/800?random=4', name: 'photo-4.jpg' },
]

const sampleVideo: PreviewFile = {
  url: 'https://www.w3schools.com/html/mov_bbb.mp4',
  name: 'sample-video.mp4',
  mimeType: 'video/mp4',
}

const sampleAudio: PreviewFile = {
  url: 'https://www.w3schools.com/html/horse.mp3',
  name: 'horse-sound.mp3',
  mimeType: 'audio/mpeg',
}

const samplePdf: PreviewFile = {
  url: 'https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf',
  name: 'document.pdf',
  mimeType: 'application/pdf',
}

const sampleUnknown: PreviewFile = {
  url: 'https://example.com/archive.zip',
  name: 'project-files.zip',
}

const mixedFiles: PreviewFile[] = [
  sampleImages[0],
  sampleVideo,
  samplePdf,
  sampleAudio,
  sampleImages[1],
  sampleUnknown,
]

// -- FilePreviewTrigger stories --

const triggerMeta = {
  title: 'UIKit/FilePreviewTrigger',
  component: FilePreviewTrigger,
  tags: ['autodocs'],
  argTypes: {
    thumbnailWidth: { control: 'number' },
    thumbnailHeight: { control: 'number' },
    showHoverPreview: { control: 'boolean' },
  },
} satisfies Meta<typeof FilePreviewTrigger>

export default triggerMeta
type Story = StoryObj<typeof triggerMeta>

export const Default: Story = {
  args: {
    file: sampleImage,
    thumbnailWidth: 80,
    thumbnailHeight: 80,
  },
}

export const WithThumbnail: Story = {
  args: {
    file: sampleImage,
    thumbnailWidth: 80,
    thumbnailHeight: 80,
  },
}

export const LargeThumbnail: Story = {
  args: {
    file: { ...sampleImage, url: 'https://picsum.photos/1200/900' },
    thumbnailWidth: 200,
    thumbnailHeight: 150,
  },
}

export const SmallThumbnail: Story = {
  args: {
    file: sampleImages[0],
    thumbnailWidth: 32,
    thumbnailHeight: 32,
  },
}

export const NoHoverPreview: Story = {
  args: {
    file: sampleImage,
    thumbnailWidth: 80,
    thumbnailHeight: 80,
    showHoverPreview: false,
  },
}

export const VideoFile: Story = {
  args: {
    file: sampleVideo,
    thumbnailWidth: 80,
    thumbnailHeight: 80,
  },
}

export const AudioFile: Story = {
  args: {
    file: sampleAudio,
    thumbnailWidth: 80,
    thumbnailHeight: 80,
  },
}

export const PdfFile: Story = {
  args: {
    file: samplePdf,
    thumbnailWidth: 80,
    thumbnailHeight: 80,
  },
}

export const UnknownFile: Story = {
  args: {
    file: sampleUnknown,
    thumbnailWidth: 80,
    thumbnailHeight: 80,
  },
}

export const NoSource: Story = {
  args: {
    file: { url: '', name: 'missing.jpg' },
    thumbnailWidth: 80,
    thumbnailHeight: 80,
  },
}

export const MultiFileGallery: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
      {sampleImages.map((file, i) => (
        <FilePreviewTrigger
          key={file.url}
          file={file}
          files={sampleImages}
          index={i}
          thumbnailWidth={64}
          thumbnailHeight={64}
        />
      ))}
    </div>
  ),
}

export const MixedFileTypes: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap', alignItems: 'center' }}>
      {mixedFiles.map((file, i) => (
        <div key={file.url} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '4px' }}>
          <FilePreviewTrigger
            file={file}
            files={mixedFiles}
            index={i}
            thumbnailWidth={64}
            thumbnailHeight={64}
          />
          <span style={{ fontSize: 10, color: '#6b7280', maxWidth: 64, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', textAlign: 'center', display: 'block' }}>
            {file.name}
          </span>
        </div>
      ))}
    </div>
  ),
}

export const InTableRow: Story = {
  render: () => (
    <table style={{ borderCollapse: 'collapse', width: '100%', maxWidth: 500 }}>
      <thead>
        <tr>
          <th style={{ textAlign: 'left', padding: '8px', borderBottom: '1px solid #e5e7eb', fontSize: 13 }}>Image</th>
          <th style={{ textAlign: 'left', padding: '8px', borderBottom: '1px solid #e5e7eb', fontSize: 13 }}>Title</th>
          <th style={{ textAlign: 'left', padding: '8px', borderBottom: '1px solid #e5e7eb', fontSize: 13 }}>Type</th>
        </tr>
      </thead>
      <tbody>
        {[
          { file: sampleImages[0], title: 'Blog Post 1', type: 'Published' },
          { file: sampleImages[1], title: 'Blog Post 2', type: 'Draft' },
          { file: { url: '', name: 'no-image' } as PreviewFile, title: 'Blog Post 3', type: 'Draft' },
        ].map(({ file, title, type }) => (
          <tr key={title}>
            <td style={{ padding: '8px', borderBottom: '1px solid #f3f4f6' }}>
              <FilePreviewTrigger file={file} thumbnailWidth={48} thumbnailHeight={48} />
            </td>
            <td style={{ padding: '8px', borderBottom: '1px solid #f3f4f6', fontSize: 13 }}>{title}</td>
            <td style={{ padding: '8px', borderBottom: '1px solid #f3f4f6', fontSize: 13 }}>{type}</td>
          </tr>
        ))}
      </tbody>
    </table>
  ),
}

// -- FilePreviewModal story (standalone) --

export const ModalStandalone: Story = {
  render: () => {
    const [open, setOpen] = useState(false)
    return (
      <div>
        <Button onClick={() => setOpen(true)} className="cursor-pointer">
          Open Preview Modal
        </Button>
        <FilePreviewModal
          open={open}
          onOpenChange={setOpen}
          files={sampleImages}
          initialIndex={0}
        />
      </div>
    )
  },
}

export const ModalMixedFiles: Story = {
  render: () => {
    const [open, setOpen] = useState(false)
    return (
      <div>
        <Button onClick={() => setOpen(true)} className="cursor-pointer">
          Open Mixed Files Modal
        </Button>
        <FilePreviewModal
          open={open}
          onOpenChange={setOpen}
          files={mixedFiles}
          initialIndex={0}
        />
      </div>
    )
  },
}

export const ModalSingleFile: Story = {
  render: () => {
    const [open, setOpen] = useState(false)
    return (
      <div>
        <Button onClick={() => setOpen(true)} className="cursor-pointer">
          Open Single Image
        </Button>
        <FilePreviewModal
          open={open}
          onOpenChange={setOpen}
          files={[sampleImage]}
        />
      </div>
    )
  },
}
