import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { ArrowRight, Layers, Zap, BarChart3, Package, ShoppingCart, Settings } from 'lucide-react'
import { Card, CardContent, Badge, Button, Separator } from '@uikit'

// --- Visual Replica ---
// AnimatedOutlet depends on react-router-dom (Outlet, useLocation), framer-motion
// (AnimatePresence, motion), useMediaQuery, and the View Transitions API.
// This self-contained demo replicates the page transition concept: navigating between
// pages with a fade transition, demonstrating both the native View Transitions path
// and the framer-motion fallback path.

const pages = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    icon: BarChart3,
    color: 'bg-blue-50 dark:bg-blue-950/30',
    content: 'Overview metrics and charts',
    details: 'Revenue: $45,230 | Orders: 142 | Customers: 89',
  },
  {
    id: 'products',
    label: 'Products',
    icon: Package,
    color: 'bg-green-50 dark:bg-green-950/30',
    content: 'Product catalog management',
    details: 'Active: 234 | Draft: 12 | Archived: 45',
  },
  {
    id: 'orders',
    label: 'Orders',
    icon: ShoppingCart,
    color: 'bg-amber-50 dark:bg-amber-950/30',
    content: 'Order processing and tracking',
    details: 'Pending: 23 | Shipped: 67 | Delivered: 312',
  },
  {
    id: 'settings',
    label: 'Settings',
    icon: Settings,
    color: 'bg-purple-50 dark:bg-purple-950/30',
    content: 'Store configuration',
    details: 'Theme | Notifications | Integrations | API Keys',
  },
]

const AnimatedOutletDemo = () => {
  const [currentPage, setCurrentPage] = useState(0)
  const [transitioning, setTransitioning] = useState(false)
  const page = pages[currentPage]

  const navigate = (index: number) => {
    if (index === currentPage || transitioning) return
    setTransitioning(true)
    setTimeout(() => {
      setCurrentPage(index)
      setTimeout(() => setTransitioning(false), 150)
    }, 100)
  }

  return (
    <div className="space-y-4 max-w-lg">
      {/* Navigation bar */}
      <div className="flex gap-2 flex-wrap">
        {pages.map((p, i) => (
          <Button
            key={p.id}
            variant={i === currentPage ? 'default' : 'outline'}
            size="sm"
            onClick={() => navigate(i)}
            className="cursor-pointer"
          >
            <p.icon className="h-3.5 w-3.5 mr-1.5" />
            {p.label}
          </Button>
        ))}
      </div>

      {/* Animated content area */}
      <Card>
        <CardContent className="p-6">
          <div
            className={`transition-opacity duration-150 ${transitioning ? 'opacity-0' : 'opacity-100'} ${page.color} p-6 rounded-lg min-h-[200px]`}
          >
            <div className="flex items-center gap-2 mb-3">
              <page.icon className="h-5 w-5 text-foreground/70" />
              <h3 className="font-semibold text-lg">{page.label}</h3>
            </div>
            <p className="text-sm text-muted-foreground mb-4">{page.content}</p>
            <Separator className="my-3" />
            <p className="text-xs text-muted-foreground font-mono">{page.details}</p>
          </div>
        </CardContent>
      </Card>

      {/* Technology info */}
      <div className="flex items-center gap-4 text-xs text-muted-foreground">
        <div className="flex items-center gap-1.5">
          <Layers className="h-3 w-3" />
          <span>View Transitions API (Chrome 111+)</span>
        </div>
        <div className="flex items-center gap-1.5">
          <Zap className="h-3 w-3" />
          <span>framer-motion fallback</span>
        </div>
      </div>
    </div>
  )
}

// --- Reduced Motion Demo ---

const ReducedMotionDemo = () => {
  const [currentPage, setCurrentPage] = useState(0)
  const [transitioning, setTransitioning] = useState(false)
  const page = pages[currentPage]

  const navigate = (index: number) => {
    if (index === currentPage || transitioning) return
    setTransitioning(true)
    // Shorter transition to simulate reduced motion
    setTimeout(() => {
      setCurrentPage(index)
      setTimeout(() => setTransitioning(false), 100)
    }, 50)
  }

  return (
    <div className="space-y-4 max-w-lg">
      <Badge variant="outline" className="mb-2">
        prefers-reduced-motion: reduce
      </Badge>
      <div className="flex gap-2 flex-wrap">
        {pages.map((p, i) => (
          <Button
            key={p.id}
            variant={i === currentPage ? 'default' : 'outline'}
            size="sm"
            onClick={() => navigate(i)}
            className="cursor-pointer"
          >
            {p.label}
          </Button>
        ))}
      </div>
      <Card>
        <CardContent className="p-6">
          <div
            className={`transition-opacity duration-100 ${transitioning ? 'opacity-0' : 'opacity-100'} ${page.color} p-6 rounded-lg min-h-[200px]`}
          >
            <h3 className="font-semibold text-lg mb-2">{page.label}</h3>
            <p className="text-sm text-muted-foreground">{page.content}</p>
          </div>
        </CardContent>
      </Card>
      <p className="text-xs text-muted-foreground">
        Transition duration reduced from 150ms to 100ms. Respects user accessibility preferences.
      </p>
    </div>
  )
}

// --- Loading Fallback Demo ---

const LoadingFallbackDemo = () => (
  <div className="space-y-4 max-w-lg">
    <Badge variant="outline">Suspense fallback</Badge>
    <Card>
      <CardContent className="p-6">
        <div className="flex flex-col items-center justify-center min-h-[200px] gap-3">
          <div className="h-8 w-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
          <p className="text-sm text-muted-foreground">Loading...</p>
        </div>
      </CardContent>
    </Card>
    <p className="text-xs text-muted-foreground">
      Shown via React Suspense while lazy-loaded page components are being fetched.
      Uses PageLoader component by default.
    </p>
  </div>
)

// --- Meta ---

const meta = {
  title: 'UIKit/AnimatedOutlet',
  component: AnimatedOutletDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component:
          'AnimatedOutlet wraps react-router-dom Outlet with page transitions. ' +
          'When the browser supports the View Transitions API (Chrome 111+, Edge 111+, Firefox 144+, Safari 18+), ' +
          'it uses native compositor-thread animations via CSS view-transition-name. ' +
          'Falls back to framer-motion AnimatePresence for unsupported browsers. ' +
          'Respects prefers-reduced-motion. This is a visual replica — the real component depends on ' +
          'react-router-dom, framer-motion, and the View Transitions API.',
      },
    },
  },
} satisfies Meta<typeof AnimatedOutletDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Interactive demo of page transitions. Click navigation buttons to see the fade transition ' +
          'effect that replicates how AnimatedOutlet transitions between routes. ' +
          'The 100ms exit + 150ms enter timing matches the framer-motion fallback variants.',
      },
    },
  },
}

export const ReducedMotion: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'When prefers-reduced-motion is active, transition durations are shortened to 100ms (from 150ms). ' +
          'AnimatedOutlet detects this via useMediaQuery and switches to reducedMotionVariants.',
      },
    },
  },
  render: () => <ReducedMotionDemo />,
}

export const LoadingFallback: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'The Suspense fallback shown while lazy-loaded page components are being fetched. ' +
          'AnimatedOutlet wraps Outlet in Suspense and defaults to PageLoader when no custom fallback is provided.',
      },
    },
  },
  render: () => <LoadingFallbackDemo />,
}
