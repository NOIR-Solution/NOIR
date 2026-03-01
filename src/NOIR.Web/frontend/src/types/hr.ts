/**
 * HR module types matching backend DTOs.
 */

// Enums
export type EmployeeStatus = 'Active' | 'Suspended' | 'Resigned' | 'Terminated'
export type EmploymentType = 'FullTime' | 'PartTime' | 'Contract' | 'Intern'
export type EmployeeTagCategory = 'Team' | 'Skill' | 'Project' | 'Location' | 'Seniority' | 'Employment' | 'Custom'

// Employee DTOs
export interface EmployeeDto {
  id: string
  employeeCode: string
  firstName: string
  lastName: string
  email: string
  phone?: string | null
  avatarUrl?: string | null
  departmentId: string
  departmentName: string
  position?: string | null
  managerId?: string | null
  managerName?: string | null
  userId?: string | null
  hasUserAccount: boolean
  joinDate: string
  endDate?: string | null
  status: EmployeeStatus
  employmentType: EmploymentType
  notes?: string | null
  tags: TagBriefDto[]
  directReports: DirectReportDto[]
  createdAt: string
  lastModifiedAt: string
}

export interface EmployeeListDto {
  id: string
  employeeCode: string
  firstName: string
  lastName: string
  email: string
  avatarUrl?: string | null
  departmentName: string
  position?: string | null
  managerName?: string | null
  status: EmployeeStatus
  employmentType: EmploymentType
  tags: TagBriefDto[]
}

export interface EmployeeSearchDto {
  id: string
  employeeCode: string
  fullName: string
  avatarUrl?: string | null
  position?: string | null
  departmentName: string
}

export interface DirectReportDto {
  id: string
  employeeCode: string
  fullName: string
  avatarUrl?: string | null
  position?: string | null
  status: EmployeeStatus
}

export interface TagBriefDto {
  id: string
  name: string
  category: EmployeeTagCategory
  color: string
}

// Department DTOs
export interface DepartmentDto {
  id: string
  name: string
  code: string
  description?: string | null
  managerId?: string | null
  managerName?: string | null
  parentDepartmentId?: string | null
  parentDepartmentName?: string | null
  sortOrder: number
  isActive: boolean
  employeeCount: number
  subDepartments: DepartmentTreeNodeDto[]
}

export interface DepartmentTreeNodeDto {
  id: string
  name: string
  code: string
  managerName?: string | null
  employeeCount: number
  isActive: boolean
  children: DepartmentTreeNodeDto[]
}

// Request types
export interface CreateEmployeeRequest {
  firstName: string
  lastName: string
  email: string
  phone?: string | null
  departmentId: string
  position?: string | null
  managerId?: string | null
  joinDate: string
  employmentType: EmploymentType
  notes?: string | null
  createUserAccount?: boolean
}

export interface UpdateEmployeeRequest {
  firstName: string
  lastName: string
  email: string
  phone?: string | null
  departmentId: string
  position?: string | null
  managerId?: string | null
  employmentType: EmploymentType
  notes?: string | null
}

export interface CreateDepartmentRequest {
  name: string
  code: string
  description?: string | null
  parentDepartmentId?: string | null
  managerId?: string | null
}

export interface UpdateDepartmentRequest {
  name: string
  code: string
  description?: string | null
  parentDepartmentId?: string | null
  managerId?: string | null
}

export interface ReorderDepartmentsRequest {
  items: { departmentId: string; sortOrder: number }[]
}

// Tag DTOs
export interface EmployeeTagDto {
  id: string
  name: string
  category: EmployeeTagCategory
  color: string
  description?: string | null
  sortOrder: number
  isActive: boolean
  employeeCount: number
  createdAt: string
  lastModifiedAt?: string | null
}

export interface CreateTagRequest {
  name: string
  category: EmployeeTagCategory
  color?: string | null
  description?: string | null
  sortOrder?: number
}

export interface UpdateTagRequest {
  name: string
  category: EmployeeTagCategory
  color?: string | null
  description?: string | null
  sortOrder?: number
}

export interface AssignTagsRequest {
  tagIds: string[]
}

export interface RemoveTagsRequest {
  tagIds: string[]
}

// Paged result
export interface EmployeePagedResult {
  items: EmployeeListDto[]
  totalCount: number
  pageIndex: number
  pageNumber: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// Org Chart DTOs
export type OrgChartNodeType = 'Department' | 'Employee'

export interface OrgChartNodeDto {
  id: string
  type: OrgChartNodeType
  name: string
  subtitle?: string | null
  avatarUrl?: string | null
  employeeCount?: number | null
  status?: EmployeeStatus | null
  children: OrgChartNodeDto[]
}

// Reports DTOs
export interface HrReportsDto {
  headcountByDepartment: DepartmentHeadcountDto[]
  tagDistribution: TagDistributionDto[]
  employmentTypeBreakdown: EmploymentTypeBreakdownDto[]
  statusBreakdown: StatusBreakdownDto[]
  totalActiveEmployees: number
  totalDepartments: number
}

export interface DepartmentHeadcountDto {
  departmentId: string
  departmentName: string
  count: number
}

export interface TagDistributionDto {
  tagId: string
  tagName: string
  category: EmployeeTagCategory
  color: string
  count: number
}

export interface EmploymentTypeBreakdownDto {
  type: EmploymentType
  count: number
}

export interface StatusBreakdownDto {
  status: EmployeeStatus
  count: number
}

// Import/Export DTOs
export interface ImportResultDto {
  totalRows: number
  successCount: number
  failedCount: number
  errors: { rowNumber: number; message: string }[]
}

// Bulk operation request types
export interface BulkAssignTagsRequest {
  employeeIds: string[]
  tagIds: string[]
}

export interface BulkChangeDepartmentRequest {
  employeeIds: string[]
  newDepartmentId: string
}

// Query params
export interface GetEmployeesParams {
  page?: number
  pageSize?: number
  search?: string
  departmentId?: string
  status?: EmployeeStatus
  employmentType?: EmploymentType
  orderBy?: string
  isDescending?: boolean
}
