# Accessibility Testing Guide

**WCAG 2.1 Level AA Compliance with axe-core**

---

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Configuration](#configuration)
4. [Test Structure](#test-structure)
5. [WCAG Compliance](#wcag-compliance)
6. [Running Tests](#running-tests)
7. [CI/CD Integration](#cicd-integration)
8. [Best Practices](#best-practices)
9. [Common Violations](#common-violations)
10. [Remediation Guide](#remediation-guide)

---

## Overview

The NOIR project uses **axe-core** integrated with **Playwright** to automatically detect accessibility violations and ensure WCAG 2.1 Level AA compliance across the application.

### Coverage Statistics

| Metric | Count |
|--------|-------|
| **Test Spec Files** | 3 files |
| **Test Cases** | 9 tests |
| **Pages Tested** | 5+ (Login, Dashboard, Products, Forms, Settings) |
| **WCAG Standards** | WCAG 2.0 Level A/AA, WCAG 2.1 Level A/AA |
| **Automated Checks** | 90+ axe-core rules |

### What's Tested

- **WCAG 2.1 Level AA** - Full automated audit
- **Keyboard Navigation** - Tab order, focus management, Enter key submission
- **ARIA Labels** - Proper labeling of interactive elements
- **Heading Hierarchy** - Proper h1-h6 structure
- **Form Accessibility** - Labels, error messages, input types
- **Color Contrast** - Text readability (4.5:1 ratio)
- **Screen Reader Compatibility** - Semantic HTML, ARIA roles

---

## Quick Start

```bash
# Navigate to e2e-tests directory
cd src/NOIR.Web/frontend/e2e-tests

# Install dependencies (if not already done)
npm install

# Install axe-core Playwright integration
npm install -D @axe-core/playwright

# Run all accessibility tests
npx playwright test tests/accessibility

# Run with HTML report
npx playwright test tests/accessibility --reporter=html

# Run specific test file
npx playwright test tests/accessibility/a11y-auth.spec.ts
```

---

## Configuration

### Playwright Config

**File:** `playwright.config.ts`

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  // ... other config

  use: {
    // Accessibility testing works better with visible browser
    headless: true,

    // Screenshot on failure helps debugging a11y issues
    screenshot: 'only-on-failure',

    // Trace for a11y debugging
    trace: 'on-first-retry',
  },

  projects: [
    // Run a11y tests on Chromium only (axe-core is browser-agnostic)
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
```

### axe-core Configuration

**Default configuration in tests:**

```typescript
import AxeBuilder from '@axe-core/playwright';

const accessibilityScanResults = await new AxeBuilder({ page })
  .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
  .analyze();
```

**Custom configuration:**

```typescript
// Exclude specific rules (use sparingly!)
const results = await new AxeBuilder({ page })
  .disableRules(['color-contrast']) // Only for known false positives
  .analyze();

// Scan specific region
const results = await new AxeBuilder({ page })
  .include('#main-content')
  .exclude('.third-party-widget')
  .analyze();
```

---

## Test Structure

### Test Organization

```
tests/accessibility/
├── a11y-auth.spec.ts       # Authentication pages (Login, Dashboard)
├── a11y-forms.spec.ts      # Form accessibility (User/Product forms)
└── a11y-products.spec.ts   # Product management pages
```

### Test Patterns

#### 1. Full Page Audit

```typescript
test('A11Y-AUTH-001: Login page has no accessibility violations', async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.navigate();

  // Run axe accessibility scan
  const accessibilityScanResults = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();

  // Should have no violations
  expect(accessibilityScanResults.violations).toEqual([]);
});
```

**What it checks:**
- Missing alt text on images
- Insufficient color contrast
- Missing form labels
- Invalid ARIA attributes
- Improper heading hierarchy
- Keyboard accessibility
- And 80+ more rules

#### 2. ARIA Label Validation

```typescript
test('A11Y-AUTH-003: Login form has proper ARIA labels', async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.navigate();

  // Check email input
  const emailInput = loginPage.emailInput;
  await expect(emailInput).toHaveAttribute('aria-label', /.+/);

  // Check password input
  const passwordInput = loginPage.passwordInput;
  await expect(passwordInput).toHaveAttribute('aria-label', /.+/);

  // Check login button
  const loginButton = loginPage.loginButton;
  await expect(loginButton).toBeEnabled();
});
```

**Why ARIA labels matter:**
- Screen readers announce element purpose
- Improves navigation for visually impaired users
- Required for icon-only buttons
- Critical for form inputs

#### 3. Heading Hierarchy

```typescript
test('A11Y-AUTH-004: Login page has proper heading hierarchy', async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.navigate();

  // Should have h1
  const h1 = page.locator('h1');
  await expect(h1.first()).toBeVisible();

  // Run axe specifically for heading order
  const accessibilityScanResults = await new AxeBuilder({ page })
    .withTags(['wcag2a'])
    .analyze();

  // Filter for heading-order violations
  const headingViolations = accessibilityScanResults.violations.filter(
    v => v.id === 'heading-order'
  );

  expect(headingViolations).toEqual([]);
});
```

**Heading hierarchy rules:**
- Page must have exactly one `<h1>`
- Headings must not skip levels (h1 → h2 → h3, not h1 → h3)
- Headings provide document outline for screen readers

#### 4. Keyboard Navigation

```typescript
test('A11Y-AUTH-005: Login form has proper focus management', async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.navigate();

  // Tab through form - focus should be visible
  await page.keyboard.press('Tab');
  let focusedElement = await page.evaluate(() => document.activeElement?.tagName);
  expect(focusedElement).toBeTruthy();

  await page.keyboard.press('Tab');
  focusedElement = await page.evaluate(() => document.activeElement?.tagName);
  expect(focusedElement).toBeTruthy();

  // Should be able to submit with Enter
  await loginPage.emailInput.fill('admin@noir.local');
  await loginPage.passwordInput.fill('123qwe');
  await page.keyboard.press('Enter');

  // Should redirect (keyboard submission works)
  await page.waitForURL(/\/(portal|dashboard)/, { timeout: 10000 });
});
```

**Keyboard accessibility requirements:**
- All interactive elements must be keyboard accessible
- Focus indicators must be visible
- Tab order must be logical
- Forms must support Enter key submission
- Modals/dialogs must trap focus

#### 5. Form Accessibility

```typescript
test('A11Y-FORM-001: User form has proper labels and error messages', async ({ page }) => {
  await page.goto('/portal/admin/users');
  await page.getByRole('button', { name: /create user/i }).click();

  // Run axe scan on dialog
  const accessibilityScanResults = await new AxeBuilder({ page })
    .include('[role="dialog"]')
    .analyze();

  expect(accessibilityScanResults.violations).toEqual([]);

  // Check form labels
  const emailInput = page.getByLabel(/email/i);
  await expect(emailInput).toBeVisible();

  const firstNameInput = page.getByLabel(/first name/i);
  await expect(firstNameInput).toBeVisible();

  // Trigger validation error
  await emailInput.fill('invalid-email');
  await emailInput.blur();

  // Error message should be associated with input
  const errorMessage = page.locator('[role="alert"]').first();
  await expect(errorMessage).toBeVisible();
});
```

**Form accessibility checklist:**
- ✅ All inputs have associated `<label>` or `aria-label`
- ✅ Error messages use `role="alert"` or `aria-describedby`
- ✅ Required fields marked with `aria-required="true"`
- ✅ Input types are semantic (`type="email"`, `type="tel"`)
- ✅ Fieldsets group related inputs

---

## WCAG Compliance

### WCAG 2.1 Level AA Requirements

#### Perceivable
- **1.1.1 Non-text Content** - Alt text for images
- **1.3.1 Info and Relationships** - Semantic HTML, ARIA roles
- **1.4.3 Contrast (Minimum)** - 4.5:1 for text, 3:1 for large text
- **1.4.11 Non-text Contrast** - 3:1 for UI components

#### Operable
- **2.1.1 Keyboard** - All functionality via keyboard
- **2.4.1 Bypass Blocks** - Skip navigation links
- **2.4.2 Page Titled** - Descriptive page titles
- **2.4.3 Focus Order** - Logical tab order
- **2.4.7 Focus Visible** - Visible focus indicators

#### Understandable
- **3.1.1 Language of Page** - `<html lang="en">`
- **3.2.1 On Focus** - No unexpected context changes
- **3.3.1 Error Identification** - Clear error messages
- **3.3.2 Labels or Instructions** - Form guidance

#### Robust
- **4.1.1 Parsing** - Valid HTML
- **4.1.2 Name, Role, Value** - Proper ARIA usage
- **4.1.3 Status Messages** - Live regions for updates

### axe-core Rule Tags

```typescript
// WCAG 2.0 Level A
.withTags(['wcag2a'])

// WCAG 2.0 Level AA
.withTags(['wcag2aa'])

// WCAG 2.1 Level A
.withTags(['wcag21a'])

// WCAG 2.1 Level AA (recommended)
.withTags(['wcag21aa'])

// All WCAG rules
.withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])

// Best practices (not WCAG)
.withTags(['best-practice'])
```

---

## Running Tests

### Local Development

```bash
# Run all accessibility tests
npx playwright test tests/accessibility

# Run with HTML report
npx playwright test tests/accessibility --reporter=html

# Open HTML report
npx playwright show-report

# Run specific test file
npx playwright test tests/accessibility/a11y-auth.spec.ts

# Run specific test case
npx playwright test tests/accessibility -g "A11Y-AUTH-001"

# Run in headed mode
npx playwright test tests/accessibility --headed

# Run with debug mode
npx playwright test tests/accessibility --debug
```

### Viewing Results

```bash
# HTML report (best for reviewing violations)
npx playwright show-report

# Console output shows:
# - Number of violations
# - Violation details (description, impact, nodes affected)
# - Remediation guidance
```

**Example violation output:**

```
A11Y-AUTH-001: Login page has no accessibility violations
  Expected: []
  Received: [
    {
      id: "color-contrast",
      impact: "serious",
      description: "Ensures text and background colors have sufficient contrast",
      nodes: [
        {
          html: '<button class="text-gray-400">Submit</button>',
          target: [".btn-submit"],
          failureSummary: "Element has insufficient color contrast of 2.1:1 (required 4.5:1)"
        }
      ],
      helpUrl: "https://dequeuniversity.com/rules/axe/4.7/color-contrast"
    }
  ]
```

---

## CI/CD Integration

### GitHub Actions Workflow

**File:** `.github/workflows/accessibility.yml`

```yaml
name: Accessibility Tests

on:
  pull_request:
    branches: [main]
    paths:
      - 'src/NOIR.Web/frontend/src/**'
      - 'src/NOIR.Web/frontend/e2e-tests/**'
  push:
    branches: [main]

jobs:
  accessibility:
    runs-on: ubuntu-latest
    timeout-minutes: 15

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'

      - name: Install dependencies
        working-directory: src/NOIR.Web/frontend
        run: npm ci

      - name: Install Playwright browsers
        working-directory: src/NOIR.Web/frontend/e2e-tests
        run: npx playwright install --with-deps chromium

      - name: Build frontend
        working-directory: src/NOIR.Web/frontend
        run: npm run build

      - name: Run accessibility tests
        working-directory: src/NOIR.Web/frontend/e2e-tests
        run: npx playwright test tests/accessibility --reporter=html

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: accessibility-report
          path: src/NOIR.Web/frontend/e2e-tests/playwright-report/
          retention-days: 30

      - name: Upload test failures
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: accessibility-test-failures
          path: |
            src/NOIR.Web/frontend/e2e-tests/test-results/
            src/NOIR.Web/frontend/e2e-tests/playwright-report/
          retention-days: 7
```

### Required Checks

Accessibility tests are **required to pass** before merging PRs. All new features must maintain WCAG 2.1 Level AA compliance.

---

## Best Practices

### 1. Use Semantic HTML

```html
<!-- GOOD: Semantic elements -->
<button type="button">Submit</button>
<nav aria-label="Main navigation">...</nav>
<main>...</main>
<footer>...</footer>

<!-- BAD: Non-semantic divs -->
<div onclick="submit()">Submit</div>  ❌
<div id="navigation">...</div>        ❌
```

### 2. Provide Text Alternatives

```html
<!-- GOOD: Alt text for images -->
<img src="logo.png" alt="NOIR Logo" />

<!-- GOOD: ARIA label for icon buttons -->
<button aria-label="Delete product">
  <TrashIcon />
</button>

<!-- BAD: Missing alt text -->
<img src="logo.png" />  ❌

<!-- BAD: No label for icon -->
<button><TrashIcon /></button>  ❌
```

### 3. Ensure Keyboard Accessibility

```typescript
// GOOD: All interactive elements accessible via keyboard
<button onClick={handleClick}>Submit</button>
<a href="/products">Products</a>

// BAD: Click handlers on non-interactive elements
<div onClick={handleClick}>Submit</div>  ❌
<span onClick={handleClick}>Click me</span>  ❌
```

### 4. Use ARIA Attributes Correctly

```html
<!-- GOOD: Proper ARIA usage -->
<button aria-label="Close dialog" aria-pressed="false">
<input aria-required="true" aria-invalid="false" aria-describedby="email-error">
<div role="alert" id="email-error">Invalid email format</div>

<!-- BAD: Redundant or incorrect ARIA -->
<button role="button">Submit</button>  <!-- Redundant -->
<div role="img" aria-label="Logo"></div>  <!-- Use <img> instead -->
```

### 5. Maintain Focus Indicators

```css
/* GOOD: Visible focus styles */
button:focus-visible {
  outline: 2px solid blue;
  outline-offset: 2px;
}

/* BAD: Removing focus outline */
button:focus {
  outline: none;  /* ❌ Never do this without alternative */
}
```

### 6. Test with Real Assistive Technology

```bash
# Automated tests catch ~40-60% of accessibility issues
# Manual testing required:

# macOS: VoiceOver (Cmd+F5)
# Windows: NVDA (free screen reader)
# Chrome: ChromeVox extension

# Test checklist:
# - Navigate with keyboard only (no mouse)
# - Use screen reader to navigate entire page
# - Test form submission and error handling
# - Verify focus order makes sense
```

---

## Common Violations

### 1. Missing Alt Text

```html
<!-- Violation -->
<img src="product.jpg" />

<!-- Fix -->
<img src="product.jpg" alt="Blue running shoes" />

<!-- Decorative images -->
<img src="decorative.png" alt="" />  <!-- Empty alt for decoration -->
```

### 2. Insufficient Color Contrast

```css
/* Violation: 2.1:1 contrast (too low) */
.text-gray-400 { color: #9CA3AF; }
.bg-white { background: #FFFFFF; }

/* Fix: 4.5:1 contrast (WCAG AA) */
.text-gray-700 { color: #374151; }
.bg-white { background: #FFFFFF; }
```

### 3. Missing Form Labels

```html
<!-- Violation -->
<input type="email" placeholder="Email" />

<!-- Fix: Visible label -->
<label for="email">Email</label>
<input type="email" id="email" />

<!-- Fix: Hidden label (if design requires) -->
<input type="email" aria-label="Email address" />
```

### 4. Improper Heading Hierarchy

```html
<!-- Violation: Skip level -->
<h1>Products</h1>
<h3>Featured Items</h3>  <!-- ❌ Skipped h2 -->

<!-- Fix -->
<h1>Products</h1>
<h2>Featured Items</h2>  <!-- ✅ Proper hierarchy -->
```

### 5. Non-Keyboard Accessible Elements

```typescript
// Violation
<div onClick={() => handleDelete()}>Delete</div>

// Fix
<button type="button" onClick={() => handleDelete()}>Delete</button>

// Or for custom components
<div
  role="button"
  tabIndex={0}
  onClick={handleDelete}
  onKeyDown={(e) => {
    if (e.key === 'Enter' || e.key === ' ') {
      handleDelete();
    }
  }}
>
  Delete
</div>
```

---

## Remediation Guide

### Step-by-Step Fix Process

1. **Run accessibility test**
   ```bash
   npx playwright test tests/accessibility -g "A11Y-AUTH-001"
   ```

2. **Review violation details**
   - Open HTML report: `npx playwright show-report`
   - Note violation ID (e.g., `color-contrast`)
   - Read description and impact

3. **Check helpUrl for guidance**
   - axe-core provides detailed remediation guides
   - Example: https://dequeuniversity.com/rules/axe/4.7/color-contrast

4. **Fix the issue**
   - Update component code
   - Test fix locally

5. **Re-run tests**
   ```bash
   npx playwright test tests/accessibility
   ```

6. **Verify with manual testing**
   - Use keyboard navigation
   - Test with screen reader

### Quick Fixes Reference

| Violation ID | Quick Fix |
|--------------|-----------|
| `image-alt` | Add `alt` attribute to images |
| `color-contrast` | Use darker text colors (Gray-700+) |
| `label` | Add `<label>` or `aria-label` to inputs |
| `button-name` | Add text content or `aria-label` to buttons |
| `heading-order` | Fix h1→h2→h3 hierarchy |
| `link-name` | Add text content or `aria-label` to links |
| `aria-required-attr` | Add missing ARIA attributes |
| `aria-valid-attr-value` | Fix invalid ARIA values |

---

## References

- [axe-core Rules Documentation](https://github.com/dequelabs/axe-core/blob/develop/doc/rule-descriptions.md)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [Playwright Accessibility Testing](https://playwright.dev/docs/accessibility-testing)
- [E2E Testing Guide](./E2E-TESTING-GUIDE.md)
- [Visual Regression Testing](./VISUAL-REGRESSION-TESTING.md)

---

**Last Updated:** 2026-02-09
**Version:** 1.0
**Status:** ✅ All 9 tests passing (CI green)
