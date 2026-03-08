/**
 * HR API Service
 *
 * Provides methods for managing employees and departments.
 */
import { apiClient } from './apiClient'
import type {
  EmployeeDto,
  EmployeePagedResult,
  EmployeeSearchDto,
  CreateEmployeeRequest,
  UpdateEmployeeRequest,
  EmployeeStatus,
  EmploymentType,
  DepartmentDto,
  DepartmentTreeNodeDto,
  CreateDepartmentRequest,
  UpdateDepartmentRequest,
  ReorderDepartmentsRequest,
  GetEmployeesParams,
  EmployeeTagDto,
  EmployeeTagCategory,
  CreateTagRequest,
  UpdateTagRequest,
  AssignTagsRequest,
  RemoveTagsRequest,
  TagBriefDto,
  OrgChartNodeDto,
  HrReportsDto,
  ImportResultDto,
  BulkAssignTagsRequest,
  BulkChangeDepartmentRequest,
} from '@/types/hr'

// ─── Employee endpoints ────────────────────────────────────────────────────

export const getEmployees = async (params: GetEmployeesParams = {}): Promise<EmployeePagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.search) queryParams.append('search', params.search)
  if (params.departmentId) queryParams.append('departmentId', params.departmentId)
  if (params.status) queryParams.append('status', params.status)
  if (params.employmentType) queryParams.append('employmentType', params.employmentType)
  if (params.orderBy) queryParams.append('orderBy', params.orderBy)
  if (params.isDescending != null) queryParams.append('isDescending', params.isDescending.toString())

  const query = queryParams.toString()
  return apiClient<EmployeePagedResult>(`/hr/employees${query ? `?${query}` : ''}`)
}

export const getEmployeeById = async (id: string): Promise<EmployeeDto> => {
  return apiClient<EmployeeDto>(`/hr/employees/${id}`)
}

export const searchEmployees = async (query: string): Promise<EmployeeSearchDto[]> => {
  const queryParams = new URLSearchParams()
  queryParams.append('query', query)
  return apiClient<EmployeeSearchDto[]>(`/hr/employees/search?${queryParams}`)
}

export const createEmployee = async (request: CreateEmployeeRequest): Promise<EmployeeDto> => {
  return apiClient<EmployeeDto>('/hr/employees', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateEmployee = async (id: string, request: UpdateEmployeeRequest): Promise<EmployeeDto> => {
  return apiClient<EmployeeDto>(`/hr/employees/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const deactivateEmployee = async (id: string, status: EmployeeStatus): Promise<void> => {
  await apiClient(`/hr/employees/${id}/deactivate`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  })
}

export const reactivateEmployee = async (id: string): Promise<void> => {
  await apiClient(`/hr/employees/${id}/reactivate`, {
    method: 'POST',
  })
}

export const linkEmployeeToUser = async (id: string, userId: string): Promise<void> => {
  await apiClient(`/hr/employees/${id}/link-user`, {
    method: 'POST',
    body: JSON.stringify({ userId }),
  })
}

// ─── Department endpoints ──────────────────────────────────────────────────

export const getDepartments = async (): Promise<DepartmentTreeNodeDto[]> => {
  return apiClient<DepartmentTreeNodeDto[]>('/hr/departments')
}

export const getDepartmentById = async (id: string): Promise<DepartmentDto> => {
  return apiClient<DepartmentDto>(`/hr/departments/${id}`)
}

export const createDepartment = async (request: CreateDepartmentRequest): Promise<DepartmentDto> => {
  return apiClient<DepartmentDto>('/hr/departments', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateDepartment = async (id: string, request: UpdateDepartmentRequest): Promise<DepartmentDto> => {
  return apiClient<DepartmentDto>(`/hr/departments/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const deleteDepartment = async (id: string): Promise<void> => {
  await apiClient(`/hr/departments/${id}`, {
    method: 'DELETE',
  })
}

export const reorderDepartments = async (request: ReorderDepartmentsRequest): Promise<void> => {
  await apiClient('/hr/departments/reorder', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

// ─── Tag endpoints ────────────────────────────────────────────────────────

export const getTags = async (params?: { category?: EmployeeTagCategory; isActive?: boolean }): Promise<EmployeeTagDto[]> => {
  const queryParams = new URLSearchParams()
  if (params?.category) queryParams.append('category', params.category)
  if (params?.isActive != null) queryParams.append('isActive', params.isActive.toString())
  const query = queryParams.toString()
  return apiClient<EmployeeTagDto[]>(`/hr/tags${query ? `?${query}` : ''}`)
}

export const getTagById = async (id: string): Promise<EmployeeTagDto> => {
  return apiClient<EmployeeTagDto>(`/hr/tags/${id}`)
}

export const createTag = async (request: CreateTagRequest): Promise<EmployeeTagDto> => {
  return apiClient<EmployeeTagDto>('/hr/tags', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateTag = async (id: string, request: UpdateTagRequest): Promise<EmployeeTagDto> => {
  return apiClient<EmployeeTagDto>(`/hr/tags/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const deleteTag = async (id: string): Promise<void> => {
  await apiClient(`/hr/tags/${id}`, {
    method: 'DELETE',
  })
}

export const assignTagsToEmployee = async (employeeId: string, request: AssignTagsRequest): Promise<TagBriefDto[]> => {
  return apiClient<TagBriefDto[]>(`/hr/tags/employees/${employeeId}/assign`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const removeTagsFromEmployee = async (employeeId: string, request: RemoveTagsRequest): Promise<TagBriefDto[]> => {
  return apiClient<TagBriefDto[]>(`/hr/tags/employees/${employeeId}/remove`, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const getEmployeesByTag = async (tagId: string, params?: { page?: number; pageSize?: number }): Promise<EmployeePagedResult> => {
  const queryParams = new URLSearchParams()
  if (params?.page != null) queryParams.append('page', params.page.toString())
  if (params?.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  const query = queryParams.toString()
  return apiClient<EmployeePagedResult>(`/hr/tags/${tagId}/employees${query ? `?${query}` : ''}`)
}

// ─── Org Chart endpoints ──────────────────────────────────────────────────

export const getOrgChart = async (departmentId?: string): Promise<OrgChartNodeDto[]> => {
  const queryParams = new URLSearchParams()
  if (departmentId) queryParams.append('departmentId', departmentId)
  const query = queryParams.toString()
  return apiClient<OrgChartNodeDto[]>(`/hr/employees/org-chart${query ? `?${query}` : ''}`)
}

// ─── Reports endpoints ────────────────────────────────────────────────────

export const getHrReports = async (): Promise<HrReportsDto> => {
  return apiClient<HrReportsDto>('/hr/employees/reports')
}

// ─── Import/Export endpoints ──────────────────────────────────────────────

export const exportEmployees = async (params?: {
  format?: string
  departmentId?: string
  status?: EmployeeStatus
  employmentType?: EmploymentType
}): Promise<Blob> => {
  const queryParams = new URLSearchParams()
  if (params?.format) queryParams.append('format', params.format)
  if (params?.departmentId) queryParams.append('departmentId', params.departmentId)
  if (params?.status) queryParams.append('status', params.status)
  if (params?.employmentType) queryParams.append('employmentType', params.employmentType)
  const query = queryParams.toString()

  const { getAccessToken } = await import('./tokenStorage')
  const { i18n } = await import('@/i18n')
  const token = getAccessToken()
  const response = await fetch(`/api/hr/employees/export${query ? `?${query}` : ''}`, {
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      'Accept-Language': i18n.language,
    },
    credentials: 'include',
  })
  if (!response.ok) throw new Error('Export failed')
  return response.blob()
}

export const importEmployees = async (file: File): Promise<ImportResultDto> => {
  const { getAccessToken } = await import('./tokenStorage')
  const { i18n } = await import('@/i18n')
  const token = getAccessToken()
  const formData = new FormData()
  formData.append('file', file)
  const response = await fetch('/api/hr/employees/import', {
    method: 'POST',
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      'Accept-Language': i18n.language,
    },
    credentials: 'include',
    body: formData,
  })
  if (!response.ok) {
    const error = await response.json().catch(() => ({ title: 'Import failed' }))
    throw new Error(error.detail || error.title || 'Import failed')
  }
  return response.json()
}

// ─── Bulk operation endpoints ─────────────────────────────────────────────

export const bulkAssignTags = async (data: BulkAssignTagsRequest): Promise<void> => {
  await apiClient('/hr/employees/bulk-assign-tags', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export const bulkChangeDepartment = async (data: BulkChangeDepartmentRequest): Promise<void> => {
  await apiClient('/hr/employees/bulk-change-department', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}
