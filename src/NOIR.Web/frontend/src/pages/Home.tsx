import { useAuthContext } from "@/contexts/AuthContext"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { ShieldCheck, LogOut, ExternalLink } from "lucide-react"
import { useNavigate } from "react-router-dom"
import { themeClasses } from "@/config/theme"

export default function HomePage() {
  // User is guaranteed to exist because ProtectedRoute guards this component
  const { user, logout } = useAuthContext()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  // User is guaranteed by ProtectedRoute, but TypeScript needs this check
  if (!user) return null

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white border-b border-gray-200 px-4 py-4">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className={`w-10 h-10 rounded-xl ${themeClasses.iconContainer} flex items-center justify-center`}>
              <ShieldCheck className="w-5 h-5 text-white" />
            </div>
            <span className="font-bold text-xl text-gray-900">NOIR</span>
          </div>
          <div className="flex items-center gap-4">
            <span className="text-sm text-gray-600">
              {user.email}
            </span>
            <Button variant="ghost" size="sm" onClick={handleLogout}>
              <LogOut className="w-4 h-4 mr-2" />
              Logout
            </Button>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto p-4 sm:p-6 lg:p-8">
        <div className="mb-8">
          <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
          <p className="text-gray-600">Welcome back, {user.fullName}!</p>
        </div>

        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          <Card>
            <CardHeader>
              <CardTitle>API Documentation</CardTitle>
              <CardDescription>Explore and test API endpoints</CardDescription>
            </CardHeader>
            <CardContent>
              <a
                href="/api/docs"
                target="_blank"
                rel="noopener noreferrer"
                className={`inline-flex items-center ${themeClasses.linkPrimary}`}
              >
                Open API Docs
                <ExternalLink className="w-4 h-4 ml-1" />
              </a>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Hangfire Dashboard</CardTitle>
              <CardDescription>Monitor background jobs</CardDescription>
            </CardHeader>
            <CardContent>
              <a
                href="/hangfire"
                target="_blank"
                rel="noopener noreferrer"
                className={`inline-flex items-center ${themeClasses.linkPrimary}`}
              >
                Open Hangfire
                <ExternalLink className="w-4 h-4 ml-1" />
              </a>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Your Profile</CardTitle>
              <CardDescription>Account information</CardDescription>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500">Email:</span>
                <span className="font-medium">{user.email}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Tenant:</span>
                <span className="font-medium">{user.tenantId || 'Not assigned'}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Roles:</span>
                <span className="font-medium">{user.roles?.join(', ') || 'None'}</span>
              </div>
            </CardContent>
          </Card>
        </div>
      </main>
    </div>
  )
}
