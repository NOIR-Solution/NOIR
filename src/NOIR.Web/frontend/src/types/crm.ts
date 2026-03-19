// ─── Enums ─────────────────────────────────────────────────────────────────

export type ContactSource = 'Web' | 'Referral' | 'Social' | 'Cold' | 'Event' | 'Other'
export type LeadStatus = 'Active' | 'Won' | 'Lost'
export type ActivityType = 'Call' | 'Email' | 'Meeting' | 'Note'
export type StageType = 'Active' | 'Won' | 'Lost'

// ─── Contact DTOs ──────────────────────────────────────────────────────────

export interface ContactDto {
  id: string
  firstName: string
  lastName: string
  email: string
  phone?: string
  jobTitle?: string
  companyId?: string
  companyName?: string
  ownerId?: string
  ownerName?: string
  source: ContactSource
  customerId?: string
  notes?: string
  leadCount: number
  createdAt: string
  modifiedAt?: string
}

export interface ContactListDto {
  id: string
  firstName: string
  lastName: string
  email: string
  phone?: string
  jobTitle?: string
  companyName?: string
  ownerName?: string
  source: ContactSource
  hasCustomer: boolean
  leadCount: number
  createdAt: string
  modifiedAt?: string
  modifiedByName?: string
}

export interface ContactPagedResult {
  items: ContactListDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// ─── Company DTOs ──────────────────────────────────────────────────────────

export interface CompanyDto {
  id: string
  name: string
  domain?: string
  industry?: string
  address?: string
  phone?: string
  website?: string
  ownerId?: string
  ownerName?: string
  taxId?: string
  employeeCount?: number
  notes?: string
  contactCount: number
  createdAt: string
  modifiedAt?: string
}

export interface CompanyListDto {
  id: string
  name: string
  domain?: string
  industry?: string
  ownerName?: string
  contactCount: number
  createdAt: string
  modifiedAt?: string
  modifiedByName?: string
}

export interface CompanyPagedResult {
  items: CompanyListDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// ─── Lead DTOs ─────────────────────────────────────────────────────────────

export interface LeadDto {
  id: string
  title: string
  contactId: string
  contactName: string
  contactEmail: string
  companyId?: string
  companyName?: string
  value: number
  currency: string
  ownerId?: string
  ownerName?: string
  pipelineId: string
  pipelineName: string
  stageId: string
  stageName: string
  stageColor: string
  status: LeadStatus
  sortOrder: number
  expectedCloseDate?: string
  wonAt?: string
  lostAt?: string
  lostReason?: string
  notes?: string
  createdAt: string
  modifiedAt?: string
}

export interface LeadCardDto {
  id: string
  title: string
  contactName: string
  value: number
  currency: string
  ownerName?: string
  status: LeadStatus
  sortOrder: number
  expectedCloseDate?: string
  createdAt: string
}

export interface LeadPagedResult {
  items: LeadDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// ─── Pipeline DTOs ─────────────────────────────────────────────────────────

export interface PipelineDto {
  id: string
  name: string
  isDefault: boolean
  stages: PipelineStageDto[]
}

export interface PipelineStageDto {
  id: string
  name: string
  sortOrder: number
  color: string
  stageType: StageType
  isSystem: boolean
}

export interface StageWithLeadsDto {
  id: string
  name: string
  color: string
  sortOrder: number
  totalValue: number
  leadCount: number
  leads: LeadCardDto[]
  stageType: StageType
  isSystem: boolean
}

export interface PipelineViewDto {
  id: string
  name: string
  stages: StageWithLeadsDto[]
}

// ─── Activity DTOs ─────────────────────────────────────────────────────────

export interface ActivityDto {
  id: string
  type: ActivityType
  subject: string
  description?: string
  contactId?: string
  contactName?: string
  leadId?: string
  leadTitle?: string
  performedById: string
  performedByName: string
  performedAt: string
  durationMinutes?: number
  createdAt: string
}

export interface ActivityPagedResult {
  items: ActivityDto[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// ─── Dashboard DTOs ────────────────────────────────────────────────────────

export interface LeadsByStageDto {
  stageName: string
  color: string
  count: number
  totalValue: number
}

export interface LeadsByOwnerDto {
  ownerName: string
  count: number
  totalValue: number
}

export interface CrmDashboardDto {
  totalContacts: number
  totalCompanies: number
  activeLeads: number
  wonLeads: number
  lostLeads: number
  totalPipelineValue: number
  wonDealValue: number
  wonDealsThisMonth: number
  wonValueThisMonth: number
  lostDealsThisMonth: number
  conversionRate: number
  leadsByStage: LeadsByStageDto[]
  leadsByOwner: LeadsByOwnerDto[]
}

// ─── Request Types ─────────────────────────────────────────────────────────

export interface CreateContactRequest {
  firstName: string
  lastName: string
  email: string
  phone?: string
  jobTitle?: string
  companyId?: string
  ownerId?: string
  source: ContactSource
  notes?: string
}

export interface UpdateContactRequest extends CreateContactRequest {}

export interface CreateCompanyRequest {
  name: string
  domain?: string
  industry?: string
  address?: string
  phone?: string
  website?: string
  ownerId?: string
  taxId?: string
  employeeCount?: number
  notes?: string
}

export interface UpdateCompanyRequest extends CreateCompanyRequest {}

export interface CreateLeadRequest {
  title: string
  contactId: string
  companyId?: string
  value: number
  currency?: string
  ownerId?: string
  pipelineId: string
  expectedCloseDate?: string
  notes?: string
}

export interface UpdateLeadRequest {
  title: string
  value: number
  currency?: string
  ownerId?: string
  expectedCloseDate?: string
  notes?: string
}

export interface MoveLeadStageRequest {
  leadId: string
  newStageId: string
  newSortOrder: number
}

export interface CreatePipelineRequest {
  name: string
  isDefault: boolean
  stages: { name: string; sortOrder: number; color: string }[]
}

export interface UpdatePipelineRequest {
  name: string
  stages: { id?: string; name: string; sortOrder: number; color: string }[]
}

export interface CreateActivityRequest {
  type: ActivityType
  subject: string
  description?: string
  contactId?: string
  leadId?: string
  performedAt: string
  durationMinutes?: number
}

export interface UpdateActivityRequest {
  type: ActivityType
  subject: string
  description?: string
  performedAt: string
  durationMinutes?: number
}

// ─── Query Params ──────────────────────────────────────────────────────────

export interface GetContactsParams {
  page?: number
  pageSize?: number
  search?: string
  companyId?: string
  ownerId?: string
  source?: ContactSource
  orderBy?: string
  isDescending?: boolean
}

export interface GetCompaniesParams {
  page?: number
  pageSize?: number
  search?: string
  orderBy?: string
  isDescending?: boolean
}

export interface GetLeadsParams {
  page?: number
  pageSize?: number
  pipelineId?: string
  stageId?: string
  ownerId?: string
  status?: LeadStatus
}

export interface GetActivitiesParams {
  contactId?: string
  leadId?: string
  page?: number
  pageSize?: number
}
