import { useEffect, useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2 } from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  CompactColorPicker,
  FormErrorBanner,
  Input,
} from '@uikit'
import { useUpdateColumn } from '@/portal-app/pm/queries'
import type { ProjectColumnDto } from '@/types/pm'
import { handleFormError } from '@/lib/form'

const createSchema = (t: (key: string) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')),
    color: z.string().min(1),
    wipLimit: z.preprocess(
      (val) => (val === '' || val === undefined || val === null) ? undefined : val,
      z.coerce.number().int().min(1).optional(),
    ),
  })

type FormData = {
  name: string
  color: string
  wipLimit: number | '' | undefined
}

interface ColumnSettingsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  projectId: string
  column: ProjectColumnDto | null
}

export const ColumnSettingsDialog = ({
  open,
  onOpenChange,
  projectId,
  column,
}: ColumnSettingsDialogProps) => {
  const { t } = useTranslation('common')
  const updateColumnMutation = useUpdateColumn()
  const [serverErrors, setServerErrors] = useState<string[]>([])

  const schema = useMemo(() => createSchema(t), [t])

  const form = useForm<FormData>({
    resolver: zodResolver(schema) as never,
    defaultValues: {
      name: '',
      color: '#6366f1',
      wipLimit: '',
    },
    mode: 'onBlur',
    reValidateMode: 'onChange',
  })

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = form

  useEffect(() => {
    if (column && open) {
      setServerErrors([])
      reset({
        name: column.name,
        color: column.color ?? '#6366f1',
        wipLimit: column.wipLimit ?? '',
      })
    }
  }, [column, open, reset])

  const onSubmit = (data: FormData) => {
    if (!column) return
    updateColumnMutation.mutate(
      {
        projectId,
        columnId: column.id,
        request: {
          name: data.name,
          color: data.color,
          wipLimit: data.wipLimit || undefined,
        },
      },
      {
        onSuccess: () => {
          onOpenChange(false)
        },
        onError: (err) => {
          handleFormError(err, form, setServerErrors, t)
        },
      },
    )
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent>
        <CredenzaHeader>
          <CredenzaTitle>{t('pm.columnSettings')}</CredenzaTitle>
          <CredenzaDescription>{t('pm.editColumn')}</CredenzaDescription>
        </CredenzaHeader>
        <form onSubmit={handleSubmit(onSubmit)}>
          <CredenzaBody className="space-y-4">
            <FormErrorBanner
              errors={serverErrors}
              onDismiss={() => setServerErrors([])}
              title={t('validation.unableToSave', 'Unable to save')}
            />
            <div>
              <label className="text-sm font-medium">{t('pm.columnName')}</label>
              <Input
                {...register('name')}
                placeholder={t('pm.columnName')}
                className="mt-1"
              />
              {errors.name && (
                <p className="text-sm text-destructive mt-1">{errors.name.message}</p>
              )}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="text-sm font-medium">{t('pm.color')}</label>
                <div className="mt-1">
                  <CompactColorPicker
                    value={watch('color') || '#6366f1'}
                    onChange={(color) => setValue('color', color, { shouldValidate: true })}
                  />
                </div>
              </div>
              <div>
                <label className="text-sm font-medium">{t('pm.wipLimit')}</label>
                <Input
                  type="number"
                  min={0}
                  {...register('wipLimit')}
                  placeholder={t('pm.wipLimit')}
                  className="mt-1"
                />
              </div>
            </div>
          </CredenzaBody>
          <CredenzaFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              className="cursor-pointer"
            >
              {t('buttons.cancel')}
            </Button>
            <Button
              type="submit"
              disabled={updateColumnMutation.isPending}
              className="cursor-pointer"
            >
              {updateColumnMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t('buttons.save')}
            </Button>
          </CredenzaFooter>
        </form>
      </CredenzaContent>
    </Credenza>
  )
}
