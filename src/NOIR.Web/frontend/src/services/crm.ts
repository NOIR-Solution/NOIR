import { apiClient } from './apiClient'
import type {
  ContactDto,
  ContactPagedResult,
  CreateContactRequest,
  UpdateContactRequest,
  GetContactsParams,
  CompanyDto,
  CompanyPagedResult,
  CreateCompanyRequest,
  UpdateCompanyRequest,
  GetCompaniesParams,
  LeadDto,
  LeadPagedResult,
  CreateLeadRequest,
  UpdateLeadRequest,
  MoveLeadStageRequest,
  GetLeadsParams,
  PipelineDto,
  PipelineViewDto,
  CreatePipelineRequest,
  UpdatePipelineRequest,
  ActivityDto,
  ActivityPagedResult,
  CreateActivityRequest,
  UpdateActivityRequest,
  GetActivitiesParams,
  CrmDashboardDto,
} from '@/types/crm'

// ─── Contact endpoints ─────────────────────────────────────────────────────

export const getContacts = async (params: GetContactsParams = {}): Promise<ContactPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.search) queryParams.append('search', params.search)
  if (params.companyId) queryParams.append('companyId', params.companyId)
  if (params.ownerId) queryParams.append('ownerId', params.ownerId)
  if (params.source) queryParams.append('source', params.source)
  if (params.orderBy) queryParams.append('orderBy', params.orderBy)
  if (params.isDescending != null) queryParams.append('isDescending', params.isDescending.toString())

  const query = queryParams.toString()
  return apiClient<ContactPagedResult>(`/crm/contacts${query ? `?${query}` : ''}`)
}

export const getContactById = async (id: string): Promise<ContactDto> => {
  return apiClient<ContactDto>(`/crm/contacts/${id}`)
}

export const createContact = async (request: CreateContactRequest): Promise<ContactDto> => {
  return apiClient<ContactDto>('/crm/contacts', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateContact = async (id: string, request: UpdateContactRequest): Promise<ContactDto> => {
  return apiClient<ContactDto>(`/crm/contacts/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const deleteContact = async (id: string): Promise<void> => {
  await apiClient(`/crm/contacts/${id}`, {
    method: 'DELETE',
  })
}

// ─── Company endpoints ─────────────────────────────────────────────────────

export const getCompanies = async (params: GetCompaniesParams = {}): Promise<CompanyPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.search) queryParams.append('search', params.search)
  if (params.orderBy) queryParams.append('orderBy', params.orderBy)
  if (params.isDescending != null) queryParams.append('isDescending', params.isDescending.toString())

  const query = queryParams.toString()
  return apiClient<CompanyPagedResult>(`/crm/companies${query ? `?${query}` : ''}`)
}

export const getCompanyById = async (id: string): Promise<CompanyDto> => {
  return apiClient<CompanyDto>(`/crm/companies/${id}`)
}

export const createCompany = async (request: CreateCompanyRequest): Promise<CompanyDto> => {
  return apiClient<CompanyDto>('/crm/companies', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateCompany = async (id: string, request: UpdateCompanyRequest): Promise<CompanyDto> => {
  return apiClient<CompanyDto>(`/crm/companies/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const deleteCompany = async (id: string): Promise<void> => {
  await apiClient(`/crm/companies/${id}`, {
    method: 'DELETE',
  })
}

// ─── Lead endpoints ────────────────────────────────────────────────────────

export const getLeads = async (params: GetLeadsParams = {}): Promise<LeadPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())
  if (params.pipelineId) queryParams.append('pipelineId', params.pipelineId)
  if (params.stageId) queryParams.append('stageId', params.stageId)
  if (params.ownerId) queryParams.append('ownerId', params.ownerId)
  if (params.status) queryParams.append('status', params.status)

  const query = queryParams.toString()
  return apiClient<LeadPagedResult>(`/crm/leads${query ? `?${query}` : ''}`)
}

export const getLeadById = async (id: string): Promise<LeadDto> => {
  return apiClient<LeadDto>(`/crm/leads/${id}`)
}

export const createLead = async (request: CreateLeadRequest): Promise<LeadDto> => {
  return apiClient<LeadDto>('/crm/leads', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateLead = async (id: string, request: UpdateLeadRequest): Promise<LeadDto> => {
  return apiClient<LeadDto>(`/crm/leads/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const moveLeadStage = async (request: MoveLeadStageRequest): Promise<LeadDto> => {
  return apiClient<LeadDto>(`/crm/leads/${request.leadId}/move-stage`, {
    method: 'POST',
    body: JSON.stringify({ newStageId: request.newStageId, newSortOrder: request.newSortOrder }),
  })
}

export const winLead = async (id: string): Promise<LeadDto> => {
  return apiClient<LeadDto>(`/crm/leads/${id}/win`, {
    method: 'POST',
  })
}

export const loseLead = async (id: string, reason?: string): Promise<LeadDto> => {
  return apiClient<LeadDto>(`/crm/leads/${id}/lose`, {
    method: 'POST',
    body: JSON.stringify({ reason }),
  })
}

export const reopenLead = async (id: string): Promise<LeadDto> => {
  return apiClient<LeadDto>(`/crm/leads/${id}/reopen`, {
    method: 'POST',
  })
}

// ─── Pipeline endpoints ────────────────────────────────────────────────────

export const getPipelines = async (): Promise<PipelineDto[]> => {
  return apiClient<PipelineDto[]>('/crm/pipelines')
}

export const getPipelineView = async (pipelineId: string): Promise<PipelineViewDto> => {
  return apiClient<PipelineViewDto>(`/crm/pipelines/${pipelineId}/view`)
}

export const createPipeline = async (request: CreatePipelineRequest): Promise<PipelineDto> => {
  return apiClient<PipelineDto>('/crm/pipelines', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updatePipeline = async (id: string, request: UpdatePipelineRequest): Promise<PipelineDto> => {
  return apiClient<PipelineDto>(`/crm/pipelines/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const deletePipeline = async (id: string): Promise<void> => {
  await apiClient(`/crm/pipelines/${id}`, {
    method: 'DELETE',
  })
}

// ─── Activity endpoints ────────────────────────────────────────────────────

export const getActivities = async (params: GetActivitiesParams = {}): Promise<ActivityPagedResult> => {
  const queryParams = new URLSearchParams()
  if (params.contactId) queryParams.append('contactId', params.contactId)
  if (params.leadId) queryParams.append('leadId', params.leadId)
  if (params.page != null) queryParams.append('page', params.page.toString())
  if (params.pageSize != null) queryParams.append('pageSize', params.pageSize.toString())

  const query = queryParams.toString()
  return apiClient<ActivityPagedResult>(`/crm/activities${query ? `?${query}` : ''}`)
}

export const createActivity = async (request: CreateActivityRequest): Promise<ActivityDto> => {
  return apiClient<ActivityDto>('/crm/activities', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateActivity = async (id: string, request: UpdateActivityRequest): Promise<ActivityDto> => {
  return apiClient<ActivityDto>(`/crm/activities/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const deleteActivity = async (id: string): Promise<void> => {
  await apiClient(`/crm/activities/${id}`, {
    method: 'DELETE',
  })
}

// ─── Dashboard endpoints ───────────────────────────────────────────────────

export const getCrmDashboard = async (): Promise<CrmDashboardDto> => {
  return apiClient<CrmDashboardDto>('/crm/dashboard')
}
