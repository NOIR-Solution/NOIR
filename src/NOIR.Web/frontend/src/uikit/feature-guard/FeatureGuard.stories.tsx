import type { Meta, StoryObj } from 'storybook'
import { ShieldOff, Package, BarChart3, Lock } from 'lucide-react'
import { Button, Card, CardContent, CardHeader, CardTitle } from '@uikit'

// --- Visual Replica ---
// FeatureGuard depends on useFeature hook (FusionCache-backed feature flags) and
// react-i18next. This self-contained demo shows the three visual states:
// enabled (renders children), disabled (default fallback), and custom fallback.

interface FeatureGuardDemoProps {
  /** Whether the feature is enabled */
  isEnabled?: boolean
  /** Custom fallback content (leave empty for default) */
  customFallback?: boolean
}

const FeatureGuardDemo = ({
  isEnabled = true,
  customFallback = false,
}: FeatureGuardDemoProps) => {
  // Enabled state — render children
  if (isEnabled) {
    return (
      <Card className="w-full max-w-lg shadow-sm">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Package className="h-5 w-5" />
            Product Management
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <p className="text-sm text-muted-foreground">
            This content is protected by FeatureGuard. It only renders when the feature
            module is enabled for the current tenant.
          </p>
          <div className="flex gap-2">
            <Button size="sm" className="cursor-pointer">
              Create Product
            </Button>
            <Button size="sm" variant="outline" className="cursor-pointer">
              Import
            </Button>
          </div>
        </CardContent>
      </Card>
    )
  }

  // Custom fallback
  if (customFallback) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] text-center space-y-4">
        <div className="p-4 rounded-2xl bg-amber-100 dark:bg-amber-950/40">
          <Lock className="h-12 w-12 text-amber-600 dark:text-amber-400" />
        </div>
        <h2 className="text-xl font-semibold">Upgrade Required</h2>
        <p className="text-muted-foreground max-w-md">
          The Analytics module requires a Business plan or higher. Contact your
          administrator to upgrade.
        </p>
        <Button variant="outline" className="cursor-pointer">
          <BarChart3 className="h-4 w-4 mr-2" />
          View Plans
        </Button>
      </div>
    )
  }

  // Default fallback — exact styling from the real component
  return (
    <div className="flex flex-col items-center justify-center min-h-[400px] text-center space-y-4">
      <ShieldOff className="h-16 w-16 text-muted-foreground" />
      <h2 className="text-xl font-semibold">Module Not Available</h2>
      <p className="text-muted-foreground max-w-md">
        This module is not available for your organization. Contact your administrator
        for access.
      </p>
    </div>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/FeatureGuard',
  component: FeatureGuardDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component:
          'Guards child routes and components by feature availability. When a feature module ' +
          'is disabled for the current tenant, it renders a fallback instead of the protected ' +
          'content. Supports default and custom fallback states. This is a visual replica ' +
          '— the real component uses useFeature hook (FusionCache-backed) and react-i18next.',
      },
    },
  },
} satisfies Meta<typeof FeatureGuardDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Enabled: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Feature is enabled — the guard renders its children content normally. ' +
          'This is the happy path where the tenant has access to the module.',
      },
    },
  },
  args: {
    isEnabled: true,
    customFallback: false,
  },
}

export const Disabled: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Feature is disabled — the guard renders the default fallback with a ShieldOff ' +
          'icon, "Module Not Available" heading, and a message to contact the administrator.',
      },
    },
  },
  args: {
    isEnabled: false,
    customFallback: false,
  },
}

export const CustomFallback: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Feature is disabled with a custom fallback. Pages can provide their own fallback ' +
          'UI, such as an upgrade prompt with plan-specific messaging and action buttons.',
      },
    },
  },
  args: {
    isEnabled: false,
    customFallback: true,
  },
}
