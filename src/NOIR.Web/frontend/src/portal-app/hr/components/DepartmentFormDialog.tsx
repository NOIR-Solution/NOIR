import { useEffect, useMemo, useState, useDeferredValue } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Building2, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import {
  Button,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormErrorBanner,
  FormMessage,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
} from '@uikit'
import {
  useCreateDepartment,
  useUpdateDepartment,
} from '@/portal-app/hr/queries'
import { useDepartmentQuery, useDepartmentsQuery, useEmployeeSearchQuery } from '@/portal-app/hr/queries'
import type { DepartmentDto, DepartmentTreeNodeDto } from '@/types/hr'
import { getRequiredFields, handleFormError } from '@/lib/form'

const createDepartmentSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    name: z.string().min(1, t('validation.required')).max(200, t('validation.maxLength', { count: 200 })),
    code: z.string().min(1, t('validation.required')).max(20, t('validation.maxLength', { count: 20 })),
    description: z.string().max(2000, t('validation.maxLength', { count: 2000 })).optional().nullable(),
    parentDepartmentId: z.string().optional().nullable(),
    managerId: z.string().optional().nullable(),
  })

type DepartmentFormData = z.infer<ReturnType<typeof createDepartmentSchema>>

interface DepartmentFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  department?: DepartmentDto | DepartmentTreeNodeDto | null
  parentDepartmentId?: string | null
  onSuccess?: () => void
}

export const DepartmentFormDialog = ({ open, onOpenChange, department, parentDepartmentId, onSuccess }: DepartmentFormDialogProps) => {
  const { t } = useTranslation('common')
  const isEditing = !!department
  const createMutation = useCreateDepartment()
  const updateMutation = useUpdateDepartment()

  const { data: departments } = useDepartmentsQuery()
  const [managerSearch, setManagerSearch] = useState('')
  const deferredManagerSearch = useDeferredValue(managerSearch)
  const { data: managerResults } = useEmployeeSearchQuery(deferredManagerSearch)

  const { data: fetchedDepartment } = useDepartmentQuery(isEditing ? department?.id : undefined)
  const fullDepartment = fetchedDepartment ?? null
  const [serverErrors, setServerErrors] = useState<string[]>([])

  const schema = useMemo(() => createDepartmentSchema(t), [t])
  const requiredFields = useMemo(() => getRequiredFields(schema), [schema])

  const form = useForm<DepartmentFormData>({
    resolver: zodResolver(schema) as unknown as Resolver<DepartmentFormData>,
    mode: 'onBlur',
    reValidateMode: 'onChange',
    defaultValues: {
      name: '',
      code: '',
      description: '',
      parentDepartmentId: parentDepartmentId || '',
      managerId: '',
    },
  })

  useEffect(() => {
    if (!open) return
    setServerErrors([])
    if (isEditing && fullDepartment) {
      form.reset({
        name: fullDepartment.name,
        code: fullDepartment.code,
        description: fullDepartment.description || '',
        parentDepartmentId: fullDepartment.parentDepartmentId || '',
        managerId: fullDepartment.managerId || '',
      })
      if (fullDepartment.managerName) {
        setManagerSearch(fullDepartment.managerName)
      }
    } else if (!isEditing) {
      form.reset({
        name: '',
        code: '',
        description: '',
        parentDepartmentId: parentDepartmentId || '',
        managerId: '',
      })
      setManagerSearch('')
    }
  }, [open, isEditing, fullDepartment, parentDepartmentId, form])

  const onSubmit = async (data: DepartmentFormData) => {
    try {
      if (isEditing && department) {
        await updateMutation.mutateAsync({
          id: department.id,
          request: {
            name: data.name,
            code: data.code,
            description: data.description || null,
            parentDepartmentId: data.parentDepartmentId || null,
            managerId: data.managerId || null,
          },
        })
        toast.success(t('hr.departmentUpdated', 'Department updated successfully'))
      } else {
        await createMutation.mutateAsync({
          name: data.name,
          code: data.code,
          description: data.description || null,
          parentDepartmentId: data.parentDepartmentId || null,
          managerId: data.managerId || null,
        })
        toast.success(t('hr.departmentCreated', 'Department created successfully'))
      }
      onOpenChange(false)
      onSuccess?.()
    } catch (err) {
      handleFormError(err, form, setServerErrors, t)
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  // Flatten department tree for parent dropdown, excluding current department if editing
  const flattenDepartments = (nodes: typeof departments, prefix = ''): { id: string; label: string }[] => {
    if (!nodes) return []
    return nodes.flatMap(node => {
      if (isEditing && department && node.id === department.id) return []
      return [
        { id: node.id, label: prefix + node.name },
        ...flattenDepartments(node.children, prefix + '  '),
      ]
    })
  }
  const flatDepartments = flattenDepartments(departments)

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[480px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
              <Building2 className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>{isEditing ? t('hr.editDepartment') : t('hr.createDepartment')}</CredenzaTitle>
              <CredenzaDescription>
                {isEditing
                  ? t('hr.editDepartmentDescription', 'Update department information')
                  : t('hr.createDepartmentDescription', 'Add a new department to your organization')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>
        <Form {...form} requiredFields={requiredFields}>
          <form onSubmit={form.handleSubmit(onSubmit)}>
            <CredenzaBody className="pb-2">
              <div className="space-y-4">
                <FormErrorBanner
                  errors={serverErrors}
                  onDismiss={() => setServerErrors([])}
                  title={t('validation.unableToSave', 'Unable to save')}
                />
                <FormField
                  control={form.control}
                  name="name"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.departmentName')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder={t('hr.departmentName')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="code"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.departmentCode')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder={t('hr.departmentCodePlaceholder', 'e.g. ENG, MKT')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="description"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.description')}</FormLabel>
                      <FormControl>
                        <Textarea {...field} value={field.value || ''} placeholder={t('hr.description')} rows={3} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="parentDepartmentId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.parentDepartment')}</FormLabel>
                      <Select
                        onValueChange={(val) => field.onChange(val === '__none__' ? '' : val)}
                        value={field.value || '__none__'}
                      >
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue placeholder={t('hr.noParent', 'No parent (root department)')} />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          <SelectItem value="__none__" className="cursor-pointer">{t('hr.noParent', 'No parent (root department)')}</SelectItem>
                          {flatDepartments.map((dept) => (
                            <SelectItem key={dept.id} value={dept.id} className="cursor-pointer">
                              {dept.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="managerId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.manager')}</FormLabel>
                      <div className="space-y-2">
                        <Input
                          placeholder={t('hr.searchManager', 'Search for manager...')}
                          value={managerSearch}
                          onChange={(e) => setManagerSearch(e.target.value)}
                          className="h-9"
                        />
                        {managerResults && managerResults.length > 0 && managerSearch.length >= 2 && (
                          <div className="border rounded-md max-h-32 overflow-y-auto">
                            {managerResults.map((emp) => (
                              <button
                                key={emp.id}
                                type="button"
                                className="w-full text-left px-3 py-2 text-sm hover:bg-muted/50 cursor-pointer"
                                onClick={() => {
                                  field.onChange(emp.id)
                                  setManagerSearch(emp.fullName)
                                }}
                              >
                                <span className="font-medium">{emp.fullName}</span>
                                {emp.position && <span className="text-muted-foreground"> - {emp.position}</span>}
                              </button>
                            ))}
                          </div>
                        )}
                        {field.value && (
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            className="cursor-pointer text-xs"
                            onClick={() => {
                              field.onChange('')
                              setManagerSearch('')
                            }}
                          >
                            {t('hr.clearManager', 'Clear manager')}
                          </Button>
                        )}
                      </div>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
            </CredenzaBody>
            <CredenzaFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={isPending}
                className="cursor-pointer"
              >
                {t('labels.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isPending} className="cursor-pointer">
                {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isEditing ? t('labels.save', 'Save') : t('labels.create', 'Create')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
