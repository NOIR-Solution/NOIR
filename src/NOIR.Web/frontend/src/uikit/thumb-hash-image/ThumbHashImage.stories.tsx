import type { Meta, StoryObj } from 'storybook'
import { ThumbHashImage } from './ThumbHashImage'

const meta = {
  title: 'UIKit/ThumbHashImage',
  component: ThumbHashImage,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 400, padding: 16 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ThumbHashImage>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    src: 'https://placehold.co/400x300/2563eb/ffffff?text=Image',
    alt: 'Sample image',
    width: 400,
    height: 300,
  },
}

export const WithDominantColor: Story = {
  args: {
    src: 'https://placehold.co/400x300/10b981/ffffff?text=Green',
    alt: 'Image with dominant color placeholder',
    dominantColor: '#10b981',
    width: 400,
    height: 300,
  },
}

export const ObjectContain: Story = {
  args: {
    src: 'https://placehold.co/400x300/8b5cf6/ffffff?text=Contain',
    alt: 'Contained image',
    width: 400,
    height: 300,
    objectFit: 'contain',
    dominantColor: '#f3f4f6',
  },
}

export const BrokenImage: Story = {
  args: {
    src: 'https://invalid-url.example/broken.jpg',
    alt: 'Broken image shows error state',
    width: 400,
    height: 300,
    dominantColor: '#e5e7eb',
  },
}

export const LazyLoading: Story = {
  args: {
    src: 'https://placehold.co/400x300/f59e0b/ffffff?text=Lazy',
    alt: 'Lazy loaded image',
    width: 400,
    height: 300,
    loading: 'lazy',
    dominantColor: '#f59e0b',
  },
}

export const EagerLoading: Story = {
  args: {
    src: 'https://placehold.co/400x300/ef4444/ffffff?text=Eager',
    alt: 'Eagerly loaded image',
    width: 400,
    height: 300,
    loading: 'eager',
    dominantColor: '#ef4444',
  },
}

export const CustomSize: Story = {
  args: {
    src: 'https://placehold.co/200x200/6366f1/ffffff?text=200x200',
    alt: 'Custom sized image',
    width: 200,
    height: 200,
    dominantColor: '#6366f1',
  },
}

export const StringDimensions: Story = {
  args: {
    src: 'https://placehold.co/600x200/ec4899/ffffff?text=Banner',
    alt: 'Banner image with string dimensions',
    width: '100%',
    height: '120px',
    dominantColor: '#ec4899',
  },
}

export const ScaleDown: Story = {
  args: {
    src: 'https://placehold.co/100x100/0ea5e9/ffffff?text=Small',
    alt: 'Scale-down fit',
    width: 400,
    height: 300,
    objectFit: 'scale-down',
    dominantColor: '#e5e7eb',
  },
}
