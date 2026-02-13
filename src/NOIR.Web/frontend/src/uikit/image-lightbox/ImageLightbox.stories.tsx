import type { Meta, StoryObj } from 'storybook'
import { ImageLightbox } from './ImageLightbox'

const meta = {
  title: 'UIKit/ImageLightbox',
  component: ImageLightbox,
  tags: ['autodocs'],
  argTypes: {
    thumbnailWidth: { control: 'number' },
    thumbnailHeight: { control: 'number' },
    showPlaceholder: { control: 'boolean' },
  },
} satisfies Meta<typeof ImageLightbox>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    src: 'https://picsum.photos/800/600',
    alt: 'Sample image',
    thumbnailWidth: 80,
    thumbnailHeight: 80,
  },
}

export const WithThumbnail: Story = {
  args: {
    src: 'https://picsum.photos/800/600',
    thumbnailSrc: 'https://picsum.photos/80/80',
    alt: 'Image with separate thumbnail',
    thumbnailWidth: 80,
    thumbnailHeight: 80,
  },
}

export const LargeThumbnail: Story = {
  args: {
    src: 'https://picsum.photos/1200/900',
    alt: 'Large thumbnail preview',
    thumbnailWidth: 200,
    thumbnailHeight: 150,
  },
}

export const SmallThumbnail: Story = {
  args: {
    src: 'https://picsum.photos/800/600',
    alt: 'Small thumbnail',
    thumbnailWidth: 32,
    thumbnailHeight: 32,
  },
}

export const NoSource: Story = {
  args: {
    src: '',
    alt: 'No image',
    showPlaceholder: true,
  },
}

export const HidePlaceholder: Story = {
  args: {
    src: '',
    alt: 'No image',
    showPlaceholder: false,
  },
}

export const CustomFallback: Story = {
  args: {
    src: '',
    alt: 'Custom fallback',
    fallback: (
      <div
        style={{
          width: 80,
          height: 80,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          background: '#f3f4f6',
          borderRadius: 8,
          fontSize: 12,
          color: '#9ca3af',
        }}
      >
        N/A
      </div>
    ),
  },
}

export const MultipleImages: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '12px' }}>
      <ImageLightbox
        src="https://picsum.photos/800/600?random=1"
        alt="Image 1"
        thumbnailWidth={64}
        thumbnailHeight={64}
      />
      <ImageLightbox
        src="https://picsum.photos/800/600?random=2"
        alt="Image 2"
        thumbnailWidth={64}
        thumbnailHeight={64}
      />
      <ImageLightbox
        src="https://picsum.photos/800/600?random=3"
        alt="Image 3"
        thumbnailWidth={64}
        thumbnailHeight={64}
      />
    </div>
  ),
}
