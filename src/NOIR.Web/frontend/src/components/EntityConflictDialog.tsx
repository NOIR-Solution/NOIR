import { useTranslation } from 'react-i18next'
import { AlertCircle } from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
} from '@uikit'
import type { EntityUpdateSignal } from '@/types/signals'

interface EntityConflictDialogProps {
  signal: EntityUpdateSignal | null
  onContinueEditing: () => void
  onReloadAndRestart: () => void
}

export const EntityConflictDialog = ({
  signal,
  onContinueEditing,
  onReloadAndRestart,
}: EntityConflictDialogProps) => {
  const { t } = useTranslation('common')

  return (
    <Credenza open={!!signal} onOpenChange={(open) => { if (!open) onContinueEditing() }}>
      <CredenzaContent>
        <CredenzaHeader>
          <div className="flex items-center gap-2">
            <AlertCircle className="h-5 w-5 text-amber-500" />
            <CredenzaTitle>{t('entityUpdate.conflict.title')}</CredenzaTitle>
          </div>
          <CredenzaDescription>
            {t('entityUpdate.conflict.description')}
          </CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody />
        <CredenzaFooter>
          <Button
            variant="outline"
            onClick={onContinueEditing}
            className="cursor-pointer"
          >
            {t('entityUpdate.conflict.continueEditing')}
          </Button>
          <Button
            onClick={onReloadAndRestart}
            className="cursor-pointer"
          >
            {t('entityUpdate.conflict.reloadAndRestart')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
