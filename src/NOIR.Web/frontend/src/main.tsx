import { StrictMode, Suspense } from 'react'
import { createRoot } from 'react-dom/client'
import { VibeKanbanWebCompanion } from 'vibe-kanban-web-companion'
import { Loader2 } from 'lucide-react'
import './index.css'
// Initialize i18n before App component
import './i18n'
import { LanguageProvider } from './i18n/LanguageContext'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {import.meta.env.DEV && <VibeKanbanWebCompanion />}
    <Suspense fallback={
      <div className="flex items-center justify-center h-screen bg-background">
        {/* Standardized spinner using Loader2 for consistency */}
        <Loader2 className="h-10 w-10 animate-spin text-primary" />
      </div>
    }>
      <LanguageProvider>
        <App />
      </LanguageProvider>
    </Suspense>
  </StrictMode>,
)
