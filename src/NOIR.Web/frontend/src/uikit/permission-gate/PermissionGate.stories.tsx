import type { Meta, StoryObj } from 'storybook'
import { ShieldCheck, ShieldX, Lock, UserCheck } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@uikit'
import { Badge } from '@uikit'
import { Button } from '@uikit'

// --- Visual Replica ---
// PermissionGate and RoleGate depend on usePermissions hook (AuthContext, API calls).
// This self-contained demo replicates the visual behavior: rendering children when
// access is granted, a fallback when denied with fallback, or nothing when denied
// without fallback. No external auth context required.

interface PermissionGateDemoProps {
  hasAccess?: boolean
  showFallback?: boolean
  permissionLabel?: string
}

const PermissionGateDemo = ({
  hasAccess = true,
  showFallback = false,
  permissionLabel = 'users:create',
}: PermissionGateDemoProps) => (
  <div className="space-y-4 max-w-md">
    <div className="flex items-center gap-2 text-sm text-muted-foreground">
      <Badge variant="outline">{permissionLabel}</Badge>
      <span>{hasAccess ? '\u2192 Granted' : '\u2192 Denied'}</span>
    </div>
    {hasAccess ? (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <ShieldCheck className="h-5 w-5 text-green-500" />
            Protected Content
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            This content is only visible to users with the required permission.
          </p>
          <Button className="mt-4 cursor-pointer" size="sm">
            Create User
          </Button>
        </CardContent>
      </Card>
    ) : showFallback ? (
      <Card className="border-destructive/30 bg-destructive/5">
        <CardContent className="flex flex-col items-center justify-center py-8 text-center space-y-3">
          <ShieldX className="h-12 w-12 text-destructive/60" />
          <h3 className="font-semibold">Access Denied</h3>
          <p className="text-sm text-muted-foreground max-w-sm">
            You do not have the required permissions to access this content.
          </p>
        </CardContent>
      </Card>
    ) : (
      <div className="p-8 border border-dashed rounded-lg text-center text-muted-foreground text-sm">
        <Lock className="h-6 w-6 mx-auto mb-2 opacity-50" />
        Content hidden (no fallback)
      </div>
    )}
  </div>
)

// --- RoleGate Demo ---

interface RoleGateDemoProps {
  userRole?: string
  requiredRoles?: string[]
  requireAll?: boolean
}

const RoleGateDemo = ({
  userRole = 'Admin',
  requiredRoles = ['Admin', 'Manager'],
  requireAll = false,
}: RoleGateDemoProps) => {
  const hasAccess = requireAll
    ? requiredRoles.every((r) => r === userRole)
    : requiredRoles.some((r) => r === userRole)

  return (
    <div className="space-y-4 max-w-md">
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <Badge variant="secondary">
          <UserCheck className="h-3 w-3 mr-1" />
          Role: {userRole}
        </Badge>
        <span className="text-xs">
          {requireAll ? 'Requires ALL:' : 'Requires ANY:'}
        </span>
        {requiredRoles.map((role) => (
          <Badge key={role} variant="outline">
            {role}
          </Badge>
        ))}
      </div>
      <div className="flex items-center gap-2 text-sm">
        {hasAccess ? (
          <Badge className="bg-green-100 text-green-800 dark:bg-green-950/40 dark:text-green-400">
            Access Granted
          </Badge>
        ) : (
          <Badge variant="destructive">Access Denied</Badge>
        )}
      </div>
      {hasAccess ? (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <UserCheck className="h-5 w-5 text-green-500" />
              Role-Protected Content
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              This content is visible because the user has the required role.
            </p>
            <Button className="mt-4 cursor-pointer" size="sm">
              Manage Team
            </Button>
          </CardContent>
        </Card>
      ) : (
        <Card className="border-destructive/30 bg-destructive/5">
          <CardContent className="flex flex-col items-center justify-center py-8 text-center space-y-3">
            <ShieldX className="h-12 w-12 text-destructive/60" />
            <h3 className="font-semibold">Insufficient Role</h3>
            <p className="text-sm text-muted-foreground max-w-sm">
              Your current role does not grant access to this section.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

// --- Meta ---

const meta = {
  title: 'UIKit/PermissionGate',
  component: PermissionGateDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component:
          'PermissionGate and RoleGate conditionally render children based on user permissions or roles. ' +
          'This is a visual replica — the real components use usePermissions hook (AuthContext, API). ' +
          'PermissionGate checks permission keys (e.g., "users:create"), RoleGate checks role names (e.g., "Admin").',
      },
    },
  },
} satisfies Meta<typeof PermissionGateDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Granted: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'User has the required permission. The protected content (children) is rendered. ' +
          'This is the default state when PermissionGate resolves access as granted.',
      },
    },
  },
  args: {
    hasAccess: true,
    permissionLabel: 'users:create',
  },
}

export const Denied: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'User lacks the required permission and no fallback is provided. ' +
          'PermissionGate renders null (nothing visible). The dashed outline here ' +
          'is for illustration only — in production, the area would be empty.',
      },
    },
  },
  args: {
    hasAccess: false,
    showFallback: false,
    permissionLabel: 'admin:access',
  },
}

export const DeniedWithFallback: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'User lacks the required permission but a fallback prop is provided. ' +
          'PermissionGate renders the fallback content instead of children, ' +
          'showing a styled "Access Denied" message.',
      },
    },
  },
  args: {
    hasAccess: false,
    showFallback: true,
    permissionLabel: 'admin:access',
  },
}

export const RoleBasedAccess: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'RoleGate variant: conditionally renders based on user roles instead of permissions. ' +
          'Supports single role, multiple roles with ANY (default) or ALL matching. ' +
          'Here the user has the "Admin" role which matches one of the required roles.',
      },
    },
  },
  render: () => <RoleGateDemo userRole="Admin" requiredRoles={['Admin', 'Manager']} />,
}

export const RoleBasedDenied: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'RoleGate when the user role does not match any of the required roles. ' +
          'Shows the denied state with an "Insufficient Role" message.',
      },
    },
  },
  render: () => <RoleGateDemo userRole="Viewer" requiredRoles={['Admin', 'Manager']} />,
}

export const MultiplePermissions: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'PermissionGate with multiple permissions displayed. In the real component, ' +
          'requireAll=false checks if the user has ANY of the listed permissions, while ' +
          'requireAll=true requires ALL permissions to be present.',
      },
    },
  },
  args: {
    hasAccess: true,
    permissionLabel: 'users:read, users:update',
  },
}
