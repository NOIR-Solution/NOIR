import { useQuery, keepPreviousData } from '@tanstack/react-query'
import {
  getEmployees,
  getEmployeeById,
  searchEmployees,
  getDepartments,
  getDepartmentById,
  getTags,
  getTagById,
  getEmployeesByTag,
  getOrgChart,
  getHrReports,
} from '@/services/hr'
import type { GetEmployeesParams, EmployeeTagCategory } from '@/types/hr'
import { employeeKeys, departmentKeys, tagKeys, hrReportKeys } from './queryKeys'

export const useEmployeesQuery = (params: GetEmployeesParams) =>
  useQuery({
    queryKey: employeeKeys.list(params),
    queryFn: () => getEmployees(params),
    placeholderData: keepPreviousData,
  })

export const useEmployeeQuery = (id: string | undefined) =>
  useQuery({
    queryKey: employeeKeys.detail(id!),
    queryFn: () => getEmployeeById(id!),
    enabled: !!id,
  })

export const useEmployeeSearchQuery = (query: string) =>
  useQuery({
    queryKey: employeeKeys.search(query),
    queryFn: () => searchEmployees(query),
    enabled: query.length >= 2,
  })

export const useDepartmentsQuery = () =>
  useQuery({
    queryKey: departmentKeys.list(),
    queryFn: () => getDepartments(),
  })

export const useDepartmentQuery = (id: string | undefined) =>
  useQuery({
    queryKey: departmentKeys.detail(id!),
    queryFn: () => getDepartmentById(id!),
    enabled: !!id,
  })

export const useTagsQuery = (params?: { category?: EmployeeTagCategory; isActive?: boolean }) =>
  useQuery({
    queryKey: tagKeys.list(params ?? {}),
    queryFn: () => getTags(params),
  })

export const useTagQuery = (id: string | undefined) =>
  useQuery({
    queryKey: tagKeys.detail(id!),
    queryFn: () => getTagById(id!),
    enabled: !!id,
  })

export const useEmployeesByTagQuery = (tagId: string | undefined, params?: { page?: number; pageSize?: number }) =>
  useQuery({
    queryKey: tagKeys.employees(tagId!),
    queryFn: () => getEmployeesByTag(tagId!, params),
    enabled: !!tagId,
  })

export const useOrgChartQuery = (departmentId?: string) =>
  useQuery({
    queryKey: employeeKeys.orgChart(departmentId),
    queryFn: () => getOrgChart(departmentId),
  })

export const useHrReportsQuery = () =>
  useQuery({
    queryKey: hrReportKeys.reports(),
    queryFn: () => getHrReports(),
  })
