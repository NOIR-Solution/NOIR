import { useState, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { Shield, User, Paintbrush } from 'lucide-react'
import { cn } from '@/lib/utils'
import { ChangePasswordForm } from '../../components/personal-settings/ChangePasswordForm'
import { ProfileForm } from '../../components/personal-settings/ProfileForm'
import { SessionManagement } from '../../components/personal-settings/SessionManagement'
import { AppearanceSettings } from '../../components/personal-settings/AppearanceSettings'
import { usePageContext } from '@/hooks/usePageContext'

type SettingsSection = 'profile' | 'security' | 'appearance'

interface NavItem {
  id: SettingsSection
  icon: typeof Shield
  labelKey: string
}

const navItems: NavItem[] = [
  { id: 'profile', icon: User, labelKey: 'profile.personalInfo' },
  { id: 'security', icon: Shield, labelKey: 'profile.security' },
  { id: 'appearance', icon: Paintbrush, labelKey: 'profile.appearance' },
]

export const PersonalSettingsPage = () => {
  const { t } = useTranslation('auth')
  const [activeSection, setActiveSection] = useState<SettingsSection>('profile')
  const [isSectionPending, startSectionTransition] = useTransition()

  // Set page context for audit logging (Activity Timeline)
  usePageContext('Profile')

  return (
    <div className="container max-w-6xl py-6">
      <div className="mb-8">
        <h1 className="text-2xl font-bold tracking-tight">{t('settings.title')}</h1>
        <p className="text-muted-foreground">{t('settings.description')}</p>
      </div>

      <div className="flex flex-col lg:flex-row gap-8">
        {/* Sidebar Navigation */}
        <aside className="lg:w-56 flex-shrink-0">
          <nav className="space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon
              const isActive = activeSection === item.id
              return (
                <button
                  type="button"
                  key={item.id}
                  onClick={() => startSectionTransition(() => setActiveSection(item.id))}
                  className={cn(
                    'w-full flex items-center gap-3 px-4 py-3 rounded-lg text-left transition-all cursor-pointer',
                    isActive
                      ? 'bg-blue-600/10 text-blue-600 font-medium shadow-sm'
                      : 'text-muted-foreground hover:bg-accent hover:text-foreground'
                  )}
                >
                  <Icon className="h-5 w-5 flex-shrink-0" />
                  <span>{t(item.labelKey)}</span>
                </button>
              )
            })}
          </nav>
        </aside>

        {/* Content Area */}
        <main className={isSectionPending ? 'flex-1 min-w-0 opacity-70 transition-opacity duration-200' : 'flex-1 min-w-0 transition-opacity duration-200'}>
          {activeSection === 'profile' && <ProfileForm />}
          {activeSection === 'security' && (
            <div className="space-y-6">
              <ChangePasswordForm />
              <SessionManagement />
            </div>
          )}
          {activeSection === 'appearance' && <AppearanceSettings />}
        </main>
      </div>
    </div>
  )
}

export default PersonalSettingsPage
