import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  createEmployee,
  updateEmployee,
  deactivateEmployee,
  reactivateEmployee,
  createDepartment,
  updateDepartment,
  deleteDepartment,
  reorderDepartments,
  createTag,
  updateTag,
  deleteTag,
  assignTagsToEmployee,
  removeTagsFromEmployee,
  bulkAssignTags,
  bulkChangeDepartment,
  importEmployees,
} from '@/services/hr'
import type {
  CreateEmployeeRequest,
  UpdateEmployeeRequest,
  EmployeeStatus,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
  ReorderDepartmentsRequest,
  CreateTagRequest,
  UpdateTagRequest,
  AssignTagsRequest,
  RemoveTagsRequest,
  BulkAssignTagsRequest,
  BulkChangeDepartmentRequest,
} from '@/types/hr'
import { employeeKeys, departmentKeys, tagKeys } from './queryKeys'

export const useCreateEmployee = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateEmployeeRequest) => createEmployee(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
    },
  })
}

export const useUpdateEmployee = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateEmployeeRequest }) => updateEmployee(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
    },
  })
}

export const useDeactivateEmployee = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: EmployeeStatus }) => deactivateEmployee(id, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
    },
  })
}

export const useReactivateEmployee = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => reactivateEmployee(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
    },
  })
}

export const useCreateDepartment = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateDepartmentRequest) => createDepartment(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: departmentKeys.all })
    },
  })
}

export const useUpdateDepartment = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateDepartmentRequest }) => updateDepartment(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: departmentKeys.all })
    },
  })
}

export const useDeleteDepartment = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteDepartment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: departmentKeys.all })
    },
  })
}

export const useReorderDepartments = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: ReorderDepartmentsRequest) => reorderDepartments(request),
    onError: () => {
      queryClient.invalidateQueries({ queryKey: departmentKeys.all })
    },
  })
}

// ─── Tag mutations ────────────────────────────────────────────────────────

export const useCreateTag = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateTagRequest) => createTag(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tagKeys.all })
    },
  })
}

export const useUpdateTag = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateTagRequest }) => updateTag(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tagKeys.all })
    },
  })
}

export const useDeleteTag = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteTag(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tagKeys.all })
    },
  })
}

export const useAssignTags = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ employeeId, data }: { employeeId: string; data: AssignTagsRequest }) =>
      assignTagsToEmployee(employeeId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
      queryClient.invalidateQueries({ queryKey: tagKeys.all })
    },
  })
}

export const useRemoveTags = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ employeeId, data }: { employeeId: string; data: RemoveTagsRequest }) =>
      removeTagsFromEmployee(employeeId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
      queryClient.invalidateQueries({ queryKey: tagKeys.all })
    },
  })
}

// ─── Bulk mutations ──────────────────────────────────────────────────────

export const useBulkAssignTags = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: BulkAssignTagsRequest) => bulkAssignTags(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
      queryClient.invalidateQueries({ queryKey: tagKeys.all })
    },
  })
}

export const useBulkChangeDepartment = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: BulkChangeDepartmentRequest) => bulkChangeDepartment(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
      queryClient.invalidateQueries({ queryKey: departmentKeys.all })
    },
  })
}

export const useImportEmployees = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (file: File) => importEmployees(file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: employeeKeys.all })
    },
  })
}
