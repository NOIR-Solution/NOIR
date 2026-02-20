import type { Meta, StoryObj } from 'storybook'
import { WifiOff, Wifi } from 'lucide-react'

/**
 * OfflineIndicator stories
 *
 * The real OfflineIndicator component uses useNetworkStatus() hook and framer-motion
 * animations internally. Since Storybook cannot easily mock browser network state,
 * we render the visual output directly to showcase the banner styles.
 */
const meta = {
  title: 'UIKit/OfflineIndicator',
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
} satisfies Meta

export default meta
type Story = StoryObj<typeof meta>

export const OfflineBanner: Story = {
  render: () => (
    <div style={{ position: 'relative', height: '200px', background: '#f5f5f5' }}>
      <div
        className="fixed bottom-4 left-4 z-50 flex items-center gap-2 px-4 py-2.5
                   bg-amber-100 dark:bg-amber-900/90 border border-amber-300 dark:border-amber-700
                   rounded-lg shadow-lg"
        style={{ position: 'absolute' }}
        role="alert"
        aria-live="assertive"
      >
        <WifiOff className="h-4 w-4 text-amber-600 dark:text-amber-400 flex-shrink-0" />
        <span className="text-sm font-medium text-amber-800 dark:text-amber-200">
          You're offline
        </span>
      </div>
    </div>
  ),
}

export const BackOnlineBanner: Story = {
  render: () => (
    <div style={{ position: 'relative', height: '200px', background: '#f5f5f5' }}>
      <div
        className="fixed bottom-4 left-4 z-50 flex items-center gap-2 px-4 py-2.5
                   bg-green-100 dark:bg-green-900/90 border border-green-300 dark:border-green-700
                   rounded-lg shadow-lg"
        style={{ position: 'absolute' }}
        role="status"
        aria-live="polite"
      >
        <Wifi className="h-4 w-4 text-green-600 dark:text-green-400 flex-shrink-0" />
        <span className="text-sm font-medium text-green-800 dark:text-green-200">
          Back online
        </span>
      </div>
    </div>
  ),
}

export const OnlineState: Story = {
  render: () => (
    <div style={{ position: 'relative', height: '200px', background: '#f5f5f5', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <p style={{ fontSize: '14px', color: '#888' }}>
        When online, the OfflineIndicator renders nothing (hidden).
      </p>
    </div>
  ),
}

export const BothBanners: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px', padding: '24px' }}>
      <div>
        <p style={{ marginBottom: '12px', fontSize: '14px', fontWeight: 500 }}>Offline Banner</p>
        <div
          className="flex items-center gap-2 px-4 py-2.5
                     bg-amber-100 border border-amber-300 rounded-lg shadow-lg"
          style={{ width: 'fit-content' }}
          role="alert"
        >
          <WifiOff className="h-4 w-4 text-amber-600 flex-shrink-0" />
          <span className="text-sm font-medium text-amber-800">
            You're offline
          </span>
        </div>
      </div>
      <div>
        <p style={{ marginBottom: '12px', fontSize: '14px', fontWeight: 500 }}>Back Online Banner</p>
        <div
          className="flex items-center gap-2 px-4 py-2.5
                     bg-green-100 border border-green-300 rounded-lg shadow-lg"
          style={{ width: 'fit-content' }}
          role="status"
        >
          <Wifi className="h-4 w-4 text-green-600 flex-shrink-0" />
          <span className="text-sm font-medium text-green-800">
            Back online
          </span>
        </div>
      </div>
    </div>
  ),
}
