import { useTranslation } from 'react-i18next'
import { CheckCircle2, Lock, AlertTriangle } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import type { ConfigurationSection } from '@/services/configuration'

interface SectionBrowserProps {
  sections: ConfigurationSection[]
  selectedSection: ConfigurationSection | null
  onSectionSelect: (section: ConfigurationSection) => void
  isLoading: boolean
}

export function SectionBrowser({
  sections,
  selectedSection,
  onSectionSelect,
  isLoading,
}: SectionBrowserProps) {
  const { t } = useTranslation()

  // Group sections by allowed/forbidden
  const allowedSections = sections.filter(s => s.isAllowed)
  const forbiddenSections = sections.filter(s => !s.isAllowed)

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-sm">{t('platformSettings.sections')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-2">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-12 w-full" />
          ))}
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="h-full flex flex-col">
      <CardHeader>
        <CardTitle className="text-sm">{t('platformSettings.sections')}</CardTitle>
      </CardHeader>
      <CardContent className="flex-1 overflow-hidden p-0">
        <ScrollArea className="h-full">
          <div className="p-4 space-y-4">
            {/* Allowed Sections */}
            {allowedSections.length > 0 && (
              <div>
                <div className="flex items-center gap-2 mb-2 px-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500" />
                  <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                    {t('platformSettings.allowedSections')}
                  </span>
                  <Badge variant="secondary" className="ml-auto">
                    {allowedSections.length}
                  </Badge>
                </div>
                <div className="space-y-1">
                  {allowedSections.map((section) => (
                    <Button
                      key={section.name}
                      variant={selectedSection?.name === section.name ? 'default' : 'ghost'}
                      className={cn(
                        'w-full justify-start h-auto py-3 px-3',
                        selectedSection?.name === section.name && 'bg-primary text-primary-foreground'
                      )}
                      onClick={() => onSectionSelect(section)}
                    >
                      <div className="flex items-start gap-3 w-full">
                        <CheckCircle2 className="h-4 w-4 mt-0.5 flex-shrink-0 text-green-500" />
                        <div className="flex-1 text-left min-w-0">
                          <div className="font-medium text-sm truncate">{section.displayName}</div>
                          <div className="text-xs text-muted-foreground truncate">{section.name}</div>
                          {section.requiresRestart && (
                            <Badge variant="secondary" className="mt-1 text-xs">
                              <AlertTriangle className="h-3 w-3 mr-1" />
                              Restart
                            </Badge>
                          )}
                        </div>
                      </div>
                    </Button>
                  ))}
                </div>
              </div>
            )}

            {/* Forbidden Sections */}
            {forbiddenSections.length > 0 && (
              <div>
                <div className="flex items-center gap-2 mb-2 px-2">
                  <Lock className="h-4 w-4 text-muted-foreground" />
                  <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                    {t('platformSettings.restrictedSections')}
                  </span>
                  <Badge variant="outline" className="ml-auto">
                    {forbiddenSections.length}
                  </Badge>
                </div>
                <div className="space-y-1">
                  {forbiddenSections.map((section) => (
                    <Button
                      key={section.name}
                      variant="ghost"
                      className="w-full justify-start h-auto py-3 px-3 opacity-50 cursor-not-allowed"
                      disabled
                    >
                      <div className="flex items-start gap-3 w-full">
                        <Lock className="h-4 w-4 mt-0.5 flex-shrink-0" />
                        <div className="flex-1 text-left min-w-0">
                          <div className="font-medium text-sm truncate">{section.displayName}</div>
                          <div className="text-xs text-muted-foreground truncate">{section.name}</div>
                        </div>
                      </div>
                    </Button>
                  ))}
                </div>
              </div>
            )}
          </div>
        </ScrollArea>
      </CardContent>
    </Card>
  )
}
