import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { XCircle } from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Textarea,
} from '@uikit'

interface RejectReviewDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (reason?: string) => void
  isBulk?: boolean
  count?: number
}

export const RejectReviewDialog = ({
  open,
  onOpenChange,
  onConfirm,
  isBulk = false,
  count,
}: RejectReviewDialogProps) => {
  const { t } = useTranslation('common')
  const [reason, setReason] = useState('')

  const handleConfirm = () => {
    onConfirm(reason || undefined)
    setReason('')
  }

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      setReason('')
    }
    onOpenChange(isOpen)
  }

  return (
    <AlertDialog open={open} onOpenChange={handleOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <XCircle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <AlertDialogTitle>
                {isBulk
                  ? t('reviews.bulkRejectTitle', {
                      count,
                      defaultValue: `Reject ${count} reviews`,
                    })
                  : t('reviews.rejectTitle', 'Reject Review')}
              </AlertDialogTitle>
              <AlertDialogDescription>
                {isBulk
                  ? t('reviews.bulkRejectDescription', {
                      count,
                      defaultValue: `Are you sure you want to reject ${count} selected reviews? This will hide them from public view.`,
                    })
                  : t(
                      'reviews.rejectDescription',
                      'Are you sure you want to reject this review? This will hide it from public view.',
                    )}
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>

        <div className="py-2">
          <Textarea
            placeholder={t('reviews.rejectReasonPlaceholder', 'Reason for rejection (optional)...')}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={3}
            className="resize-none"
          />
        </div>

        <AlertDialogFooter>
          <AlertDialogCancel className="cursor-pointer">
            {t('buttons.cancel', 'Cancel')}
          </AlertDialogCancel>
          <AlertDialogAction
            className="cursor-pointer bg-destructive text-destructive-foreground hover:bg-destructive/90 border-destructive/30"
            onClick={handleConfirm}
          >
            {t('reviews.reject', 'Reject')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
