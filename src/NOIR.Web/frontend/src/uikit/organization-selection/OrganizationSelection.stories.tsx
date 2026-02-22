import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { Building2, ArrowLeft, Check, Loader2 } from 'lucide-react'
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle } from '@uikit'

import { cn } from '@/lib/utils'

// --- Visual Replica ---
// OrganizationSelection depends on framer-motion, react-router-dom, and the TenantOption
// type from auth contexts. This self-contained demo replicates the visual appearance
// without requiring those external dependencies. Animations are replaced with CSS transitions.

interface MockOrg {
  tenantId: string | null
  name: string
  identifier: string
}

const mockOrgs: MockOrg[] = [
  { tenantId: 'org-1', name: 'Acme Corporation', identifier: 'acme-corp' },
  { tenantId: 'org-2', name: 'Globex Corporation', identifier: 'globex' },
  { tenantId: null, name: 'Platform Administration', identifier: 'platform' },
]

interface OrganizationSelectionDemoProps {
  /** List of organizations to display */
  organizations?: MockOrg[]
  /** Email address displayed in the header */
  userEmail?: string
  /** Show the continue button in loading state */
  isLoading?: boolean
  /** Error message to display */
  error?: string | null
  /** Pre-selected organization index (0-based) */
  preSelectedIndex?: number | null
}

const OrganizationSelectionDemo = ({
  organizations = mockOrgs,
  userEmail = 'user@example.com',
  isLoading = false,
  error = null,
  preSelectedIndex = null,
}: OrganizationSelectionDemoProps) => {
  const [selectedOrg, setSelectedOrg] = useState<MockOrg | null>(
    preSelectedIndex !== null ? (organizations[preSelectedIndex] ?? null) : null
  )

  const handleSelect = (org: MockOrg) => {
    if (!isLoading) {
      setSelectedOrg(org)
    }
  }

  return (
    <div className="w-full max-w-md space-y-8">
      {/* Back Button */}
      <Button
        variant="ghost"
        size="sm"
        disabled={isLoading}
        className="text-muted-foreground hover:text-foreground cursor-pointer"
      >
        <ArrowLeft className="mr-2 h-4 w-4" />
        Back to login
      </Button>

      {/* Header */}
      <div className="text-center space-y-2">
        <div className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-blue-700 to-cyan-700 shadow-xl mb-4">
          <Building2 className="h-8 w-8 text-white" />
        </div>

        <h1 className="text-2xl font-bold tracking-tight text-foreground">
          Select Organization
        </h1>
        <p className="text-sm text-muted-foreground">
          <span className="font-medium text-foreground">{userEmail}</span> has access to
          multiple organizations
        </p>
      </div>

      {/* Organization Selection Card */}
      <Card className="backdrop-blur-xl bg-background/80 border-border/50 shadow-2xl">
        <CardHeader>
          <CardTitle className="text-lg">Choose Organization</CardTitle>
          <CardDescription>
            Select which organization you want to sign in to
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Error Message */}
          {error && (
            <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20">
              <p className="text-sm text-destructive font-medium">{error}</p>
            </div>
          )}

          {/* Organization List */}
          <div className="space-y-2">
            {organizations.map((org) => {
              const orgKey = org.tenantId || 'platform'
              const isSelected = selectedOrg?.tenantId === org.tenantId

              return (
                <button
                  key={orgKey}
                  type="button"
                  onClick={() => handleSelect(org)}
                  disabled={isLoading}
                  className={cn(
                    'w-full text-left p-4 rounded-lg border-2 transition-all cursor-pointer',
                    'hover:bg-accent/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                    isSelected
                      ? 'border-primary bg-primary/5'
                      : 'border-border hover:border-primary/50',
                    isLoading && 'opacity-50 cursor-not-allowed'
                  )}
                >
                  <div className="flex items-center gap-3">
                    {/* Icon */}
                    <div
                      className={cn(
                        'flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center transition-colors',
                        isSelected ? 'bg-primary/20' : 'bg-muted'
                      )}
                    >
                      <Building2
                        className={cn(
                          'h-5 w-5',
                          isSelected ? 'text-primary' : 'text-muted-foreground'
                        )}
                      />
                    </div>

                    {/* Name and Identifier */}
                    <div className="flex-1 min-w-0">
                      <div className="font-semibold text-foreground truncate">
                        {org.name}
                      </div>
                      {org.identifier && (
                        <div className="text-xs text-muted-foreground truncate">
                          {org.identifier}
                        </div>
                      )}
                    </div>

                    {/* Check Mark */}
                    {isSelected && (
                      <div className="flex-shrink-0">
                        <div className="w-5 h-5 rounded-full bg-primary flex items-center justify-center">
                          <Check className="h-3 w-3 text-primary-foreground" />
                        </div>
                      </div>
                    )}
                  </div>
                </button>
              )
            })}
          </div>

          {/* Continue Button */}
          <Button
            disabled={!selectedOrg || isLoading}
            className="w-full h-12 text-base font-semibold rounded-xl cursor-pointer transition-all duration-200"
          >
            {isLoading ? (
              <span className="flex items-center gap-2">
                <Loader2 className="h-5 w-5 animate-spin" />
                Signing in...
              </span>
            ) : (
              'Continue'
            )}
          </Button>
        </CardContent>
      </Card>

      {/* Footer */}
      <div className="text-center">
        <p className="text-xs text-muted-foreground">
          Select an organization to access your account
        </p>
      </div>
    </div>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/OrganizationSelection',
  component: OrganizationSelectionDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component:
          'Organization selection screen shown during multi-tenant login when a user ' +
          'belongs to more than one organization. Displays organization cards with ' +
          'selection state and a continue button. This is a visual replica ' +
          '— the real component uses framer-motion, react-router-dom, and auth contexts.',
      },
    },
  },
} satisfies Meta<typeof OrganizationSelectionDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Default state with three organizations to choose from, including a platform ' +
          'administration option. Click an organization card to select it, then Continue.',
      },
    },
  },
  args: {
    organizations: mockOrgs,
    userEmail: 'admin@acme.com',
  },
}

export const SingleOrg: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Single organization available. In production, the real component may auto-select ' +
          'and skip this screen when only one org is available.',
      },
    },
  },
  args: {
    organizations: [
      { tenantId: 'org-1', name: 'Acme Corporation', identifier: 'acme-corp' },
    ],
    userEmail: 'user@acme.com',
  },
}

export const Loading: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Loading state after clicking Continue. Organization cards are disabled and the ' +
          'button shows a spinner with "Signing in..." text.',
      },
    },
  },
  args: {
    organizations: mockOrgs,
    userEmail: 'admin@acme.com',
    isLoading: true,
    preSelectedIndex: 0,
  },
}

export const WithError: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Error state displayed when organization sign-in fails. Shows a destructive-styled ' +
          'alert above the organization list.',
      },
    },
  },
  args: {
    organizations: mockOrgs,
    userEmail: 'admin@acme.com',
    error: 'Unable to connect to the selected organization. Please try again.',
  },
}

export const Selected: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Pre-selected organization state. The selected card shows a primary border, ' +
          'highlighted icon background, and a checkmark indicator.',
      },
    },
  },
  args: {
    organizations: mockOrgs,
    userEmail: 'admin@acme.com',
    preSelectedIndex: 1,
  },
}
