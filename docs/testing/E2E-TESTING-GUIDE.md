# NOIR E2E Testing Guide

**Playwright Setup & Implementation Guide**

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Project Setup](#project-setup)
3. [Configuration](#configuration)
4. [Page Object Model](#page-object-model)
5. [Writing Tests](#writing-tests)
6. [Authentication](#authentication)
7. [Test Data Management](#test-data-management)
8. [Running Tests](#running-tests)
9. [Debugging](#debugging)
10. [CI/CD Integration](#cicd-integration)
11. [Best Practices](#best-practices)

---

## Quick Start

```bash
# Navigate to e2e-tests directory
cd e2e-tests

# Install dependencies
npm install

# Install browsers
npx playwright install

# Run all tests
npm test

# Run with UI mode (recommended for development)
npm run test:ui

# Run smoke tests only
npm run test:smoke
```

---

## Project Setup

### 1. Initialize Project

```bash
# Create e2e-tests directory at project root
mkdir e2e-tests
cd e2e-tests

# Initialize npm project
npm init -y

# Install Playwright
npm install -D @playwright/test

# Install additional dependencies
npm install -D typescript @types/node dotenv
```

### 2. Directory Structure

```
e2e-tests/
├── playwright.config.ts          # Main configuration
├── package.json                  # Dependencies & scripts
├── tsconfig.json                 # TypeScript config
├── .env                          # Environment variables (git-ignored)
├── .env.example                  # Environment template
│
├── fixtures/                     # Test fixtures
│   ├── auth.fixture.ts          # Authentication state
│   ├── api.fixture.ts           # API helpers
│   └── base.fixture.ts          # Extended test fixture
│
├── pages/                        # Page Object Model
│   ├── base.page.ts             # Base page class
│   ├── login.page.ts
│   ├── dashboard.page.ts
│   └── [feature]/
│       ├── [feature]-list.page.ts
│       └── [feature]-form.page.ts
│
├── tests/                        # Test specifications
│   ├── smoke/                   # P0 Critical paths
│   ├── auth/                    # Authentication tests
│   ├── products/                # Product tests
│   └── [feature]/               # Feature tests
│
├── utils/                        # Utilities
│   ├── test-data.ts             # Data generators
│   ├── api-helpers.ts           # API client
│   └── assertions.ts            # Custom assertions
│
├── playwright/                   # Playwright artifacts
│   └── .auth/                   # Stored auth states
│
└── reports/                      # Test reports
```

### 3. TypeScript Configuration

**tsconfig.json:**
```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "commonjs",
    "moduleResolution": "node",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true,
    "outDir": "./dist",
    "rootDir": "./",
    "resolveJsonModule": true,
    "declaration": true,
    "paths": {
      "@pages/*": ["./pages/*"],
      "@utils/*": ["./utils/*"],
      "@fixtures/*": ["./fixtures/*"]
    }
  },
  "include": ["**/*.ts"],
  "exclude": ["node_modules", "dist"]
}
```

### 4. Package Scripts

**package.json:**
```json
{
  "name": "noir-e2e-tests",
  "version": "1.0.0",
  "scripts": {
    "test": "playwright test",
    "test:smoke": "playwright test --grep @smoke",
    "test:p0": "playwright test --grep @p0",
    "test:p1": "playwright test --grep @p1",
    "test:auth": "playwright test tests/auth/",
    "test:products": "playwright test tests/products/",
    "test:users": "playwright test tests/users/",
    "test:ecommerce": "playwright test tests/ecommerce/",
    "test:chromium": "playwright test --project=chromium",
    "test:firefox": "playwright test --project=firefox",
    "test:webkit": "playwright test --project=webkit",
    "test:mobile": "playwright test --project=mobile-chrome",
    "test:headed": "playwright test --headed",
    "test:debug": "playwright test --debug",
    "test:ui": "playwright test --ui",
    "report": "playwright show-report reports/html",
    "codegen": "playwright codegen http://localhost:3000",
    "codegen:auth": "playwright codegen --save-storage=playwright/.auth/admin.json http://localhost:3000"
  },
  "devDependencies": {
    "@playwright/test": "^1.50.0",
    "@types/node": "^20.0.0",
    "dotenv": "^16.0.0",
    "typescript": "^5.0.0"
  }
}
```

---

## Configuration

### playwright.config.ts

```typescript
import { defineConfig, devices } from '@playwright/test';
import dotenv from 'dotenv';

dotenv.config();

export default defineConfig({
  // Test directory
  testDir: './tests',

  // Test file patterns
  testMatch: '**/*.spec.ts',

  // Run tests in parallel
  fullyParallel: true,

  // Fail the build on CI if accidentally left test.only
  forbidOnly: !!process.env.CI,

  // Retry on CI only
  retries: process.env.CI ? 2 : 0,

  // Limit parallel workers on CI
  workers: process.env.CI ? 2 : undefined,

  // Reporter configuration
  reporter: [
    ['html', { outputFolder: 'reports/html', open: 'never' }],
    ['json', { outputFile: 'reports/results.json' }],
    ['junit', { outputFile: 'reports/junit.xml' }],
    ['list'],
  ],

  // Shared settings for all projects
  use: {
    // Base URL
    baseURL: process.env.BASE_URL || 'http://localhost:3000',

    // API URL for direct API calls
    extraHTTPHeaders: {
      'Accept': 'application/json',
    },

    // Collect trace on first retry
    trace: 'on-first-retry',

    // Screenshot on failure
    screenshot: 'only-on-failure',

    // Video on failure (CI only)
    video: process.env.CI ? 'retain-on-failure' : 'off',

    // Timeout settings
    actionTimeout: 15000,
    navigationTimeout: 30000,
  },

  // Global timeout
  timeout: 60000,

  // Expect timeout
  expect: {
    timeout: 10000,
  },

  // Projects for different browsers
  projects: [
    // Setup project - runs first to authenticate
    {
      name: 'setup',
      testMatch: /.*\.setup\.ts/,
    },

    // Desktop browsers
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'playwright/.auth/admin.json',
      },
      dependencies: ['setup'],
    },
    {
      name: 'firefox',
      use: {
        ...devices['Desktop Firefox'],
        storageState: 'playwright/.auth/admin.json',
      },
      dependencies: ['setup'],
    },
    {
      name: 'webkit',
      use: {
        ...devices['Desktop Safari'],
        storageState: 'playwright/.auth/admin.json',
      },
      dependencies: ['setup'],
    },

    // Mobile browsers
    {
      name: 'mobile-chrome',
      use: {
        ...devices['Pixel 5'],
        storageState: 'playwright/.auth/admin.json',
      },
      dependencies: ['setup'],
    },
    {
      name: 'mobile-safari',
      use: {
        ...devices['iPhone 12'],
        storageState: 'playwright/.auth/admin.json',
      },
      dependencies: ['setup'],
    },

    // Unauthenticated tests (login, password reset)
    {
      name: 'chromium-unauthenticated',
      testMatch: '**/auth/**/*.spec.ts',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Web server configuration (optional - for local dev)
  webServer: process.env.CI ? undefined : {
    command: 'cd .. && ./start-dev.sh',
    url: 'http://localhost:3000',
    reuseExistingServer: true,
    timeout: 120000,
  },
});
```

### Environment Variables

**.env.example:**
```env
# Application URLs
BASE_URL=http://localhost:3000
API_URL=http://localhost:4000

# Test accounts
ADMIN_EMAIL=admin@noir.local
ADMIN_PASSWORD=123qwe
PLATFORM_ADMIN_EMAIL=platform@noir.local
PLATFORM_ADMIN_PASSWORD=123qwe

# CI settings
CI=false
```

---

## Page Object Model

### Base Page Class

**pages/base.page.ts:**
```typescript
import { Page, Locator, expect } from '@playwright/test';

export abstract class BasePage {
  readonly page: Page;

  // Common elements
  readonly loadingSpinner: Locator;
  readonly toast: Locator;
  readonly errorAlert: Locator;

  constructor(page: Page) {
    this.page = page;
    this.loadingSpinner = page.locator('[data-testid="loading-spinner"]');
    this.toast = page.locator('[data-testid="toast"]');
    this.errorAlert = page.locator('[role="alert"]');
  }

  // Abstract method - each page defines its URL
  abstract get url(): string;

  // Navigate to this page
  async goto() {
    await this.page.goto(this.url);
    await this.waitForPageLoad();
  }

  // Wait for page to be fully loaded
  async waitForPageLoad() {
    await this.page.waitForLoadState('networkidle');
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {});
  }

  // Wait for toast message
  async expectToast(message: string | RegExp) {
    await expect(this.toast).toContainText(message);
  }

  // Wait for error alert
  async expectError(message: string | RegExp) {
    await expect(this.errorAlert).toContainText(message);
  }

  // Get current URL path
  getPath(): string {
    return new URL(this.page.url()).pathname;
  }

  // Common actions
  async clickButton(name: string | RegExp) {
    await this.page.getByRole('button', { name }).click();
  }

  async fillInput(label: string, value: string) {
    await this.page.getByLabel(label).fill(value);
  }

  async selectOption(label: string, value: string) {
    await this.page.getByLabel(label).selectOption(value);
  }
}
```

### Login Page

**pages/login.page.ts:**
```typescript
import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class LoginPage extends BasePage {
  // Form elements
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly loginButton: Locator;
  readonly forgotPasswordLink: Locator;
  readonly rememberMeCheckbox: Locator;

  // Error elements
  readonly emailError: Locator;
  readonly passwordError: Locator;
  readonly loginError: Locator;

  constructor(page: Page) {
    super(page);

    this.emailInput = page.getByTestId('email-input');
    this.passwordInput = page.getByTestId('password-input');
    this.loginButton = page.getByTestId('login-button');
    this.forgotPasswordLink = page.getByRole('link', { name: /forgot password/i });
    this.rememberMeCheckbox = page.getByLabel(/remember me/i);

    this.emailError = page.getByTestId('email-error');
    this.passwordError = page.getByTestId('password-error');
    this.loginError = page.getByTestId('login-error');
  }

  get url(): string {
    return '/login';
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();
  }

  async loginAndExpectDashboard(email: string, password: string) {
    await this.login(email, password);
    await this.page.waitForURL('/portal');
    await expect(this.page).toHaveURL('/portal');
  }

  async loginAndExpectError(email: string, password: string) {
    await this.login(email, password);
    await expect(this.loginError).toBeVisible();
  }

  async goToForgotPassword() {
    await this.forgotPasswordLink.click();
    await this.page.waitForURL('/forgot-password');
  }
}
```

### Products List Page

**pages/products/products-list.page.ts:**
```typescript
import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from '../base.page';

export class ProductsListPage extends BasePage {
  // Header elements
  readonly pageTitle: Locator;
  readonly createButton: Locator;

  // Search & Filter
  readonly searchInput: Locator;
  readonly statusFilter: Locator;
  readonly categoryFilter: Locator;
  readonly clearFiltersButton: Locator;

  // Table elements
  readonly productsTable: Locator;
  readonly tableRows: Locator;
  readonly emptyState: Locator;

  // Pagination
  readonly pagination: Locator;
  readonly nextPageButton: Locator;
  readonly prevPageButton: Locator;

  // Bulk actions
  readonly selectAllCheckbox: Locator;
  readonly bulkActionsMenu: Locator;

  constructor(page: Page) {
    super(page);

    this.pageTitle = page.getByRole('heading', { name: /products/i });
    this.createButton = page.getByRole('button', { name: /create product/i });

    this.searchInput = page.getByPlaceholder(/search/i);
    this.statusFilter = page.getByTestId('status-filter');
    this.categoryFilter = page.getByTestId('category-filter');
    this.clearFiltersButton = page.getByRole('button', { name: /clear/i });

    this.productsTable = page.getByRole('table');
    this.tableRows = page.locator('tbody tr');
    this.emptyState = page.getByTestId('empty-state');

    this.pagination = page.getByTestId('pagination');
    this.nextPageButton = page.getByRole('button', { name: /next/i });
    this.prevPageButton = page.getByRole('button', { name: /prev/i });

    this.selectAllCheckbox = page.getByTestId('select-all');
    this.bulkActionsMenu = page.getByTestId('bulk-actions');
  }

  get url(): string {
    return '/portal/ecommerce/products';
  }

  // Actions
  async search(query: string) {
    await this.searchInput.fill(query);
    await this.page.waitForLoadState('networkidle');
  }

  async clearSearch() {
    await this.searchInput.clear();
    await this.page.waitForLoadState('networkidle');
  }

  async filterByStatus(status: 'Active' | 'Draft' | 'Archived' | 'All') {
    await this.statusFilter.click();
    await this.page.getByRole('option', { name: status }).click();
    await this.page.waitForLoadState('networkidle');
  }

  async filterByCategory(categoryName: string) {
    await this.categoryFilter.click();
    await this.page.getByRole('option', { name: categoryName }).click();
    await this.page.waitForLoadState('networkidle');
  }

  async goToCreateProduct() {
    await this.createButton.click();
    await this.page.waitForURL(/\/products\/new/);
  }

  async clickProduct(name: string) {
    await this.tableRows.filter({ hasText: name }).click();
  }

  async openProductActions(name: string) {
    const row = this.tableRows.filter({ hasText: name });
    await row.getByTestId('actions-menu').click();
  }

  async deleteProduct(name: string) {
    await this.openProductActions(name);
    await this.page.getByRole('menuitem', { name: /delete/i }).click();
    await this.page.getByRole('button', { name: /confirm/i }).click();
  }

  // Assertions
  async getProductCount(): Promise<number> {
    await this.waitForPageLoad();
    const count = await this.tableRows.count();
    return count;
  }

  async expectProductVisible(name: string) {
    await expect(this.tableRows.filter({ hasText: name })).toBeVisible();
  }

  async expectProductNotVisible(name: string) {
    await expect(this.tableRows.filter({ hasText: name })).not.toBeVisible();
  }

  async expectEmptyState() {
    await expect(this.emptyState).toBeVisible();
  }
}
```

### Product Form Page

**pages/products/product-form.page.ts:**
```typescript
import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from '../base.page';

export interface ProductFormData {
  name?: string;
  sku?: string;
  price?: number;
  comparePrice?: number;
  description?: string;
  category?: string;
  stock?: number;
}

export class ProductFormPage extends BasePage {
  // Basic info
  readonly nameInput: Locator;
  readonly skuInput: Locator;
  readonly priceInput: Locator;
  readonly comparePriceInput: Locator;
  readonly descriptionEditor: Locator;
  readonly categorySelect: Locator;

  // Inventory
  readonly stockInput: Locator;
  readonly trackInventoryCheckbox: Locator;

  // Images
  readonly imageUploadZone: Locator;
  readonly imageGallery: Locator;
  readonly imageThumbnails: Locator;

  // Variants
  readonly addOptionButton: Locator;
  readonly generateVariantsButton: Locator;
  readonly variantsTable: Locator;

  // Attributes
  readonly attributesSection: Locator;

  // Actions
  readonly saveButton: Locator;
  readonly saveDraftButton: Locator;
  readonly publishButton: Locator;
  readonly cancelButton: Locator;

  // Errors
  readonly nameError: Locator;
  readonly skuError: Locator;
  readonly priceError: Locator;

  constructor(page: Page) {
    super(page);

    this.nameInput = page.getByLabel(/^name/i);
    this.skuInput = page.getByLabel(/sku/i);
    this.priceInput = page.getByLabel(/^price/i);
    this.comparePriceInput = page.getByLabel(/compare.*price/i);
    this.descriptionEditor = page.locator('.ProseMirror');
    this.categorySelect = page.getByLabel(/category/i);

    this.stockInput = page.getByLabel(/stock/i);
    this.trackInventoryCheckbox = page.getByLabel(/track inventory/i);

    this.imageUploadZone = page.getByTestId('image-upload-zone');
    this.imageGallery = page.getByTestId('image-gallery');
    this.imageThumbnails = page.locator('[data-testid="image-thumbnail"]');

    this.addOptionButton = page.getByRole('button', { name: /add option/i });
    this.generateVariantsButton = page.getByRole('button', { name: /generate variants/i });
    this.variantsTable = page.getByTestId('variants-table');

    this.attributesSection = page.getByTestId('attributes-section');

    this.saveButton = page.getByRole('button', { name: /^save$/i });
    this.saveDraftButton = page.getByRole('button', { name: /save.*draft/i });
    this.publishButton = page.getByRole('button', { name: /publish/i });
    this.cancelButton = page.getByRole('button', { name: /cancel/i });

    this.nameError = page.getByTestId('name-error');
    this.skuError = page.getByTestId('sku-error');
    this.priceError = page.getByTestId('price-error');
  }

  get url(): string {
    return '/portal/ecommerce/products/new';
  }

  async gotoEdit(productId: string) {
    await this.page.goto(`/portal/ecommerce/products/${productId}`);
    await this.waitForPageLoad();
  }

  // Fill form
  async fillBasicInfo(data: ProductFormData) {
    if (data.name !== undefined) {
      await this.nameInput.fill(data.name);
    }
    if (data.sku !== undefined) {
      await this.skuInput.fill(data.sku);
    }
    if (data.price !== undefined) {
      await this.priceInput.fill(data.price.toString());
    }
    if (data.comparePrice !== undefined) {
      await this.comparePriceInput.fill(data.comparePrice.toString());
    }
    if (data.description !== undefined) {
      await this.descriptionEditor.fill(data.description);
    }
    if (data.category !== undefined) {
      await this.categorySelect.click();
      await this.page.getByRole('option', { name: data.category }).click();
    }
    if (data.stock !== undefined) {
      await this.stockInput.fill(data.stock.toString());
    }
  }

  // Images
  async uploadImage(filePath: string) {
    const fileInput = this.page.locator('input[type="file"]');
    await fileInput.setInputFiles(filePath);
    await this.page.waitForLoadState('networkidle');
  }

  async uploadImages(filePaths: string[]) {
    const fileInput = this.page.locator('input[type="file"]');
    await fileInput.setInputFiles(filePaths);
    await this.page.waitForLoadState('networkidle');
  }

  async setImageAsPrimary(index: number) {
    const thumbnail = this.imageThumbnails.nth(index);
    await thumbnail.hover();
    await thumbnail.getByRole('button', { name: /primary/i }).click();
  }

  // Variants
  async addOption(name: string, values: string[]) {
    await this.addOptionButton.click();
    await this.page.getByPlaceholder(/option name/i).last().fill(name);

    for (const value of values) {
      await this.page.getByPlaceholder(/add value/i).last().fill(value);
      await this.page.keyboard.press('Enter');
    }
  }

  async generateVariants() {
    await this.generateVariantsButton.click();
    await this.page.waitForLoadState('networkidle');
  }

  // Actions
  async save() {
    await this.saveButton.click();
    await this.waitForPageLoad();
  }

  async saveDraft() {
    await this.saveDraftButton.click();
    await this.waitForPageLoad();
  }

  async publish() {
    await this.publishButton.click();
    await this.waitForPageLoad();
  }

  async cancel() {
    await this.cancelButton.click();
  }

  // Assertions
  async expectValidationErrors() {
    await expect(
      this.nameError.or(this.skuError).or(this.priceError)
    ).toBeVisible();
  }
}
```

---

## Writing Tests

### Test Structure

**tests/products/product-crud.spec.ts:**
```typescript
import { test, expect } from '@playwright/test';
import { ProductsListPage } from '../../pages/products/products-list.page';
import { ProductFormPage } from '../../pages/products/product-form.page';
import { generateTestProduct } from '../../utils/test-data';
import { createProductViaApi, deleteProductViaApi } from '../../utils/api-helpers';

test.describe('Product CRUD Operations', () => {
  let productsPage: ProductsListPage;
  let productForm: ProductFormPage;

  test.beforeEach(async ({ page }) => {
    productsPage = new ProductsListPage(page);
    productForm = new ProductFormPage(page);
  });

  test.describe('Create Product', () => {
    test('@p0 @smoke should create a simple product', async ({ page }) => {
      const testProduct = generateTestProduct();

      await productsPage.goto();
      await productsPage.goToCreateProduct();

      await productForm.fillBasicInfo({
        name: testProduct.name,
        sku: testProduct.sku,
        price: testProduct.price,
      });
      await productForm.save();

      // Should redirect to list
      await expect(page).toHaveURL(/\/products$/);

      // Product should be visible
      await productsPage.expectProductVisible(testProduct.name);
    });

    test('@p1 should create product with variants', async ({ page }) => {
      const testProduct = generateTestProduct();

      await productsPage.goto();
      await productsPage.goToCreateProduct();

      await productForm.fillBasicInfo({
        name: testProduct.name,
        sku: testProduct.sku,
        price: testProduct.price,
      });

      // Add size option
      await productForm.addOption('Size', ['S', 'M', 'L']);
      await productForm.generateVariants();

      // Verify 3 variants created
      const variantCount = await productForm.variantsTable.locator('tbody tr').count();
      expect(variantCount).toBe(3);

      await productForm.save();
      await productsPage.expectProductVisible(testProduct.name);
    });

    test('@p1 should show validation errors on empty form', async () => {
      await productsPage.goto();
      await productsPage.goToCreateProduct();

      await productForm.save();

      await productForm.expectValidationErrors();
    });
  });

  test.describe('Read Products', () => {
    test('@p0 should display products list', async () => {
      await productsPage.goto();

      await expect(productsPage.pageTitle).toBeVisible();
      await expect(productsPage.productsTable).toBeVisible();
    });

    test('@p1 should search products by name', async ({ request }) => {
      // Create test product via API
      const testProduct = generateTestProduct();
      const created = await createProductViaApi(request, testProduct);

      try {
        await productsPage.goto();
        await productsPage.search(testProduct.name);

        const count = await productsPage.getProductCount();
        expect(count).toBe(1);
        await productsPage.expectProductVisible(testProduct.name);
      } finally {
        // Cleanup
        await deleteProductViaApi(request, created.id);
      }
    });

    test('@p1 should filter by status', async () => {
      await productsPage.goto();
      await productsPage.filterByStatus('Active');

      // All visible products should have Active status
      const statusBadges = productsPage.tableRows.locator('[data-testid="status-badge"]');
      const count = await statusBadges.count();

      for (let i = 0; i < count; i++) {
        await expect(statusBadges.nth(i)).toHaveText('Active');
      }
    });
  });

  test.describe('Update Product', () => {
    let testProduct: any;
    let createdProduct: any;

    test.beforeEach(async ({ request }) => {
      testProduct = generateTestProduct();
      createdProduct = await createProductViaApi(request, testProduct);
    });

    test.afterEach(async ({ request }) => {
      if (createdProduct) {
        await deleteProductViaApi(request, createdProduct.id);
      }
    });

    test('@p0 should update product name', async ({ page }) => {
      await productForm.gotoEdit(createdProduct.id);

      const newName = 'Updated ' + testProduct.name;
      await productForm.fillBasicInfo({ name: newName });
      await productForm.save();

      await productsPage.expectProductVisible(newName);
    });

    test('@p0 should update product price', async ({ page }) => {
      await productForm.gotoEdit(createdProduct.id);

      const newPrice = 9999;
      await productForm.fillBasicInfo({ price: newPrice });
      await productForm.save();

      // Verify price updated
      await expect(page.getByText('99.99')).toBeVisible();
    });
  });

  test.describe('Delete Product', () => {
    test('@p1 should delete product with confirmation', async ({ request }) => {
      const testProduct = generateTestProduct();
      const created = await createProductViaApi(request, testProduct);

      await productsPage.goto();
      await productsPage.expectProductVisible(testProduct.name);

      await productsPage.deleteProduct(testProduct.name);

      await productsPage.expectToast(/deleted/i);
      await productsPage.expectProductNotVisible(testProduct.name);
    });
  });
});
```

### Tag Conventions

Use tags to categorize tests:

```typescript
// Priority tags
test('@p0 @smoke critical test', ...);
test('@p1 high priority test', ...);
test('@p2 medium priority test', ...);

// Feature tags
test('@auth @login login test', ...);
test('@products @crud create product', ...);
test('@ecommerce @checkout checkout flow', ...);

// Regression tag
test('@regression verify existing functionality', ...);
```

---

## Authentication

### Authentication Setup

**fixtures/auth.setup.ts:**
```typescript
import { test as setup, expect } from '@playwright/test';

const ADMIN_FILE = 'playwright/.auth/admin.json';
const PLATFORM_ADMIN_FILE = 'playwright/.auth/platform-admin.json';

setup('authenticate as tenant admin', async ({ page }) => {
  await page.goto('/login');

  await page.getByTestId('email-input').fill(process.env.ADMIN_EMAIL || 'admin@noir.local');
  await page.getByTestId('password-input').fill(process.env.ADMIN_PASSWORD || '123qwe');
  await page.getByTestId('login-button').click();

  // Wait for dashboard
  await page.waitForURL('/portal');
  await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible();

  // Save storage state
  await page.context().storageState({ path: ADMIN_FILE });
});

setup('authenticate as platform admin', async ({ page }) => {
  await page.goto('/login');

  await page.getByTestId('email-input').fill(process.env.PLATFORM_ADMIN_EMAIL || 'platform@noir.local');
  await page.getByTestId('password-input').fill(process.env.PLATFORM_ADMIN_PASSWORD || '123qwe');
  await page.getByTestId('login-button').click();

  // Wait for dashboard
  await page.waitForURL('/portal');

  // Save storage state
  await page.context().storageState({ path: PLATFORM_ADMIN_FILE });
});
```

### Using Authenticated State

```typescript
// In test file - use default admin auth
test.describe('Admin Features', () => {
  // Uses admin auth from playwright.config.ts

  test('should access admin page', async ({ page }) => {
    await page.goto('/portal/admin/users');
    await expect(page).toHaveURL('/portal/admin/users');
  });
});

// Override for specific tests
test.describe('Platform Admin Features', () => {
  test.use({ storageState: 'playwright/.auth/platform-admin.json' });

  test('should access tenants', async ({ page }) => {
    await page.goto('/portal/admin/tenants');
    await expect(page).toHaveURL('/portal/admin/tenants');
  });
});

// Unauthenticated tests
test.describe('Login Tests', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test('should show login page', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByTestId('login-button')).toBeVisible();
  });
});
```

---

## Test Data Management

### Data Generators

**utils/test-data.ts:**
```typescript
import { randomUUID } from 'crypto';

export function generateTestProduct() {
  const id = randomUUID().slice(0, 8);
  return {
    name: `Test Product ${id}`,
    sku: `SKU-${id}`,
    price: Math.floor(Math.random() * 10000) + 100,
    comparePrice: Math.floor(Math.random() * 15000) + 100,
    description: `Test product description ${id}`,
    stock: Math.floor(Math.random() * 100) + 1,
  };
}

export function generateTestUser() {
  const id = randomUUID().slice(0, 8);
  return {
    email: `test.user.${id}@noir.local`,
    firstName: 'Test',
    lastName: `User ${id}`,
    password: 'Test@123',
  };
}

export function generateTestRole() {
  const id = randomUUID().slice(0, 8);
  return {
    name: `Test Role ${id}`,
    description: `Test role description ${id}`,
  };
}

export function generateTestTenant() {
  const id = randomUUID().slice(0, 8);
  return {
    name: `Test Tenant ${id}`,
    identifier: `tenant-${id}`,
    adminEmail: `admin.${id}@test.local`,
  };
}
```

### API Helpers

**utils/api-helpers.ts:**
```typescript
import { APIRequestContext } from '@playwright/test';

const API_URL = process.env.API_URL || 'http://localhost:4000';

async function getAuthToken(request: APIRequestContext): Promise<string> {
  const response = await request.post(`${API_URL}/api/auth/login`, {
    data: {
      email: process.env.ADMIN_EMAIL || 'admin@noir.local',
      password: process.env.ADMIN_PASSWORD || '123qwe',
    },
  });
  const data = await response.json();
  return data.accessToken;
}

export async function createProductViaApi(
  request: APIRequestContext,
  product: any
): Promise<{ id: string }> {
  const token = await getAuthToken(request);

  const response = await request.post(`${API_URL}/api/products`, {
    headers: { Authorization: `Bearer ${token}` },
    data: product,
  });

  if (!response.ok()) {
    throw new Error(`Failed to create product: ${response.status()}`);
  }

  return await response.json();
}

export async function deleteProductViaApi(
  request: APIRequestContext,
  productId: string
): Promise<void> {
  const token = await getAuthToken(request);

  await request.delete(`${API_URL}/api/products/${productId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function createUserViaApi(
  request: APIRequestContext,
  user: any
): Promise<{ id: string }> {
  const token = await getAuthToken(request);

  const response = await request.post(`${API_URL}/api/users`, {
    headers: { Authorization: `Bearer ${token}` },
    data: user,
  });

  if (!response.ok()) {
    throw new Error(`Failed to create user: ${response.status()}`);
  }

  return await response.json();
}

export async function deleteUserViaApi(
  request: APIRequestContext,
  userId: string
): Promise<void> {
  const token = await getAuthToken(request);

  await request.delete(`${API_URL}/api/users/${userId}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}
```

---

## Running Tests

### Command Reference

```bash
# Run all tests
npm test

# Run specific test file
npx playwright test tests/products/product-crud.spec.ts

# Run tests matching pattern
npx playwright test -g "create product"

# Run by tag
npm run test:smoke          # @smoke tests
npm run test:p0             # @p0 tests
npm run test:p1             # @p1 tests

# Run by feature
npm run test:auth
npm run test:products
npm run test:users
npm run test:ecommerce

# Run specific browser
npm run test:chromium
npm run test:firefox
npm run test:webkit
npm run test:mobile

# Debug modes
npm run test:headed         # See browser
npm run test:debug          # Step through
npm run test:ui             # UI mode

# Generate test code
npm run codegen             # Record actions
```

### Viewing Reports

```bash
# Open HTML report
npm run report

# Report is also available at:
# reports/html/index.html
```

---

## Debugging

### Debug Mode

```bash
# Run with debugger
npx playwright test --debug

# Debug specific test
npx playwright test -g "login" --debug
```

### UI Mode

```bash
# Interactive test runner
npx playwright test --ui
```

### Trace Viewer

```bash
# Run with trace
npx playwright test --trace on

# View trace
npx playwright show-trace trace.zip
```

### Common Debug Techniques

```typescript
// Add pause for debugging
await page.pause();

// Take screenshot
await page.screenshot({ path: 'debug.png' });

// Log element state
console.log(await element.textContent());
console.log(await element.getAttribute('class'));
console.log(await element.isVisible());

// Slow down actions
test.use({ actionTimeout: 30000 });

// Run in headed mode
test.use({ headless: false });
```

---

## CI/CD Integration

### GitHub Actions Workflow

**.github/workflows/e2e-tests.yml:**
```yaml
name: E2E Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 2 * * *'  # Nightly at 2 AM UTC

env:
  CI: true
  BASE_URL: http://localhost:3000
  API_URL: http://localhost:4000

jobs:
  e2e-tests:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_USER: noir
          POSTGRES_PASSWORD: noir
          POSTGRES_DB: noir_test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: |
            e2e-tests/package-lock.json
            src/NOIR.Web/frontend/package-lock.json

      - name: Install E2E dependencies
        working-directory: ./e2e-tests
        run: npm ci

      - name: Install Playwright browsers
        working-directory: ./e2e-tests
        run: npx playwright install --with-deps chromium

      - name: Install frontend dependencies
        working-directory: ./src/NOIR.Web/frontend
        run: npm ci

      - name: Build frontend
        working-directory: ./src/NOIR.Web/frontend
        run: npm run build

      - name: Build backend
        run: dotnet build src/NOIR.sln -c Release

      - name: Run database migrations
        run: |
          dotnet ef database update \
            --project src/NOIR.Infrastructure \
            --startup-project src/NOIR.Web \
            --context TenantStoreDbContext
          dotnet ef database update \
            --project src/NOIR.Infrastructure \
            --startup-project src/NOIR.Web \
            --context ApplicationDbContext
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=noir_test;Username=noir;Password=noir"

      - name: Start backend
        run: |
          dotnet run --project src/NOIR.Web -c Release &
          sleep 30
        env:
          ASPNETCORE_ENVIRONMENT: Testing
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=noir_test;Username=noir;Password=noir"

      - name: Start frontend
        working-directory: ./src/NOIR.Web/frontend
        run: |
          npm run preview &
          sleep 10

      - name: Wait for services
        run: |
          npx wait-on http://localhost:3000 http://localhost:4000/health --timeout 60000

      - name: Run E2E tests
        working-directory: ./e2e-tests
        run: npx playwright test --project=chromium

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: playwright-report
          path: |
            e2e-tests/reports/
            e2e-tests/test-results/
          retention-days: 30

      - name: Upload test screenshots
        uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: test-screenshots
          path: e2e-tests/test-results/**/*.png
          retention-days: 7
```

---

## Best Practices

### 1. Selectors

```typescript
// GOOD: Use data-testid for stable selectors
await page.getByTestId('submit-button').click();

// GOOD: Use role selectors for accessibility
await page.getByRole('button', { name: 'Submit' }).click();

// GOOD: Use label for form fields
await page.getByLabel('Email').fill('test@example.com');

// AVOID: CSS selectors (fragile)
await page.locator('.btn-primary').click();

// AVOID: XPath (hard to read)
await page.locator('//button[@class="submit"]').click();
```

### 2. Waits

```typescript
// GOOD: Wait for specific conditions
await expect(page.getByText('Success')).toBeVisible();
await page.waitForURL('/dashboard');

// GOOD: Wait for network idle
await page.waitForLoadState('networkidle');

// AVOID: Hard-coded waits
await page.waitForTimeout(5000);
```

### 3. Test Isolation

```typescript
// GOOD: Create test data via API (faster)
test.beforeEach(async ({ request }) => {
  testProduct = await createProductViaApi(request, generateTestProduct());
});

test.afterEach(async ({ request }) => {
  await deleteProductViaApi(request, testProduct.id);
});

// AVOID: Creating data through UI in every test
```

### 4. Assertions

```typescript
// GOOD: Use specific assertions
await expect(page.getByTestId('error')).toHaveText('Invalid email');
await expect(page).toHaveURL('/dashboard');
await expect(button).toBeEnabled();

// GOOD: Multiple assertions in sequence
await expect(page.getByText('Product created')).toBeVisible();
await expect(page).toHaveURL(/\/products$/);

// AVOID: Generic assertions
expect(await page.textContent('body')).toContain('error');
```

### 5. Organization

```typescript
// GOOD: Group related tests
test.describe('Product CRUD', () => {
  test.describe('Create', () => {
    test('should create simple product', ...);
    test('should create with variants', ...);
  });

  test.describe('Update', () => {
    test('should update name', ...);
    test('should update price', ...);
  });
});
```

---

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| Tests timeout | Increase timeout in config, check network |
| Element not found | Add wait, check selector, use data-testid |
| Auth state not saved | Check setup project runs first |
| Flaky tests | Use explicit waits, check race conditions |
| Port already in use | Kill existing processes, check webServer config |

### Debug Checklist

1. Run with `--headed` to see what's happening
2. Add `await page.pause()` to stop at problem area
3. Check element visibility with `await element.isVisible()`
4. Take screenshots: `await page.screenshot({ path: 'debug.png' })`
5. Check console logs: `page.on('console', msg => console.log(msg.text()))`
6. Enable trace: `npx playwright test --trace on`

---

## References

- [Playwright Documentation](https://playwright.dev/docs/intro)
- [Test Plan](./TEST_PLAN.md)
- [NOIR Architecture](../ARCHITECTURE.md)
- [Frontend Architecture](../frontend/architecture.md)

---

**Last Updated:** 2026-02-05
