import type { PageAuditConfig } from './rules/types';
import {
  testBlogPost,
  testProduct,
  testCustomer,
  testContact,
  testLead,
  testEmployee,
  testProject,
} from '../../helpers/test-data';

/**
 * Complete registry of all NOIR portal pages for UI audit.
 *
 * Routes from: src/NOIR.Web/frontend/src/App.tsx (lines 135-233)
 * Source files are relative to: src/NOIR.Web/frontend/src/
 *
 * Two registries:
 * - ADMIN_PAGE_REGISTRY: pages accessible with tenant admin (admin@noir.local)
 * - PLATFORM_PAGE_REGISTRY: pages requiring platform admin (platform@noir.local)
 */

// ─── Admin Pages (51 pages) ──────────────────────────────────────────────

export const ADMIN_PAGE_REGISTRY: PageAuditConfig[] = [
  // ── Dashboard ──────────────────────────────────────────────
  {
    id: 'dashboard',
    domain: 'dashboard',
    url: '/portal',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/dashboard/features/dashboard/DashboardPage.tsx',
  },

  // ── Personal Settings ──────────────────────────────────────
  {
    id: 'personal-settings',
    domain: 'settings',
    url: '/portal/settings',
    requiresData: false,
    waitFor: '[role="tablist"]',
    tabs: [
      { id: 'profile', param: 'profile' },
      { id: 'security', param: 'security' },
      { id: 'appearance', param: 'appearance' },
      { id: 'notifications', param: 'notifications' },
    ],
    sourceFile: 'portal-app/settings/features/personal-settings/PersonalSettingsPage.tsx',
  },

  // ── Notifications ──────────────────────────────────────────
  {
    id: 'notifications',
    domain: 'notifications',
    url: '/portal/notifications',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/notifications/features/notifications/NotificationsPage.tsx',
  },
  {
    id: 'notification-preferences',
    domain: 'notifications',
    url: '/portal/settings/notifications',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/notifications/features/notification-preferences/NotificationPreferencesPage.tsx',
  },

  // ── Blog CMS ───────────────────────────────────────────────
  {
    id: 'blog-posts',
    domain: 'blog',
    url: '/portal/blog/posts',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/blogs/features/blog-posts/BlogPostsPage.tsx',
    dialogTriggers: [],
  },
  {
    id: 'blog-post-new',
    domain: 'blog',
    url: '/portal/blog/posts/new',
    requiresData: false,
    waitFor: 'form, [contenteditable]',
    sourceFile: 'portal-app/blogs/features/blog-post-edit/BlogPostEditPage.tsx',
  },
  {
    id: 'blog-categories',
    domain: 'blog',
    url: '/portal/blog/categories',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/blogs/features/blog-categories/BlogCategoriesPage.tsx',
    dialogTriggers: [
      { id: 'create-blog-category', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'blog-tags',
    domain: 'blog',
    url: '/portal/blog/tags',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/blogs/features/blog-tags/BlogTagsPage.tsx',
    dialogTriggers: [
      { id: 'create-blog-tag', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },

  // ── E-commerce: Products ───────────────────────────────────
  {
    id: 'products',
    domain: 'ecommerce',
    url: '/portal/ecommerce/products',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/products/features/product-list/ProductsPage.tsx',
  },
  {
    id: 'product-new',
    domain: 'ecommerce',
    url: '/portal/ecommerce/products/new',
    requiresData: false,
    waitFor: 'form',
    sourceFile: 'portal-app/products/features/product-form/ProductFormPage.tsx',
  },
  {
    id: 'product-categories',
    domain: 'ecommerce',
    url: '/portal/ecommerce/categories',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/products/features/product-categories/ProductCategoriesPage.tsx',
    dialogTriggers: [
      { id: 'create-category', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'brands',
    domain: 'ecommerce',
    url: '/portal/ecommerce/brands',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/brands/features/brand-list/BrandsPage.tsx',
    dialogTriggers: [
      { id: 'create-brand', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'product-attributes',
    domain: 'ecommerce',
    url: '/portal/ecommerce/attributes',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/products/features/product-attributes/ProductAttributesPage.tsx',
  },

  // ── E-commerce: Payments ───────────────────────────────────
  {
    id: 'payments',
    domain: 'ecommerce',
    url: '/portal/ecommerce/payments',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/payments/features/payment-list/PaymentsPage.tsx',
  },

  // ── E-commerce: Orders ─────────────────────────────────────
  {
    id: 'orders',
    domain: 'ecommerce',
    url: '/portal/ecommerce/orders',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/orders/features/order-list/OrdersPage.tsx',
  },
  {
    id: 'order-create',
    domain: 'ecommerce',
    url: '/portal/ecommerce/orders/create',
    requiresData: false,
    waitFor: 'form, main',
    sourceFile: 'portal-app/orders/features/manual-create-order/ManualCreateOrderPage.tsx',
  },

  // ── E-commerce: Inventory ──────────────────────────────────
  {
    id: 'inventory-receipts',
    domain: 'ecommerce',
    url: '/portal/ecommerce/inventory',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/inventory/features/inventory-receipts/InventoryReceiptsPage.tsx',
  },

  // ── E-commerce: Customers ──────────────────────────────────
  {
    id: 'customers',
    domain: 'ecommerce',
    url: '/portal/ecommerce/customers',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/customers/features/customer-list/CustomersPage.tsx',
    dialogTriggers: [
      { id: 'create-customer', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'customer-groups',
    domain: 'ecommerce',
    url: '/portal/ecommerce/customer-groups',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/customer-groups/features/customer-group-list/CustomerGroupsPage.tsx',
    dialogTriggers: [
      { id: 'create-customer-group', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },

  // ── E-commerce: Reviews ────────────────────────────────────
  {
    id: 'reviews',
    domain: 'ecommerce',
    url: '/portal/ecommerce/reviews',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    tabs: [
      { id: 'pending', param: 'pending' },
      { id: 'approved', param: 'approved' },
      { id: 'rejected', param: 'rejected' },
    ],
    sourceFile: 'portal-app/reviews/features/review-list/ReviewsPage.tsx',
  },

  // ── E-commerce: Wishlists ──────────────────────────────────
  {
    id: 'wishlist-analytics',
    domain: 'ecommerce',
    url: '/portal/ecommerce/wishlists',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/wishlists/features/wishlist-analytics/WishlistAnalyticsPage.tsx',
  },
  {
    id: 'wishlist-manage',
    domain: 'ecommerce',
    url: '/portal/ecommerce/wishlists/manage',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/wishlists/features/wishlist-manage/WishlistPage.tsx',
  },

  // ── CRM ────────────────────────────────────────────────────
  {
    id: 'crm-contacts',
    domain: 'crm',
    url: '/portal/crm/contacts',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/crm/features/contact-list/CrmContactsPage.tsx',
    dialogTriggers: [
      { id: 'create-contact', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'crm-companies',
    domain: 'crm',
    url: '/portal/crm/companies',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/crm/features/company-list/CrmCompaniesPage.tsx',
    dialogTriggers: [
      { id: 'create-company', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'crm-pipeline',
    domain: 'crm',
    url: '/portal/crm/pipeline',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/crm/features/pipeline-kanban/PipelineKanbanPage.tsx',
  },

  // ── HR ─────────────────────────────────────────────────────
  {
    id: 'hr-employees',
    domain: 'hr',
    url: '/portal/hr/employees',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/hr/features/employee-list/EmployeesPage.tsx',
    dialogTriggers: [
      { id: 'create-employee', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'hr-departments',
    domain: 'hr',
    url: '/portal/hr/departments',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/hr/features/department-list/DepartmentsPage.tsx',
    dialogTriggers: [
      { id: 'create-department', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'hr-tags',
    domain: 'hr',
    url: '/portal/hr/tags',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/hr/features/tag-list/TagsPage.tsx',
    dialogTriggers: [
      { id: 'create-tag', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'hr-org-chart',
    domain: 'hr',
    url: '/portal/hr/org-chart',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/hr/features/org-chart/OrgChartPage.tsx',
  },
  {
    id: 'hr-reports',
    domain: 'hr',
    url: '/portal/hr/reports',
    requiresData: false,
    waitFor: 'main',
    tabs: [
      { id: 'headcount', param: 'headcount' },
      { id: 'departments', param: 'departments' },
      { id: 'tags', param: 'tags' },
    ],
    sourceFile: 'portal-app/hr/features/hr-reports/HrReportsPage.tsx',
  },

  // ── Project Management ─────────────────────────────────────
  {
    id: 'pm-projects',
    domain: 'pm',
    url: '/portal/projects',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/pm/features/project-list/PmProjectsPage.tsx',
    dialogTriggers: [
      { id: 'create-project', label: /create|add|new/i, waitForSelector: '[role="dialog"]' },
    ],
  },

  // ── Marketing ──────────────────────────────────────────────
  {
    id: 'promotions',
    domain: 'marketing',
    url: '/portal/marketing/promotions',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/promotions/features/promotion-list/PromotionsPage.tsx',
    dialogTriggers: [
      { id: 'create-promotion', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'marketing-reports',
    domain: 'marketing',
    url: '/portal/marketing/reports',
    requiresData: false,
    waitFor: 'main',
    tabs: [
      { id: 'revenue', param: 'revenue' },
      { id: 'orders', param: 'orders' },
      { id: 'products', param: 'products' },
      { id: 'customers', param: 'customers' },
    ],
    sourceFile: 'portal-app/reports/features/reports/ReportsPage.tsx',
  },

  // ── Media ──────────────────────────────────────────────────
  {
    id: 'media-library',
    domain: 'media',
    url: '/portal/media',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/media/features/media-library/MediaLibraryPage.tsx',
  },

  // ── Admin: Users & Roles ───────────────────────────────────
  {
    id: 'roles',
    domain: 'admin',
    url: '/portal/admin/roles',
    requiresData: false,
    waitFor: 'table, .border-dashed',
    sourceFile: 'portal-app/user-access/features/role-list/RolesPage.tsx',
    dialogTriggers: [
      { id: 'create-role', label: /create|add/i, waitForSelector: '[role="dialog"]' },
    ],
  },
  {
    id: 'users',
    domain: 'admin',
    url: '/portal/admin/users',
    requiresData: false,
    waitFor: 'table',
    sourceFile: 'portal-app/user-access/features/user-list/UsersPage.tsx',
    dialogTriggers: [
      { id: 'create-user', label: /create|add|invite/i, waitForSelector: '[role="dialog"]' },
    ],
  },

  // ── System ─────────────────────────────────────────────────
  {
    id: 'activity-timeline',
    domain: 'system',
    url: '/portal/activity-timeline',
    requiresData: false,
    waitFor: 'main',
    sourceFile: 'portal-app/systems/features/activity-timeline/ActivityTimelinePage.tsx',
  },

  // ── Detail Pages (need seeded data) ────────────────────────
  {
    id: 'blog-post-edit',
    domain: 'blog',
    url: '/portal/blog/posts/:id/edit',
    requiresData: true,
    seedFn: async (api) => {
      const post = await api.createBlogPost(testBlogPost());
      return { cleanup: () => api.deleteBlogPost(post.id), routeParam: post.id };
    },
    waitFor: 'form, [contenteditable]',
    sourceFile: 'portal-app/blogs/features/blog-post-edit/BlogPostEditPage.tsx',
  },
  {
    id: 'product-detail',
    domain: 'ecommerce',
    url: '/portal/ecommerce/products/:id',
    requiresData: true,
    seedFn: async (api) => {
      const product = await api.createProduct(testProduct());
      return { cleanup: () => api.deleteProduct(product.id), routeParam: product.id };
    },
    waitFor: 'form, main',
    sourceFile: 'portal-app/products/features/product-form/ProductFormPage.tsx',
  },
  {
    id: 'customer-detail',
    domain: 'ecommerce',
    url: '/portal/ecommerce/customers/:id',
    requiresData: true,
    seedFn: async (api) => {
      const customer = await api.createCustomer(testCustomer());
      return { cleanup: () => api.deleteCustomer(customer.id), routeParam: customer.id };
    },
    waitFor: '[role="tablist"], main',
    tabs: [
      { id: 'overview', param: 'overview' },
      { id: 'orders', param: 'orders' },
      { id: 'addresses', param: 'addresses' },
      { id: 'timeline', param: 'timeline' },
    ],
    sourceFile: 'portal-app/customers/features/customer-detail/CustomerDetailPage.tsx',
  },
  {
    id: 'crm-contact-detail',
    domain: 'crm',
    url: '/portal/crm/contacts/:id',
    requiresData: true,
    seedFn: async (api) => {
      const contact = await api.createContact(testContact());
      return { cleanup: () => api.deleteContact(contact.id), routeParam: contact.id };
    },
    waitFor: '[role="tablist"], main',
    tabs: [
      { id: 'overview', param: 'overview' },
      { id: 'activities', param: 'activities' },
      { id: 'deals', param: 'deals' },
      { id: 'timeline', param: 'timeline' },
    ],
    sourceFile: 'portal-app/crm/features/contact-detail/CrmContactDetailPage.tsx',
  },
  {
    id: 'crm-company-detail',
    domain: 'crm',
    url: '/portal/crm/companies/:id',
    requiresData: true,
    seedFn: async (api) => {
      const company = await api.createCompany({ name: `AuditCo-${Date.now()}` });
      return { cleanup: () => api.deleteCompany(company.id), routeParam: company.id };
    },
    waitFor: '[role="tablist"], main',
    tabs: [
      { id: 'overview', param: 'overview' },
      { id: 'contacts', param: 'contacts' },
      { id: 'activities', param: 'activities' },
    ],
    sourceFile: 'portal-app/crm/features/company-detail/CrmCompanyDetailPage.tsx',
  },
  {
    id: 'crm-deal-detail',
    domain: 'crm',
    url: '/portal/crm/pipeline/deals/:id',
    requiresData: true,
    seedFn: async (api) => {
      // Lead requires a contact — create one first
      const contact = await api.createContact(testContact());
      const contactId = contact?.id ?? contact?.data?.id;
      if (!contactId) return { cleanup: async () => {} };
      const lead = await api.createLead({ ...testLead(), contactId });
      const leadId = lead?.id ?? lead?.data?.id;
      return {
        cleanup: async () => {
          if (leadId) await api.deleteLead(leadId).catch(() => {});
          await api.deleteContact(contactId).catch(() => {});
        },
        routeParam: leadId,
      };
    },
    waitFor: 'main',
    sourceFile: 'portal-app/crm/features/deal-detail/DealDetailPage.tsx',
  },
  {
    id: 'hr-employee-detail',
    domain: 'hr',
    url: '/portal/hr/employees/:id',
    requiresData: true,
    seedFn: async (api) => {
      const employee = await api.createEmployee(testEmployee());
      const employeeId = employee?.id ?? employee?.data?.id;
      return {
        cleanup: async () => { if (employeeId) await api.deleteEmployee(employeeId).catch(() => {}); },
        routeParam: employeeId,
      };
    },
    waitFor: '[role="tablist"], main',
    tabs: [
      { id: 'overview', param: 'overview' },
      { id: 'documents', param: 'documents' },
      { id: 'timeline', param: 'timeline' },
    ],
    sourceFile: 'portal-app/hr/features/employee-detail/EmployeeDetailPage.tsx',
  },
  {
    id: 'pm-project-detail',
    domain: 'pm',
    url: '/portal/projects/:id',
    requiresData: true,
    seedFn: async (api) => {
      const project = await api.createProject(testProject());
      return { cleanup: () => api.deleteProject(project.id), routeParam: project.id };
    },
    waitFor: 'main',
    tabs: [
      { id: 'board', param: 'board' },
      { id: 'list', param: 'list' },
      { id: 'settings', param: 'settings' },
    ],
    sourceFile: 'portal-app/pm/features/project-detail/PmProjectDetailPage.tsx',
  },
  {
    id: 'order-detail',
    domain: 'ecommerce',
    url: '/portal/ecommerce/orders/:id',
    requiresData: true,
    seedFn: async (api) => {
      // Query existing orders — can't easily create one via API (needs cart + checkout flow)
      const res = await api.request.get(`${process.env.API_URL ?? 'http://localhost:4000'}/api/orders?page=1&pageSize=1`);
      if (res.ok()) {
        const body = await res.json();
        const items = body?.items ?? body?.data ?? [];
        if (items.length > 0) {
          return { cleanup: async () => {}, routeParam: items[0].id };
        }
      }
      // No orders exist — test will show empty/404 (still valid audit target)
      return { cleanup: async () => {} };
    },
    waitFor: 'main',
    tabs: [
      { id: 'details', param: 'details' },
      { id: 'payments', param: 'payments' },
      { id: 'shipping', param: 'shipping' },
      { id: 'timeline', param: 'timeline' },
    ],
    sourceFile: 'portal-app/orders/features/order-detail/OrderDetailPage.tsx',
  },
  {
    id: 'payment-detail',
    domain: 'ecommerce',
    url: '/portal/ecommerce/payments/:id',
    requiresData: true,
    seedFn: async (api) => {
      // Query existing payments — read-only, depends on orders
      const res = await api.request.get(`${process.env.API_URL ?? 'http://localhost:4000'}/api/payments?page=1&pageSize=1`);
      if (res.ok()) {
        const body = await res.json();
        const items = body?.items ?? body?.data ?? [];
        if (items.length > 0) {
          return { cleanup: async () => {}, routeParam: items[0].id };
        }
      }
      return { cleanup: async () => {} };
    },
    waitFor: 'main',
    tabs: [
      { id: 'overview', param: 'overview' },
      { id: 'transactions', param: 'transactions' },
    ],
    sourceFile: 'portal-app/payments/features/payment-detail/PaymentDetailPage.tsx',
  },
  {
    id: 'task-detail',
    domain: 'pm',
    url: '/portal/tasks/:id',
    requiresData: true,
    seedFn: async (api) => {
      const API = process.env.API_URL ?? 'http://localhost:4000';
      // Create project, then create a task in it
      const project = await api.createProject(testProject());
      const projectId = project?.id ?? project?.data?.id;
      if (!projectId) return { cleanup: async () => {} };
      // Create a task via API
      const taskRes = await api.request.post(`${API}/api/pm/tasks`, {
        data: { projectId, title: `Audit Task ${Date.now()}` },
      });
      let taskId: string | undefined;
      if (taskRes.ok()) {
        const taskBody = await taskRes.json();
        taskId = taskBody?.id ?? taskBody?.data?.id;
      }
      return {
        cleanup: async () => { await api.deleteProject(projectId).catch(() => {}); },
        routeParam: taskId,
      };
    },
    waitFor: 'main',
    sourceFile: 'portal-app/pm/features/task-detail/PmTaskDetailPage.tsx',
  },
];

// ─── Platform Admin Pages (5 pages) ─────────────────────────────────────

export const PLATFORM_PAGE_REGISTRY: PageAuditConfig[] = [
  {
    id: 'platform-settings',
    domain: 'admin',
    url: '/portal/admin/platform-settings',
    authProfile: 'platform',
    requiresData: false,
    waitFor: '[role="tablist"]',
    tabs: [
      { id: 'smtp', param: 'smtp' },
      { id: 'emailTemplates', param: 'emailTemplates' },
      { id: 'legalPages', param: 'legalPages' },
      { id: 'modules', param: 'modules' },
    ],
    sourceFile: 'portal-app/settings/features/platform-settings/PlatformSettingsPage.tsx',
  },
  {
    id: 'tenant-settings',
    domain: 'admin',
    url: '/portal/admin/tenant-settings',
    authProfile: 'platform',
    requiresData: false,
    waitFor: '[role="tablist"]',
    tabs: [
      { id: 'branding', param: 'branding' },
      { id: 'contact', param: 'contact' },
      { id: 'regional', param: 'regional' },
      { id: 'paymentGateways', param: 'paymentGateways' },
      { id: 'shippingProviders', param: 'shippingProviders' },
      { id: 'smtp', param: 'smtp' },
      { id: 'emailTemplates', param: 'emailTemplates' },
      { id: 'legalPages', param: 'legalPages' },
      { id: 'modules', param: 'modules' },
      { id: 'webhooks', param: 'webhooks' },
    ],
    sourceFile: 'portal-app/settings/features/tenant-settings/TenantSettingsPage.tsx',
  },
  {
    id: 'tenants',
    domain: 'admin',
    url: '/portal/admin/tenants',
    authProfile: 'platform',
    requiresData: false,
    waitFor: 'table',
    sourceFile: 'portal-app/systems/features/tenants/TenantsPage.tsx',
  },
  {
    id: 'developer-logs',
    domain: 'system',
    url: '/portal/developer-logs',
    authProfile: 'platform',
    requiresData: false,
    waitFor: 'main',
    tabs: [
      { id: 'live', param: 'live' },
      { id: 'stored', param: 'stored' },
    ],
    sourceFile: 'portal-app/systems/features/developer-logs/DeveloperLogsPage.tsx',
  },
];
