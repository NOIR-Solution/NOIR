import type { Meta, StoryObj } from 'storybook'
import { ShieldCheck, LogIn, Loader2, ShieldX, ArrowRight, ExternalLink } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle, Badge, Button, Separator } from '@uikit'

// --- Visual Replica ---
// ProtectedRoute depends on AuthContext (isAuthenticated, isLoading), usePermissions
// hook, react-router-dom (Navigate, useLocation), and PageSpinner.
// This self-contained demo replicates the four visual states: loading, authenticated,
// unauthenticated (redirect to login), and insufficient permissions (redirect to portal).

type AuthState = 'loading' | 'authenticated' | 'unauthenticated' | 'no-permission'

interface ProtectedRouteDemoProps {
  state?: AuthState
  permission?: string
  redirectPath?: string
}

const ProtectedRouteDemo = ({
  state = 'authenticated',
  permission = 'users:read',
  redirectPath = '/portal',
}: ProtectedRouteDemoProps) => (
  <div className="space-y-4 max-w-md">
    {/* State indicator */}
    <div className="flex items-center gap-2">
      <Badge
        variant={
          state === 'authenticated'
            ? 'default'
            : state === 'loading'
              ? 'secondary'
              : 'destructive'
        }
      >
        {state}
      </Badge>
      {permission && <Badge variant="outline">{permission}</Badge>}
    </div>

    {/* Rendered output based on state */}
    {state === 'loading' && (
      <Card>
        <CardContent className="p-0">
          <div className="min-h-[300px] flex flex-col items-center justify-center bg-background rounded-lg gap-3">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
            <p className="text-sm text-muted-foreground">Verifying authentication...</p>
          </div>
        </CardContent>
      </Card>
    )}

    {state === 'authenticated' && (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-lg">
            <ShieldCheck className="h-5 w-5 text-green-500" />
            Protected Page Content
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <p className="text-sm text-muted-foreground">
            User is authenticated and has the required permissions. The route children are rendered normally.
          </p>
          <Separator />
          <div className="bg-muted/50 rounded-lg p-4 space-y-2">
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Auth status</span>
              <Badge className="bg-green-100 text-green-800 dark:bg-green-950/40 dark:text-green-400">
                Authenticated
              </Badge>
            </div>
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Permission check</span>
              <Badge className="bg-green-100 text-green-800 dark:bg-green-950/40 dark:text-green-400">
                Passed
              </Badge>
            </div>
          </div>
          <Button className="w-full cursor-pointer" size="sm">
            Access Dashboard
          </Button>
        </CardContent>
      </Card>
    )}

    {state === 'unauthenticated' && (
      <Card className="border-amber-300/50 bg-amber-50/50 dark:border-amber-800/30 dark:bg-amber-950/20">
        <CardContent className="flex flex-col items-center justify-center py-8 text-center space-y-4">
          <div className="p-3 rounded-full bg-amber-100 dark:bg-amber-950/40">
            <LogIn className="h-8 w-8 text-amber-600 dark:text-amber-400" />
          </div>
          <div className="space-y-1">
            <h3 className="font-semibold">Authentication Required</h3>
            <p className="text-sm text-muted-foreground max-w-sm">
              You are not logged in. Redirecting to the login page...
            </p>
          </div>
          <div className="flex items-center gap-2 text-xs text-muted-foreground bg-muted/50 rounded-md px-3 py-2 font-mono">
            <ArrowRight className="h-3 w-3" />
            <span>/login?returnUrl=%2Fportal%2Fusers</span>
          </div>
          <p className="text-xs text-muted-foreground">
            The current URL is preserved as returnUrl for post-login redirect.
          </p>
        </CardContent>
      </Card>
    )}

    {state === 'no-permission' && (
      <Card className="border-destructive/30 bg-destructive/5">
        <CardContent className="flex flex-col items-center justify-center py-8 text-center space-y-4">
          <div className="p-3 rounded-full bg-destructive/10">
            <ShieldX className="h-8 w-8 text-destructive/70" />
          </div>
          <div className="space-y-1">
            <h3 className="font-semibold">Insufficient Permissions</h3>
            <p className="text-sm text-muted-foreground max-w-sm">
              You are authenticated but lack the required permissions. Redirecting...
            </p>
          </div>
          <div className="flex items-center gap-2 text-xs text-muted-foreground bg-muted/50 rounded-md px-3 py-2 font-mono">
            <ArrowRight className="h-3 w-3" />
            <span>{redirectPath}</span>
          </div>
          <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <span>Required:</span>
            <Badge variant="outline" className="text-xs">
              {permission}
            </Badge>
          </div>
        </CardContent>
      </Card>
    )}
  </div>
)

// --- All States Overview ---

const AllStatesDemo = () => (
  <div className="grid grid-cols-1 md:grid-cols-2 gap-6 max-w-3xl">
    <div className="space-y-2">
      <h4 className="text-sm font-medium text-muted-foreground">1. Loading</h4>
      <ProtectedRouteDemo state="loading" permission="users:read" />
    </div>
    <div className="space-y-2">
      <h4 className="text-sm font-medium text-muted-foreground">2. Authenticated</h4>
      <ProtectedRouteDemo state="authenticated" permission="users:read" />
    </div>
    <div className="space-y-2">
      <h4 className="text-sm font-medium text-muted-foreground">3. Unauthenticated</h4>
      <ProtectedRouteDemo state="unauthenticated" permission="users:read" />
    </div>
    <div className="space-y-2">
      <h4 className="text-sm font-medium text-muted-foreground">4. No Permission</h4>
      <ProtectedRouteDemo state="no-permission" permission="admin:access" />
    </div>
  </div>
)

// --- Meta ---

const meta = {
  title: 'UIKit/ProtectedRoute',
  component: ProtectedRouteDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
    docs: {
      description: {
        component:
          'ProtectedRoute guards routes requiring authentication and optionally specific permissions. ' +
          'It renders a loading spinner while checking auth, redirects to /login if unauthenticated ' +
          '(preserving returnUrl), redirects to /portal if permissions are insufficient, and renders ' +
          'children when all checks pass. This is a visual replica — the real component depends on ' +
          'AuthContext, usePermissions hook, and react-router-dom Navigate.',
      },
    },
  },
  argTypes: {
    state: {
      control: 'select',
      options: ['loading', 'authenticated', 'unauthenticated', 'no-permission'],
      description: 'The authentication/authorization state to display',
    },
    permission: {
      control: 'text',
      description: 'The permission key being checked',
    },
    redirectPath: {
      control: 'text',
      description: 'Where to redirect when permissions are insufficient',
    },
  },
} satisfies Meta<typeof ProtectedRouteDemo>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Authenticated: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'User is authenticated and has the required permission. ProtectedRoute renders its children normally. ' +
          'This is the happy path — both auth and permission checks pass.',
      },
    },
  },
  args: {
    state: 'authenticated',
    permission: 'users:read',
  },
}

export const Loading: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'Auth state is still being determined (authLoading or permissionsLoading is true). ' +
          'ProtectedRoute renders a full-screen centered PageSpinner. ' +
          'The real component uses min-h-screen with flex centering and bg-background.',
      },
    },
  },
  args: {
    state: 'loading',
    permission: 'users:read',
  },
}

export const Unauthenticated: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'User is not logged in. ProtectedRoute renders <Navigate to="/login?returnUrl=..." replace />. ' +
          'The current pathname and search params are encoded into returnUrl for post-login redirect. ' +
          'Uses replace to avoid adding the protected URL to browser history.',
      },
    },
  },
  args: {
    state: 'unauthenticated',
    permission: 'users:read',
  },
}

export const InsufficientPermissions: Story = {
  parameters: {
    docs: {
      description: {
        story:
          'User is authenticated but lacks the required permission(s). ProtectedRoute renders ' +
          '<Navigate to={redirectTo} replace />. Default redirect is /portal. ' +
          'Supports single permission, multiple with ANY match (default), or ALL match (requireAll=true).',
      },
    },
  },
  args: {
    state: 'no-permission',
    permission: 'admin:access',
    redirectPath: '/portal',
  },
}

export const AllStates: Story = {
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        story:
          'Overview of all four ProtectedRoute states side by side: Loading, Authenticated, ' +
          'Unauthenticated, and Insufficient Permissions. Useful for comparing the visual treatment of each state.',
      },
    },
  },
  render: () => <AllStatesDemo />,
}
