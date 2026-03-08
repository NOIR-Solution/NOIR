import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Building2, Loader2 } from 'lucide-react'
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
import { toast } from 'sonner'
import { useDeleteDepartment } from '@/portal-app/hr/queries'

interface DepartmentToDelete {
  id: string
  name: string
  code: string
  employeeCount: number
  childCount: number
}

interface DeleteDepartmentDialogProps {
  department: DepartmentToDelete | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export const DeleteDepartmentDialog = ({ department, open, onOpenChange }: DeleteDepartmentDialogProps) => {
  const { t } = useTranslation('common')
  const [isDeleting, setIsDeleting] = useState(false)
  const deleteMutation = useDeleteDepartment()

  const canDelete = department
    ? department.employeeCount === 0 && department.childCount === 0
    : false

  const handleConfirm = async () => {
    if (!department || !canDelete) return
    setIsDeleting(true)
    try {
      await deleteMutation.mutateAsync(department.id)
      toast.success(t('hr.departmentDeleted', 'Department deleted'))
      onOpenChange(false)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : t('errors.generic', 'An error occurred'))
    } finally {
      setIsDeleting(false)
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <CredenzaTitle>{t('hr.deleteDepartmentTitle', 'Delete Department')}</CredenzaTitle>
              <CredenzaDescription>{t('hr.deleteConfirmation')}</CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <CredenzaBody>
          {department && (
            <div className="my-4 p-4 bg-muted rounded-lg">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center">
                  <Building2 className="h-5 w-5 text-primary" />
                </div>
                <div>
                  <p className="font-medium">{department.name}</p>
                  <p className="text-sm text-muted-foreground font-mono">{department.code}</p>
                </div>
              </div>
              {!canDelete && (
                <div className="mt-3 p-2 bg-destructive/10 rounded text-sm text-destructive">
                  {department.employeeCount > 0 && (
                    <p>{t('hr.departmentHasEmployees', { count: department.employeeCount, defaultValue: `Has ${department.employeeCount} employee(s)` })}</p>
                  )}
                  {department.childCount > 0 && (
                    <p>{t('hr.departmentHasChildren', { count: department.childCount, defaultValue: `Has ${department.childCount} sub-department(s)` })}</p>
                  )}
                  <p className="mt-1 font-medium">{t('hr.reassignFirst', 'Reassign or remove them first')}</p>
                </div>
              )}
            </div>
          )}
        </CredenzaBody>

        <CredenzaFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isDeleting}
            className="cursor-pointer"
          >
            {t('labels.cancel', 'Cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirm}
            disabled={isDeleting || !canDelete}
            className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"
          >
            {isDeleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isDeleting ? t('labels.deleting', 'Deleting...') : t('labels.delete', 'Delete')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
