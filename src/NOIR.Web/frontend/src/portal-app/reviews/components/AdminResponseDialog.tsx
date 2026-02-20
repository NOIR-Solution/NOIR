import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Loader2, MessageSquare } from 'lucide-react'
import { toast } from 'sonner'
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Textarea,
} from '@uikit'
import { useReviewQuery, useAddAdminResponse } from '@/portal-app/reviews/queries'

interface AdminResponseDialogProps {
  reviewId: string | undefined
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export const AdminResponseDialog = ({
  reviewId,
  open,
  onOpenChange,
  onSuccess,
}: AdminResponseDialogProps) => {
  const { t } = useTranslation('common')
  const { data: review } = useReviewQuery(reviewId)
  const addResponseMutation = useAddAdminResponse()
  const [response, setResponse] = useState('')

  // Pre-fill with existing admin response when dialog opens
  useEffect(() => {
    if (open && review?.adminResponse) {
      setResponse(review.adminResponse)
    } else if (!open) {
      setResponse('')
    }
  }, [open, review?.adminResponse])

  const handleSubmit = async () => {
    if (!reviewId || !response.trim()) return
    try {
      await addResponseMutation.mutateAsync({ id: reviewId, response: response.trim() })
      toast.success(t('reviews.responseSuccess', 'Admin response added successfully'))
      onSuccess()
    } catch {
      toast.error(t('reviews.responseFailed', 'Failed to add admin response'))
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <MessageSquare className="h-5 w-5 text-primary" />
            </div>
            <div>
              <DialogTitle>
                {review?.adminResponse
                  ? t('reviews.editResponse', 'Edit Response')
                  : t('reviews.addResponse', 'Add Response')}
              </DialogTitle>
              <DialogDescription>
                {t(
                  'reviews.adminResponseDescription',
                  'Write a public response to this review. This will be visible to all customers.',
                )}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <div className="py-2">
          <Textarea
            placeholder={t('reviews.responsePlaceholder', 'Write your response...')}
            value={response}
            onChange={(e) => setResponse(e.target.value)}
            rows={5}
            className="resize-none"
          />
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            className="cursor-pointer"
            onClick={() => onOpenChange(false)}
          >
            {t('buttons.cancel', 'Cancel')}
          </Button>
          <Button
            className="cursor-pointer"
            onClick={handleSubmit}
            disabled={!response.trim() || addResponseMutation.isPending}
          >
            {addResponseMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {addResponseMutation.isPending
              ? t('reviews.submitting', 'Submitting...')
              : t('reviews.submitResponse', 'Submit Response')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
