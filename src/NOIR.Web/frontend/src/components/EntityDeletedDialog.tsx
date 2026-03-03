import { useTranslation } from 'react-i18next'
import { Trash2 } from 'lucide-react'
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

interface EntityDeletedDialogProps {
  signal: EntityUpdateSignal | null
  onGoBack: () => void
}

export const EntityDeletedDialog = ({
  signal,
  onGoBack,
}: EntityDeletedDialogProps) => {
  const { t } = useTranslation('common')

  return (
    <Credenza open={!!signal} onOpenChange={() => { /* Non-dismissible */ }}>
      <CredenzaContent
        onInteractOutside={(e) => e.preventDefault()}
        onEscapeKeyDown={(e) => e.preventDefault()}
        className="[&>button:last-child]:hidden"
      >
        <CredenzaHeader>
          <div className="flex items-center gap-2">
            <Trash2 className="h-5 w-5 text-destructive" />
            <CredenzaTitle>{t('entityUpdate.deleted.title')}</CredenzaTitle>
          </div>
          <CredenzaDescription>
            {t('entityUpdate.deleted.description')}
          </CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody />
        <CredenzaFooter>
          <Button
            onClick={onGoBack}
            className="cursor-pointer"
          >
            {t('entityUpdate.deleted.goBack')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
