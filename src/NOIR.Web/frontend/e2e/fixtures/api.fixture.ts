import { APIRequestContext } from '@playwright/test';

const API_URL = process.env.API_URL ?? 'http://localhost:4000';

/**
 * API helper for test data setup/cleanup.
 * Uses API calls instead of UI to seed data → 10-50x faster than UI setup.
 *
 * Pattern: create data in beforeAll → run test → cleanup in afterAll
 */
export class ApiHelper {
  constructor(public readonly request: APIRequestContext) {}

  // ─── Auth ────────────────────────────────────────────────
  async login(email: string, password: string) {
    const res = await this.request.post(`${API_URL}/api/auth/login`, {
      data: { email, password },
      headers: { 'X-Tenant': 'default' },
    });
    const body = await res.json();
    return body.token as string;
  }

  // ─── Products ────────────────────────────────────────────
  async createProduct(data: {
    name: string;
    sku?: string;
    price?: number;
    status?: string;
  }) {
    // Generate slug from name (lowercase, hyphens)
    const slug = data.name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
    const res = await this.request.post(`${API_URL}/api/products`, {
      data: {
        name: data.name,
        slug,
        sku: data.sku,
        basePrice: data.price ?? 100_000,
        status: data.status ?? 'Draft',
        currency: 'VND', // Required by backend validator (ISO 4217, length 3)
      },
    });
    const text = await res.text();
    const parsed = text ? JSON.parse(text) : {};
    // Return with consistent price field for tests that use product.price
    return { price: data.price ?? 100_000, name: data.name, sku: data.sku, ...parsed };
  }

  async deleteProduct(id: string) {
    await this.request.delete(`${API_URL}/api/products/${id}`);
  }

  // ─── Categories ──────────────────────────────────────────
  async createCategory(data: { name: string; parentId?: string }) {
    // Generate slug from name (lowercase, hyphens only)
    const slug = data.name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
    const res = await this.request.post(`${API_URL}/api/products/categories`, {
      data: { ...data, slug },
    });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteCategory(id: string) {
    await this.request.delete(`${API_URL}/api/products/categories/${id}`);
  }

  // ─── Customers ───────────────────────────────────────────
  async createCustomer(data: {
    firstName: string;
    lastName: string;
    email: string;
  }) {
    const res = await this.request.post(`${API_URL}/api/customers`, {
      data,
    });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteCustomer(id: string) {
    await this.request.delete(`${API_URL}/api/customers/${id}`);
  }

  // ─── Orders ──────────────────────────────────────────────
  async getOrder(id: string) {
    const res = await this.request.get(`${API_URL}/api/orders/${id}`);
    return res.json();
  }

  // ─── CRM ─────────────────────────────────────────────────
  async createContact(data: {
    firstName: string;
    lastName: string;
    email: string;
  }) {
    const res = await this.request.post(`${API_URL}/api/crm/contacts`, {
      data,
    });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteContact(id: string) {
    await this.request.delete(`${API_URL}/api/crm/contacts/${id}`);
  }

  async createLead(data: {
    title: string;
    contactId?: string;
    pipelineId?: string;
    value?: number;
  }) {
    const res = await this.request.post(`${API_URL}/api/crm/leads`, {
      data: { value: 1_000_000, ...data },
    });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  // ─── HR ──────────────────────────────────────────────────
  async createEmployee(data: {
    firstName: string;
    lastName: string;
    email: string;
    departmentId?: string;
    joinDate?: string;
    managerId?: string;
  }) {
    const now = new Date().toISOString().split('T')[0]; // YYYY-MM-DD
    const res = await this.request.post(`${API_URL}/api/hr/employees`, {
      data: {
        joinDate: now, // Required field — default to today
        ...data,
      },
    });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteEmployee(id: string) {
    await this.request.delete(`${API_URL}/api/hr/employees/${id}`);
  }

  async createDepartment(data: { name: string; code?: string; parentDepartmentId?: string }) {
    const res = await this.request.post(`${API_URL}/api/hr/departments`, { data });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteDepartment(id: string) {
    await this.request.delete(`${API_URL}/api/hr/departments/${id}`);
  }

  async deleteLead(id: string) {
    await this.request.delete(`${API_URL}/api/crm/leads/${id}`);
  }

  // ─── Blog ──────────────────────────────────────────────
  async createBlogPost(data: {
    title: string;
    body?: string;
    status?: string;
  }) {
    const res = await this.request.post(`${API_URL}/api/blog/posts`, {
      data: { body: '<p>E2E test content</p>', status: 'Draft', ...data },
    });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteBlogPost(id: string) {
    await this.request.delete(`${API_URL}/api/blog/posts/${id}`);
  }

  async createBlogCategory(data: { name: string }) {
    const res = await this.request.post(`${API_URL}/api/blog/categories`, { data });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteBlogCategory(id: string) {
    await this.request.delete(`${API_URL}/api/blog/categories/${id}`);
  }

  async createBlogTag(data: { name: string }) {
    const res = await this.request.post(`${API_URL}/api/blog/tags`, { data });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteBlogTag(id: string) {
    await this.request.delete(`${API_URL}/api/blog/tags/${id}`);
  }

  // ─── Inventory ─────────────────────────────────────────
  async createInventoryReceipt(data: {
    type: string;
    items?: Array<{ productVariantId: string; quantity: number }>;
  }) {
    const res = await this.request.post(`${API_URL}/api/inventory/receipts`, { data });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteInventoryReceipt(id: string) {
    await this.request.delete(`${API_URL}/api/inventory/receipts/${id}`);
  }

  // ─── Project Management ────────────────────────────────
  async createProject(data: { name: string; description?: string }) {
    const res = await this.request.post(`${API_URL}/api/pm/projects`, { data });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteProject(id: string) {
    await this.request.delete(`${API_URL}/api/pm/projects/${id}`);
  }

  // ─── Brands ────────────────────────────────────────────
  async createBrand(data: { name: string }) {
    const res = await this.request.post(`${API_URL}/api/brands`, { data });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteBrand(id: string) {
    await this.request.delete(`${API_URL}/api/brands/${id}`);
  }

  // ─── Users & Roles ────────────────────────────────────
  async createUser(data: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    roleId?: string;
  }) {
    const res = await this.request.post(`${API_URL}/api/users`, { data });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteUser(id: string) {
    await this.request.delete(`${API_URL}/api/users/${id}`);
  }

  async createRole(data: { name: string; description?: string; permissions?: string[] }) {
    const res = await this.request.post(`${API_URL}/api/roles`, { data });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteRole(id: string) {
    await this.request.delete(`${API_URL}/api/roles/${id}`);
  }

  // ─── Promotions ──────────────────────────────────────────
  async createPromotion(data: {
    name: string;
    code: string;
    discountType?: string;
    discountValue?: number;
    startDate?: string;
    endDate?: string;
  }) {
    const now = new Date();
    const endDate = new Date(now.getTime() + 30 * 24 * 60 * 60 * 1000); // 30 days from now
    const res = await this.request.post(`${API_URL}/api/promotions`, {
      data: {
        discountType: 'Percentage',
        discountValue: 10,
        startDate: now.toISOString(),
        endDate: endDate.toISOString(),
        usageLimit: 100,
        isActive: true,
        ...data,
      },
    });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deletePromotion(id: string) {
    await this.request.delete(`${API_URL}/api/promotions/${id}`);
  }

  // ─── Reviews ────────────────────────────────────────────
  async createReview(data: {
    productId: string;
    rating: number;
    title?: string;
    comment?: string;
  }) {
    const res = await this.request.post(`${API_URL}/api/products/${data.productId}/reviews`, {
      data: {
        rating: data.rating ?? 4,
        title: data.title ?? 'E2E Test Review',
        content: data.comment ?? 'E2E test review content',
      },
    });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteReview(id: string) {
    await this.request.delete(`${API_URL}/api/reviews/${id}`);
  }

  // ─── Customer Groups ──────────────────────────────────────
  async createCustomerGroup(data: { name: string; description?: string }) {
    const res = await this.request.post(`${API_URL}/api/customer-groups`, { data });
    const text = await res.text();
    return text ? JSON.parse(text) : {};
  }

  async deleteCustomerGroup(id: string) {
    await this.request.delete(`${API_URL}/api/customer-groups/${id}`);
  }

  // ─── Generic cleanup ────────────────────────────────────
  async deleteEntity(endpoint: string, id: string) {
    await this.request.delete(`${API_URL}/api/${endpoint}/${id}`);
  }
}
