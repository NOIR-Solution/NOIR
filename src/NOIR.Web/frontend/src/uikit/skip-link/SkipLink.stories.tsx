import type { Meta, StoryObj } from 'storybook'

// --- Visual Replica ---
// SkipLink depends on react-i18next (useTranslation). This self-contained demo
// replicates the visual appearance and keyboard behavior without requiring i18n context.

interface SkipLinkDemoProps {
  /** ID of the element to skip to */
  targetId?: string
  /** Link label text */
  label?: string
}

const SkipLinkDemo = ({
  targetId = 'main-content',
  label = 'Skip to main content',
}: SkipLinkDemoProps) => (
  <div className="relative p-8">
    <p className="text-sm text-muted-foreground mb-4">
      Press{' '}
      <kbd className="px-1.5 py-0.5 text-xs border rounded bg-muted font-mono">Tab</kbd>{' '}
      to reveal the skip link
    </p>

    <a
      href={`#${targetId}`}
      className="sr-only focus:not-sr-only focus:fixed focus:top-4 focus:left-4 focus:z-[100]
                 focus:px-4 focus:py-2 focus:bg-primary focus:text-primary-foreground
                 focus:rounded-md focus:shadow-lg focus:outline-none focus:ring-2 focus:ring-ring
                 focus:ring-offset-2 focus:ring-offset-background transition-all"
    >
      {label}
    </a>

    <div id={targetId} className="p-4 border rounded-lg bg-muted/50">
      <h2 className="font-semibold">Main Content Area</h2>
      <p className="text-sm text-muted-foreground">
        This is where the skip link navigates to.
      </p>
    </div>
  </div>
)

// --- Meta ---

const meta = {
  title: 'UIKit/SkipLink',
  component: SkipLinkDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Accessibility component for keyboard users to skip navigation. Visually hidden ' +
          'by default, becomes visible on focus via Tab key. Should be placed at the very ' +
          'top of the page. This is a visual replica — the real component uses react-i18next.',
      },
    },
  },
} satisfies Meta<typeof SkipLinkDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Default skip link targeting "main-content". Press Tab to reveal the link, ' +
          'which appears as a fixed badge in the top-left corner.',
      },
    },
  },
  args: {
    targetId: 'main-content',
    label: 'Skip to main content',
  },
}

export const CustomLabel: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Skip link with a custom label and target. Useful for pages with multiple ' +
          'landmarks that users might want to jump to.',
      },
    },
  },
  args: {
    targetId: 'sidebar',
    label: 'Skip to sidebar',
  },
}
