import { useCallback, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { getUserPermissions } from '@/services/auth'
import { useAuthContext } from '@/contexts/AuthContext'
import { authKeys } from '@/hooks/queries/queryKeys'

interface UsePermissionsResult {
  /** Raw permissions array from the server */
  permissions: string[]
  /** User's roles */
  roles: string[]
  /** Whether permissions are currently loading */
  isLoading: boolean
  /** Error if permissions failed to load */
  error: Error | null
  /** Check if user has a specific permission */
  hasPermission: (permission: string) => boolean
  /** Check if user has ALL specified permissions */
  hasAllPermissions: (permissions: string[]) => boolean
  /** Check if user has ANY of the specified permissions */
  hasAnyPermission: (permissions: string[]) => boolean
  /** Check if user has a specific role */
  hasRole: (role: string) => boolean
  /** Manually refresh permissions */
  refreshPermissions: () => Promise<void>
}

/**
 * Hook to access the current user's permissions
 * Automatically fetches permissions when authenticated and caches them
 * Provides utility functions for permission checking
 */
export const usePermissions = (): UsePermissionsResult => {
  const { t } = useTranslation('common')
  const { isAuthenticated, user } = useAuthContext()

  const { data: permissionsData, isLoading, error: queryError, refetch } = useQuery({
    queryKey: authKeys.permissions(),
    queryFn: () => getUserPermissions(),
    enabled: isAuthenticated && !!user,
    staleTime: 2 * 60_000,
  })

  const error = queryError instanceof Error
    ? queryError
    : queryError
      ? new Error(t('errors.failedToFetchPermissions', 'Failed to fetch permissions'))
      : null

  // Create a Set for O(1) permission lookup
  const permissionSet = useMemo(
    () => new Set(permissionsData?.permissions ?? []),
    [permissionsData?.permissions]
  )

  const roleSet = useMemo(
    () => new Set(permissionsData?.roles ?? []),
    [permissionsData?.roles]
  )

  const hasPermission = useCallback(
    (permission: string): boolean => {
      return permissionSet.has(permission)
    },
    [permissionSet]
  )

  const hasAllPermissions = useCallback(
    (permissions: string[]): boolean => {
      return permissions.every(p => permissionSet.has(p))
    },
    [permissionSet]
  )

  const hasAnyPermission = useCallback(
    (permissions: string[]): boolean => {
      return permissions.some(p => permissionSet.has(p))
    },
    [permissionSet]
  )

  const hasRole = useCallback(
    (role: string): boolean => {
      return roleSet.has(role)
    },
    [roleSet]
  )

  const refreshPermissions = useCallback(async () => {
    await refetch()
  }, [refetch])

  return {
    permissions: permissionsData?.permissions ?? [],
    roles: permissionsData?.roles ?? [],
    isLoading,
    error,
    hasPermission,
    hasAllPermissions,
    hasAnyPermission,
    hasRole,
    refreshPermissions,
  }
}

// Common permission constants for type-safe usage
export const Permissions = {
  // Users
  UsersRead: 'users:read',
  UsersCreate: 'users:create',
  UsersUpdate: 'users:update',
  UsersDelete: 'users:delete',
  UsersManageRoles: 'users:manage-roles',
  // Roles
  RolesRead: 'roles:read',
  RolesCreate: 'roles:create',
  RolesUpdate: 'roles:update',
  RolesDelete: 'roles:delete',
  RolesManagePermissions: 'roles:manage-permissions',
  // Email Templates
  EmailTemplatesRead: 'email-templates:read',
  EmailTemplatesUpdate: 'email-templates:update',
  // Legal Pages
  LegalPagesRead: 'legal-pages:read',
  LegalPagesUpdate: 'legal-pages:update',
  // Tenant Settings
  TenantSettingsRead: 'tenant-settings:read',
  TenantSettingsUpdate: 'tenant-settings:update',
  // Tenants
  TenantsRead: 'tenants:read',
  TenantsCreate: 'tenants:create',
  TenantsUpdate: 'tenants:update',
  TenantsDelete: 'tenants:delete',
  // System (platform-level only)
  SystemAdmin: 'system:admin',
  SystemAuditLogs: 'system:audit-logs',
  SystemSettings: 'system:settings',
  HangfireDashboard: 'system:hangfire',
  // Audit
  AuditRead: 'audit:read',
  AuditExport: 'audit:export',
  AuditEntityHistory: 'audit:entity-history',
  // Blog Posts
  BlogPostsRead: 'blog-posts:read',
  BlogPostsCreate: 'blog-posts:create',
  BlogPostsUpdate: 'blog-posts:update',
  BlogPostsDelete: 'blog-posts:delete',
  BlogPostsPublish: 'blog-posts:publish',
  // Blog Categories
  BlogCategoriesRead: 'blog-categories:read',
  BlogCategoriesCreate: 'blog-categories:create',
  BlogCategoriesUpdate: 'blog-categories:update',
  BlogCategoriesDelete: 'blog-categories:delete',
  // Blog Tags
  BlogTagsRead: 'blog-tags:read',
  BlogTagsCreate: 'blog-tags:create',
  BlogTagsUpdate: 'blog-tags:update',
  BlogTagsDelete: 'blog-tags:delete',
  // Platform Settings
  PlatformSettingsRead: 'platform-settings:read',
  PlatformSettingsManage: 'platform-settings:manage',
  // Payment Gateways
  PaymentGatewaysRead: 'payment-gateways:read',
  PaymentGatewaysManage: 'payment-gateways:manage',
  // Products
  ProductsRead: 'products:read',
  ProductsCreate: 'products:create',
  ProductsUpdate: 'products:update',
  ProductsDelete: 'products:delete',
  ProductsPublish: 'products:publish',
  // Product Categories
  ProductCategoriesRead: 'product-categories:read',
  ProductCategoriesCreate: 'product-categories:create',
  ProductCategoriesUpdate: 'product-categories:update',
  ProductCategoriesDelete: 'product-categories:delete',
  // Brands
  BrandsRead: 'brands:read',
  BrandsCreate: 'brands:create',
  BrandsUpdate: 'brands:update',
  BrandsDelete: 'brands:delete',
  // Product Attributes
  AttributesRead: 'attributes:read',
  AttributesCreate: 'attributes:create',
  AttributesUpdate: 'attributes:update',
  AttributesDelete: 'attributes:delete',
  // Payments
  PaymentsRead: 'payments:read',
  PaymentsCreate: 'payments:create',
  PaymentsManage: 'payments:manage',
  // Orders
  OrdersRead: 'orders:read',
  OrdersWrite: 'orders:write',
  OrdersManage: 'orders:manage',
  // Inventory
  InventoryRead: 'inventory:read',
  InventoryWrite: 'inventory:write',
  InventoryManage: 'inventory:manage',
  // Reviews
  ReviewsRead: 'reviews:read',
  ReviewsWrite: 'reviews:write',
  ReviewsManage: 'reviews:manage',
  // Customers
  CustomersRead: 'customers:read',
  CustomersCreate: 'customers:create',
  CustomersUpdate: 'customers:update',
  CustomersDelete: 'customers:delete',
  CustomersManage: 'customers:manage',
  // Customer Groups
  CustomerGroupsRead: 'customer-groups:read',
  CustomerGroupsCreate: 'customer-groups:create',
  CustomerGroupsUpdate: 'customer-groups:update',
  CustomerGroupsDelete: 'customer-groups:delete',
  CustomerGroupsManageMembers: 'customer-groups:manage-members',
  // Promotions
  PromotionsRead: 'promotions:read',
  PromotionsWrite: 'promotions:write',
  PromotionsDelete: 'promotions:delete',
  PromotionsManage: 'promotions:manage',
  // Wishlists
  WishlistsRead: 'wishlists:read',
  WishlistsWrite: 'wishlists:write',
  WishlistsManage: 'wishlists:manage',
  // Reports
  ReportsRead: 'reports:read',
  // Features
  FeaturesRead: 'features:read',
  FeaturesUpdate: 'features:update',
  // Webhooks
  WebhooksRead: 'webhooks:read',
  WebhooksManage: 'webhooks:manage',
  WebhooksTest: 'webhooks:test',
  // Media
  MediaRead: 'media:read',
  MediaCreate: 'media:create',
  MediaUpdate: 'media:update',
  MediaDelete: 'media:delete',
  MediaManage: 'media:manage',
  // Search
  SearchGlobal: 'search:global',
  // HR - Employees
  HrEmployeesRead: 'hr-employees:read',
  HrEmployeesCreate: 'hr-employees:create',
  HrEmployeesUpdate: 'hr-employees:update',
  HrEmployeesDelete: 'hr-employees:delete',
  // HR - Departments
  HrDepartmentsRead: 'hr-departments:read',
  HrDepartmentsCreate: 'hr-departments:create',
  HrDepartmentsUpdate: 'hr-departments:update',
  HrDepartmentsDelete: 'hr-departments:delete',
  // HR - Tags
  HrTagsRead: 'hr-tags:read',
  HrTagsManage: 'hr-tags:manage',
  // CRM - Contacts
  CrmContactsRead: 'crm-contacts:read',
  CrmContactsCreate: 'crm-contacts:create',
  CrmContactsUpdate: 'crm-contacts:update',
  CrmContactsDelete: 'crm-contacts:delete',
  // CRM - Companies
  CrmCompaniesRead: 'crm-companies:read',
  CrmCompaniesCreate: 'crm-companies:create',
  CrmCompaniesUpdate: 'crm-companies:update',
  CrmCompaniesDelete: 'crm-companies:delete',
  // CRM - Leads
  CrmLeadsRead: 'crm-leads:read',
  CrmLeadsCreate: 'crm-leads:create',
  CrmLeadsUpdate: 'crm-leads:update',
  CrmLeadsManage: 'crm-leads:manage',
  // CRM - Pipeline
  CrmPipelineManage: 'crm-pipeline:manage',
  // CRM - Activities
  CrmActivitiesRead: 'crm-activities:read',
  CrmActivitiesCreate: 'crm-activities:create',
  CrmActivitiesUpdate: 'crm-activities:update',
  CrmActivitiesDelete: 'crm-activities:delete',
  // PM - Projects
  PmProjectsRead: 'pm-projects:read',
  PmProjectsCreate: 'pm-projects:create',
  PmProjectsUpdate: 'pm-projects:update',
  PmProjectsDelete: 'pm-projects:delete',
  PmProjectsManage: 'pm-projects:manage',
  // PM - Tasks
  PmTasksRead: 'pm-tasks:read',
  PmTasksCreate: 'pm-tasks:create',
  PmTasksUpdate: 'pm-tasks:update',
  PmTasksDelete: 'pm-tasks:delete',
  PmTasksManage: 'pm-tasks:manage',
  // PM - Members
  PmMembersManage: 'pm-members:manage',
  // API Keys
  ApiKeysRead: 'api-keys:read',
  ApiKeysCreate: 'api-keys:create',
  ApiKeysDelete: 'api-keys:delete',
} as const

export type PermissionKey = (typeof Permissions)[keyof typeof Permissions]
