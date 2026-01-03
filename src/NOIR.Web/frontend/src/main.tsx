import { StrictMode, Suspense } from 'react'
import { createRoot } from 'react-dom/client'
import { VibeKanbanWebCompanion } from 'vibe-kanban-web-companion'
import './index.css'
// Initialize i18n before App component
import './i18n'
import { LanguageProvider } from './i18n/LanguageContext'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    {import.meta.env.DEV && <VibeKanbanWebCompanion />}
    <Suspense fallback={
      <div className="flex items-center justify-center h-screen">
        {/* Use spinner instead of text - works for all languages */}
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600" />
      </div>
    }>
      <LanguageProvider>
        <App />
      </LanguageProvider>
    </Suspense>
  </StrictMode>,
)
