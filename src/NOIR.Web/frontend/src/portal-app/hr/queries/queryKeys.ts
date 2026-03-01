import type { GetEmployeesParams } from '@/types/hr'

export const employeeKeys = {
  all: ['hr-employees'] as const,
  lists: () => [...employeeKeys.all, 'list'] as const,
  list: (params: GetEmployeesParams) => [...employeeKeys.lists(), params] as const,
  details: () => [...employeeKeys.all, 'detail'] as const,
  detail: (id: string) => [...employeeKeys.details(), id] as const,
  search: (query: string) => [...employeeKeys.all, 'search', query] as const,
}

export const departmentKeys = {
  all: ['hr-departments'] as const,
  lists: () => [...departmentKeys.all, 'list'] as const,
  list: () => [...departmentKeys.lists()] as const,
  details: () => [...departmentKeys.all, 'detail'] as const,
  detail: (id: string) => [...departmentKeys.details(), id] as const,
}

export const tagKeys = {
  all: ['hr-tags'] as const,
  lists: () => [...tagKeys.all, 'list'] as const,
  list: (filters: Record<string, unknown>) => [...tagKeys.lists(), filters] as const,
  details: () => [...tagKeys.all, 'detail'] as const,
  detail: (id: string) => [...tagKeys.details(), id] as const,
  employees: (id: string) => [...tagKeys.all, 'employees', id] as const,
}
