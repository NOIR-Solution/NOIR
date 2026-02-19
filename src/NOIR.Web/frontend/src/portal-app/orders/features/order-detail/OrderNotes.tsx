import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { MessageSquare, Send, Trash2 } from 'lucide-react'
import { toast } from 'sonner'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Skeleton,
  Textarea,
} from '@uikit'
import {
  useOrderNotesQuery,
  useAddOrderNoteMutation,
  useDeleteOrderNoteMutation,
} from '@/portal-app/orders/queries'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'

interface OrderNotesProps {
  orderId: string
  canWrite: boolean
}

export const OrderNotes = ({ orderId, canWrite }: OrderNotesProps) => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()

  const [noteContent, setNoteContent] = useState('')
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const [noteToDelete, setNoteToDelete] = useState<string | null>(null)

  const { data: notes = [], isLoading } = useOrderNotesQuery(orderId)
  const addMutation = useAddOrderNoteMutation()
  const deleteMutation = useDeleteOrderNoteMutation()

  const handleAdd = async () => {
    if (!noteContent.trim()) return
    try {
      await addMutation.mutateAsync({ orderId, content: noteContent.trim() })
      setNoteContent('')
      toast.success(t('orders.notes.addSuccess'))
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('orders.notes.addError'))
    }
  }

  const handleDelete = async () => {
    if (!noteToDelete) return
    try {
      await deleteMutation.mutateAsync({ orderId, noteId: noteToDelete })
      toast.success(t('orders.notes.deleteSuccess'))
      setShowDeleteDialog(false)
      setNoteToDelete(null)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('orders.notes.deleteError'))
    }
  }

  return (
    <>
      <Card className="shadow-sm">
        <CardHeader className="pb-3">
          <CardTitle className="text-sm flex items-center gap-2">
            <MessageSquare className="h-4 w-4" />
            {t('orders.notes.title')}
          </CardTitle>
          <CardDescription>{t('orders.notes.description')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {canWrite && (
            <div className="flex gap-2">
              <Textarea
                value={noteContent}
                onChange={(e) => setNoteContent(e.target.value)}
                placeholder={t('orders.notes.placeholder')}
                aria-label={t('orders.notes.placeholder')}
                rows={2}
                className="resize-none"
                maxLength={2000}
              />
              <Button
                size="icon"
                className="cursor-pointer shrink-0 self-end"
                onClick={handleAdd}
                disabled={!noteContent.trim() || addMutation.isPending}
                aria-label={t('orders.notes.addNote')}
              >
                <Send className="h-4 w-4" />
              </Button>
            </div>
          )}

          {isLoading ? (
            <div className="space-y-3">
              <Skeleton className="h-16 w-full rounded-lg" />
              <Skeleton className="h-16 w-full rounded-lg" />
            </div>
          ) : notes.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-4">
              {t('orders.notes.noNotes')}
            </p>
          ) : (
            <div className="space-y-3">
              {notes.map((note) => (
                <div key={note.id} className="rounded-lg border p-3 space-y-1.5">
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <span className="font-medium text-foreground">{note.createdByUserName}</span>
                      <span>&middot;</span>
                      <span>{formatDateTime(note.createdAt)}</span>
                    </div>
                    {canWrite && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="cursor-pointer h-6 w-6 text-muted-foreground hover:text-destructive"
                        onClick={() => { setNoteToDelete(note.id); setShowDeleteDialog(true) }}
                        aria-label={t('orders.notes.deleteNote')}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    )}
                  </div>
                  <p className="text-sm whitespace-pre-wrap">{note.content}</p>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <AlertDialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <div className="flex items-center gap-3">
              <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
                <Trash2 className="h-5 w-5 text-destructive" />
              </div>
              <div>
                <AlertDialogTitle>{t('orders.notes.deleteTitle')}</AlertDialogTitle>
                <AlertDialogDescription>{t('orders.notes.deleteDescription')}</AlertDialogDescription>
              </div>
            </div>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel className="cursor-pointer">{t('labels.cancel')}</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
            >
              <Trash2 className="h-4 w-4 mr-2" />
              {t('orders.notes.confirmDelete')}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
