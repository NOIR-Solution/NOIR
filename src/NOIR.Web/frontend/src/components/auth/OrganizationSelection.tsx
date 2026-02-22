import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { motion } from 'framer-motion'
import { Building2, ArrowLeft, Check, Loader2 } from 'lucide-react'
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle } from '@uikit'

import { cn } from '@/lib/utils'
import type { TenantOption } from '@/types'

interface OrganizationSelectionProps {
  organizations: TenantOption[]
  onSelect: (tenantId: string | null) => void
  onBack: () => void
  userEmail: string
  isLoading?: boolean
  error?: string | null
}

export const OrganizationSelection = ({
  organizations,
  onSelect,
  onBack,
  userEmail: _userEmail,
  isLoading = false,
  error = null,
}: OrganizationSelectionProps) => {
  const { t } = useTranslation('auth')
  const [selectedOrg, setSelectedOrg] = useState<TenantOption | null>(null)

  const handleSelect = (org: TenantOption) => {
    if (!isLoading) {
      setSelectedOrg(org)
    }
  }

  const handleContinue = () => {
    if (selectedOrg && !isLoading) {
      onSelect(selectedOrg.tenantId)
    }
  }

  return (
    <div className="w-full max-w-md space-y-8 animate-fade-in">
      {/* Back Button */}
      <Button
        variant="ghost"
        size="sm"
        onClick={onBack}
        disabled={isLoading}
        className="text-muted-foreground hover:text-foreground"
      >
        <ArrowLeft className="mr-2 h-4 w-4" />
        {t('forgotPassword.backToLogin')}
      </Button>

      {/* Header */}
      <div className="text-center space-y-2">
        <motion.div
          initial={{ scale: 0 }}
          animate={{ scale: 1 }}
          transition={{
            type: 'spring',
            stiffness: 260,
            damping: 20,
          }}
          className="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-gradient-to-br from-blue-700 to-cyan-700 shadow-xl mb-4"
        >
          <Building2 className="h-8 w-8 text-white" />
        </motion.div>

        <h1 className="text-2xl font-bold tracking-tight text-foreground">
          {t('login.selectOrganization')}
        </h1>
        <p className="text-sm text-muted-foreground">
          {t('login.multiTenantMessage')}
        </p>
      </div>

      {/* Organization Selection Card */}
      <Card className="backdrop-blur-xl bg-background/80 border-border/50 shadow-2xl">
        <CardHeader>
          <CardTitle className="text-lg">{t('login.organizationLabel')}</CardTitle>
          <CardDescription>
            {t('login.selectOrganizationPlaceholder')}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Error Message */}
          {error && (
            <motion.div
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              className="p-3 rounded-lg bg-destructive/10 border border-destructive/20"
            >
              <p className="text-sm text-destructive font-medium">{error}</p>
            </motion.div>
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
                    'w-full text-left p-4 rounded-lg border-2 transition-all',
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
                      <motion.div
                        initial={{ scale: 0 }}
                        animate={{ scale: 1 }}
                        transition={{ type: 'spring', stiffness: 500, damping: 30 }}
                        className="flex-shrink-0"
                      >
                        <div className="w-5 h-5 rounded-full bg-primary flex items-center justify-center">
                          <Check className="h-3 w-3 text-primary-foreground" />
                        </div>
                      </motion.div>
                    )}
                  </div>
                </button>
              )
            })}
          </div>

          {/* Continue Button */}
          <Button
            onClick={handleContinue}
            disabled={!selectedOrg || isLoading}
            className="w-full h-12 text-base font-semibold rounded-xl bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white shadow-lg hover:shadow-xl transition-all duration-200"
          >
            {isLoading ? (
              <span className="flex items-center gap-2">
                <Loader2 className="h-5 w-5 animate-spin" />
                {t('login.submitting')}
              </span>
            ) : (
              t('login.continue')
            )}
          </Button>
        </CardContent>
      </Card>

      {/* Footer */}
      <div className="text-center">
        <p className="text-xs text-muted-foreground">
          {t('login.selectOrganization')}
        </p>
      </div>
    </div>
  )
}
