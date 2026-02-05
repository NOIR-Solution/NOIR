/**
 * Application Routes
 *
 * Centralized route definitions to ensure consistency across all tests.
 * Based on actual routes from src/components/portal/Sidebar.tsx
 */

export const ROUTES = {
  // Authentication
  LOGIN: '/login',
  FORGOT_PASSWORD: '/forgot-password',

  // Portal
  PORTAL: '/portal',
  DASHBOARD: '/portal',

  // E-commerce
  PRODUCTS: {
    LIST: '/portal/ecommerce/products',
    NEW: '/portal/ecommerce/products/new',
    EDIT: (id: string) => `/portal/ecommerce/products/${id}/edit`,
    VIEW: (id: string) => `/portal/ecommerce/products/${id}`,
  },
  CATEGORIES: {
    LIST: '/portal/ecommerce/categories',
  },
  BRANDS: {
    LIST: '/portal/ecommerce/brands',
  },
  ATTRIBUTES: {
    LIST: '/portal/ecommerce/attributes',
  },

  // Content (Blog)
  BLOG: {
    POSTS: '/portal/blog/posts',
    CATEGORIES: '/portal/blog/categories',
    TAGS: '/portal/blog/tags',
  },

  // Admin
  USERS: {
    LIST: '/portal/admin/users',
  },
  ROLES: {
    LIST: '/portal/admin/roles',
  },
  TENANTS: {
    LIST: '/portal/admin/tenants',
  },

  // Settings
  PLATFORM_SETTINGS: '/portal/admin/platform-settings',
  TENANT_SETTINGS: '/portal/admin/tenant-settings',

  // System
  ACTIVITY_TIMELINE: '/portal/activity-timeline',
  DEVELOPER_LOGS: '/portal/developer-logs',
} as const;

/**
 * Route patterns for assertions
 */
export const ROUTE_PATTERNS = {
  LOGIN: /\/login/,
  PORTAL: /\/portal/,
  PRODUCTS_LIST: /\/portal\/ecommerce\/products$/,
  PRODUCTS_NEW: /\/portal\/ecommerce\/products\/new$/,
  PRODUCTS_EDIT: /\/portal\/ecommerce\/products\/[^/]+\/edit$/,
  CATEGORIES_LIST: /\/portal\/ecommerce\/categories/,
  BRANDS_LIST: /\/portal\/ecommerce\/brands/,
  ATTRIBUTES_LIST: /\/portal\/ecommerce\/attributes/,
  USERS_LIST: /\/portal\/admin\/users/,
  ROLES_LIST: /\/portal\/admin\/roles/,
  TENANTS_LIST: /\/portal\/admin\/tenants/,
  TENANT_SETTINGS: /\/portal\/admin\/tenant-settings/,
} as const;
