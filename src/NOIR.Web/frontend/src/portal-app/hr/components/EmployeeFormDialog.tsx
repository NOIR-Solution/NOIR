import { useEffect, useMemo, useState, useDeferredValue } from 'react'
import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, UserPlus, UserCog } from 'lucide-react'
import { toast } from 'sonner'
import {
  Button,
  Checkbox,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  DatePicker,
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
  useCreateEmployee,
  useUpdateEmployee,
} from '@/portal-app/hr/queries'
import { useDepartmentsQuery, useEmployeeSearchQuery } from '@/portal-app/hr/queries'
import type { EmployeeDto, EmployeeListDto, EmploymentType } from '@/types/hr'
import { getRequiredFields, handleFormError } from '@/lib/form'

const EMPLOYMENT_TYPES: EmploymentType[] = ['FullTime', 'PartTime', 'Contract', 'Intern']

const createEmployeeSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    firstName: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 })),
    lastName: z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 })),
    email: z.string().min(1, t('validation.required')).email(t('validation.invalidEmail')),
    phone: z.string().max(20, t('validation.maxLength', { count: 20 })).optional().nullable(),
    departmentId: z.string().min(1, t('validation.required')),
    position: z.string().max(200, t('validation.maxLength', { count: 200 })).optional().nullable(),
    managerId: z.string().optional().nullable(),
    joinDate: z.string().min(1, t('validation.required')),
    employmentType: z.string().min(1, t('validation.required')),
    notes: z.string().max(2000, t('validation.maxLength', { count: 2000 })).optional().nullable(),
    createUserAccount: z.boolean().optional(),
  })

type EmployeeFormData = z.infer<ReturnType<typeof createEmployeeSchema>>

interface EmployeeFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  employee?: EmployeeDto | EmployeeListDto | null
  onSuccess?: () => void
}

export const EmployeeFormDialog = ({ open, onOpenChange, employee, onSuccess }: EmployeeFormDialogProps) => {
  const { t } = useTranslation('common')
  const { formatDate } = useRegionalSettings()
  const isEditing = !!employee
  const createMutation = useCreateEmployee()
  const updateMutation = useUpdateEmployee()

  const { data: departments } = useDepartmentsQuery()
  const [managerSearch, setManagerSearch] = useState('')
  const deferredManagerSearch = useDeferredValue(managerSearch)
  const { data: managerResults } = useEmployeeSearchQuery(deferredManagerSearch)

  const fullEmployee = employee && 'departmentId' in employee ? employee as EmployeeDto : null
  const [serverErrors, setServerErrors] = useState<string[]>([])

  const schema = useMemo(() => createEmployeeSchema(t), [t])
  const requiredFields = useMemo(() => getRequiredFields(schema), [schema])

  const form = useForm<EmployeeFormData>({
    resolver: zodResolver(schema) as unknown as Resolver<EmployeeFormData>,
    mode: 'onBlur',
    reValidateMode: 'onChange',
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      departmentId: '',
      position: '',
      managerId: '',
      joinDate: new Date().toISOString().split('T')[0],
      employmentType: 'FullTime',
      notes: '',
      createUserAccount: false,
    },
  })

  useEffect(() => {
    if (open) {
      setServerErrors([])
      if (fullEmployee) {
        form.reset({
          firstName: fullEmployee.firstName,
          lastName: fullEmployee.lastName,
          email: fullEmployee.email,
          phone: fullEmployee.phone || '',
          departmentId: fullEmployee.departmentId,
          position: fullEmployee.position || '',
          managerId: fullEmployee.managerId || '',
          joinDate: fullEmployee.joinDate?.split('T')[0] || '',
          employmentType: fullEmployee.employmentType,
          notes: fullEmployee.notes || '',
          createUserAccount: false,
        })
      } else {
        form.reset({
          firstName: '',
          lastName: '',
          email: '',
          phone: '',
          departmentId: '',
          position: '',
          managerId: '',
          joinDate: new Date().toISOString().split('T')[0],
          employmentType: 'FullTime',
          notes: '',
          createUserAccount: false,
        })
      }
    }
  }, [open, fullEmployee, form])

  const onSubmit = async (data: EmployeeFormData) => {
    try {
      if (isEditing && fullEmployee) {
        await updateMutation.mutateAsync({
          id: fullEmployee.id,
          request: {
            firstName: data.firstName,
            lastName: data.lastName,
            email: data.email,
            phone: data.phone || null,
            departmentId: data.departmentId,
            position: data.position || null,
            managerId: data.managerId || null,
            employmentType: data.employmentType as EmploymentType,
            notes: data.notes || null,
          },
        })
        toast.success(t('hr.employeeUpdated', 'Employee updated successfully'))
      } else {
        await createMutation.mutateAsync({
          firstName: data.firstName,
          lastName: data.lastName,
          email: data.email,
          phone: data.phone || null,
          departmentId: data.departmentId,
          position: data.position || null,
          managerId: data.managerId || null,
          joinDate: data.joinDate,
          employmentType: data.employmentType as EmploymentType,
          notes: data.notes || null,
          createUserAccount: data.createUserAccount,
        })
        toast.success(t('hr.employeeCreated', 'Employee created successfully'))
      }
      onOpenChange(false)
      onSuccess?.()
    } catch (err) {
      handleFormError(err, form, setServerErrors, t)
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  // Flatten department tree for dropdown
  const flattenDepartments = (nodes: typeof departments, prefix = ''): { id: string; label: string }[] => {
    if (!nodes) return []
    return nodes.flatMap(node => [
      { id: node.id, label: prefix + node.name },
      ...flattenDepartments(node.children, prefix + '  '),
    ])
  }
  const flatDepartments = flattenDepartments(departments)

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[540px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-primary/10 border border-primary/20">
              {isEditing ? <UserCog className="h-5 w-5 text-primary" /> : <UserPlus className="h-5 w-5 text-primary" />}
            </div>
            <div>
              <CredenzaTitle>{isEditing ? t('hr.editEmployee') : t('hr.createEmployee')}</CredenzaTitle>
              <CredenzaDescription>
                {isEditing
                  ? t('hr.editEmployeeDescription', 'Update employee information')
                  : t('hr.createEmployeeDescription', 'Add a new employee to your organization')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>
        <Form {...form} requiredFields={requiredFields}>
          <form onSubmit={form.handleSubmit(onSubmit)}>
            <CredenzaBody>
              <div className="space-y-4">
                <FormErrorBanner
                  errors={serverErrors}
                  onDismiss={() => setServerErrors([])}
                  title={t('validation.unableToSave', 'Unable to save')}
                />
                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="firstName"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('hr.firstName')}</FormLabel>
                        <FormControl>
                          <Input {...field} placeholder={t('hr.firstName')} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="lastName"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('hr.lastName')}</FormLabel>
                        <FormControl>
                          <Input {...field} placeholder={t('hr.lastName')} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <FormField
                  control={form.control}
                  name="email"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.email')}</FormLabel>
                      <FormControl>
                        <Input {...field} type="email" placeholder={t('hr.email')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="phone"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.phone')}</FormLabel>
                      <FormControl>
                        <Input {...field} value={field.value || ''} placeholder={t('hr.phone')} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="departmentId"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.department')}</FormLabel>
                      <Select onValueChange={field.onChange} value={field.value}>
                        <FormControl>
                          <SelectTrigger className="cursor-pointer">
                            <SelectValue placeholder={t('hr.selectDepartment', 'Select department')} />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
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
                  name="position"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.position')}</FormLabel>
                      <FormControl>
                        <Input {...field} value={field.value || ''} placeholder={t('hr.position')} />
                      </FormControl>
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

                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="joinDate"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('hr.joinDate')}</FormLabel>
                        <FormControl>
                          <DatePicker
                            value={field.value ? new Date(field.value) : undefined}
                            onChange={(date) => field.onChange(date ? `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}` : '')}
                            placeholder={t('hr.joinDate')}
                            disabled={isEditing}
                            formatDate={formatDate}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="employmentType"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>{t('hr.employmentType')}</FormLabel>
                        <Select onValueChange={field.onChange} value={field.value}>
                          <FormControl>
                            <SelectTrigger className="cursor-pointer">
                              <SelectValue />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {EMPLOYMENT_TYPES.map((type) => (
                              <SelectItem key={type} value={type} className="cursor-pointer">
                                {t(`hr.employmentTypes.${type.charAt(0).toLowerCase() + type.slice(1).replace(/([A-Z])/g, (m) => m.toLowerCase())}`, type)}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <FormField
                  control={form.control}
                  name="notes"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('hr.notes')}</FormLabel>
                      <FormControl>
                        <Textarea {...field} value={field.value || ''} placeholder={t('hr.notes')} rows={3} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                {!isEditing && (
                  <FormField
                    control={form.control}
                    name="createUserAccount"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center space-x-3 space-y-0">
                        <FormControl>
                          <Checkbox
                            checked={field.value}
                            onCheckedChange={field.onChange}
                            className="cursor-pointer"
                          />
                        </FormControl>
                        <FormLabel className="text-sm font-normal cursor-pointer">
                          {t('hr.createUserAccount')}
                        </FormLabel>
                      </FormItem>
                    )}
                  />
                )}
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
