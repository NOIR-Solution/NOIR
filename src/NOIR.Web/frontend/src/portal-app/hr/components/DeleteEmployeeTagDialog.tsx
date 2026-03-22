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
import type { EmployeeTagDto } from '@/types/hr'

interface DeleteEmployeeTagDialogProps {
  tag: EmployeeTagDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
  isPending: boolean
}

export const DeleteEmployeeTagDialog = ({ tag, open, onOpenChange, onConfirm, isPending }: DeleteEmployeeTagDialogProps) => {
  const { t } = useTranslation('common')

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <Trash2 className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <CredenzaTitle>{t('hr.tags.deleteTag')}</CredenzaTitle>
              <CredenzaDescription>
                {t('hr.tags.deleteConfirmation')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>
        <CredenzaBody>
          {tag && (
            <div className="p-4 bg-muted rounded-lg">
              <div className="flex items-center gap-3">
                <span
                  className="h-4 w-4 rounded-full flex-shrink-0"
                  style={{ backgroundColor: tag.color }}
                />
                <div>
                  <p className="font-medium">{tag.name}</p>
                  <p className="text-sm text-muted-foreground">
                    {t(`hr.tags.categories.${tag.category}`)}
                  </p>
                </div>
              </div>
              {tag.employeeCount > 0 && (
                <div className="mt-3 p-2 bg-yellow-100 dark:bg-yellow-900/20 rounded text-sm text-yellow-800 dark:text-yellow-200">
                  {t('hr.tags.tagUsedByEmployees', { count: tag.employeeCount })}
                </div>
              )}
            </div>
          )}
        </CredenzaBody>
        <CredenzaFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isPending}
            className="cursor-pointer"
          >
            {t('labels.cancel', 'Cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={onConfirm}
            disabled={isPending}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            {t('hr.tags.deleteTag')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
