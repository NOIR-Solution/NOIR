import { StrictMode, Suspense } from 'react'
import { createRoot } from 'react-dom/client'
import { VibeKanbanWebCompanion } from 'vibe-kanban-web-companion'
import './index.css'
// Initialize i18n before App component
import './i18n'
import { LanguageProvider } from './i18n/LanguageContext'
import App from './App.tsx'

/**
 * AppLoadingSkeleton - Root-level loading skeleton
 * Shows during initial app bundle load before React hydrates
 * Uses CSS-only skeleton to avoid importing components before app loads
 */
const AppLoadingSkeleton = () => {
  return (
    <div className="flex h-screen bg-background">
      {/* Sidebar skeleton */}
      <div className="w-64 border-r bg-card p-4 space-y-4">
        <div className="h-8 w-32 bg-muted animate-pulse rounded" />
        <div className="space-y-2 pt-4">
          {[1, 2, 3, 4, 5].map((i) => (
            <div key={i} className="h-9 bg-muted animate-pulse rounded" />
          ))}
        </div>
      </div>
      {/* Main content skeleton */}
      <div className="flex-1 p-6 space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="h-10 w-10 bg-muted animate-pulse rounded-lg" />
            <div className="space-y-2">
              <div className="h-8 w-48 bg-muted animate-pulse rounded" />
              <div className="h-4 w-64 bg-muted animate-pulse rounded" />
            </div>
          </div>
          <div className="h-10 w-32 bg-muted animate-pulse rounded" />
        </div>
        {/* Card */}
        <div className="border rounded-lg p-6 space-y-4">
          <div className="flex items-center justify-between pb-4">
            <div className="space-y-2">
              <div className="h-5 w-24 bg-muted animate-pulse rounded" />
              <div className="h-4 w-40 bg-muted animate-pulse rounded" />
            </div>
            <div className="flex gap-2">
              <div className="h-10 w-48 bg-muted animate-pulse rounded" />
              <div className="h-10 w-20 bg-muted animate-pulse rounded" />
            </div>
          </div>
          {/* Table rows */}
          <div className="border rounded">
            {[1, 2, 3, 4, 5].map((i) => (
              <div key={i} className="border-b last:border-0 p-4 flex items-center gap-4">
                <div className="h-4 w-24 bg-muted animate-pulse rounded" />
                <div className="h-4 w-32 bg-muted animate-pulse rounded" />
                <div className="h-6 w-16 bg-muted animate-pulse rounded-full" />
                <div className="h-4 w-24 bg-muted animate-pulse rounded" />
                <div className="flex gap-2 ml-auto">
                  <div className="h-8 w-8 bg-muted animate-pulse rounded" />
                  <div className="h-8 w-8 bg-muted animate-pulse rounded" />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {import.meta.env.DEV && <VibeKanbanWebCompanion />}
    <Suspense fallback={<AppLoadingSkeleton />}>
      <LanguageProvider>
        <App />
      </LanguageProvider>
    </Suspense>
  </StrictMode>,
)
