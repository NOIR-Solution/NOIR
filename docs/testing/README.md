# Testing Documentation

**Comprehensive Testing Infrastructure for NOIR Project**

---

## Quick Navigation

### 🎯 Start Here

| Document | Purpose | When to Use |
|----------|---------|-------------|
| **[TESTING-INFRASTRUCTURE-SUMMARY.md](./TESTING-INFRASTRUCTURE-SUMMARY.md)** | **Complete testing overview** | Understanding the entire testing strategy |
| [TEST_PLAN.md](./TEST_PLAN.md) | Test strategy and roadmap | Planning new tests or understanding scope |

### 📚 Implementation Guides

| Document | Purpose | When to Use |
|----------|---------|-------------|
| [E2E-TESTING-GUIDE.md](./E2E-TESTING-GUIDE.md) | Playwright E2E testing | Writing functional tests, setting up Page Objects |
| [VISUAL-REGRESSION-TESTING.md](./VISUAL-REGRESSION-TESTING.md) | Screenshot comparison tests | Detecting UI changes, validating responsive design |
| [ACCESSIBILITY-TESTING.md](./ACCESSIBILITY-TESTING.md) | WCAG compliance testing | Ensuring a11y compliance, fixing violations |

### 📋 Reference

| Document | Purpose | When to Use |
|----------|---------|-------------|
| [TEST_CASES.md](./TEST_CASES.md) | Test case catalog | Finding existing tests, planning coverage |

---

## Testing at a Glance

### Backend Testing (6,750+ tests)

```bash
# Run all backend tests
cd /d/GIT/TOP/NOIR
dotnet test src/NOIR.sln
```

**Coverage:**
- 842 domain tests
- 5,231 application tests
- 654 integration tests
- 25 architecture tests

**Execution Time:** ~2 minutes

### Frontend E2E Testing (~490 tests)

```bash
# Run all E2E tests
cd src/NOIR.Web/frontend/e2e-tests
npm test
```

**Coverage:**
- Authentication flows
- E-commerce features (products, categories, cart, checkout)
- Admin functionality (users, roles, tenants)
- Content management (blog posts, legal pages)
- System features (notifications, command palette)

**Execution Time:** ~12 minutes

### Visual Regression Testing (15 tests)

```bash
# Run visual tests
cd src/NOIR.Web/frontend/e2e-tests
npx playwright test tests/visual
```

**Coverage:**
- Full page layouts
- Component isolation
- Responsive design (Mobile, Tablet, Desktop)
- Theme variants (Light, Dark)

**Baseline Size:** 505KB (16 snapshots)

**Execution Time:** ~60 seconds

### Accessibility Testing (9 tests)

```bash
# Run accessibility tests
cd src/NOIR.Web/frontend/e2e-tests
npx playwright test tests/accessibility
```

**Coverage:**
- WCAG 2.1 Level AA compliance
- Keyboard navigation
- ARIA labels and roles
- Color contrast
- Form accessibility

**Execution Time:** ~30 seconds

---

## Test Categories

### P0 - Critical (Smoke Tests)

**Purpose:** Validate core functionality before deployment

**Coverage:**
- User authentication
- Product creation
- Order placement
- Payment processing
- User management

**Run Frequency:** Every commit

```bash
npm run test:smoke
```

### P1 - High Priority

**Purpose:** Essential features that must work

**Coverage:**
- Advanced product features
- Admin operations
- Content management
- Notification system

**Run Frequency:** Every PR

### P2 - Medium Priority

**Purpose:** Important but not blocking

**Coverage:**
- Edge cases
- Less common workflows
- Analytics
- Reporting

**Run Frequency:** Daily/Weekly

---

## Common Tasks

### Writing a New E2E Test

1. **Create test file**
   ```bash
   touch tests/my-feature.spec.ts
   ```

2. **Follow structure**
   ```typescript
   import { test, expect } from '@playwright/test';
   import { MyFeaturePage } from '../pages/my-feature.page';

   test.describe('My Feature', () => {
     test('@p1 should do something', async ({ page }) => {
       const myPage = new MyFeaturePage(page);
       await myPage.navigate();
       // Test logic
     });
   });
   ```

3. **Run test**
   ```bash
   npx playwright test tests/my-feature.spec.ts
   ```

4. **See guide:** [E2E-TESTING-GUIDE.md](./E2E-TESTING-GUIDE.md)

### Updating Visual Baselines

```bash
# After intentional UI changes
cd src/NOIR.Web/frontend/e2e-tests
npx playwright test tests/visual --update-snapshots
```

**See guide:** [VISUAL-REGRESSION-TESTING.md](./VISUAL-REGRESSION-TESTING.md)

### Fixing Accessibility Violations

1. **Run test to identify violations**
   ```bash
   npx playwright test tests/accessibility
   ```

2. **View report**
   ```bash
   npx playwright show-report
   ```

3. **Fix violation** (example)
   ```html
   <!-- Before: Missing alt text -->
   <img src="logo.png" />

   <!-- After: Add alt text -->
   <img src="logo.png" alt="NOIR Logo" />
   ```

4. **Re-run test**
   ```bash
   npx playwright test tests/accessibility
   ```

**See guide:** [ACCESSIBILITY-TESTING.md](./ACCESSIBILITY-TESTING.md)

---

## CI/CD Integration

### GitHub Actions Workflows

| Workflow | Trigger | Duration |
|----------|---------|----------|
| Backend Tests | Every push, PR | ~3 min |
| E2E Tests | Frontend changes | ~15 min |
| Visual Regression | Frontend changes | ~5 min |
| Accessibility | Frontend changes | ~3 min |

### Required Checks

Before merging PRs:
- ✅ All backend tests pass
- ✅ All E2E tests pass
- ✅ No visual regressions (or approved)
- ✅ No accessibility violations

---

## Troubleshooting

### Flaky Tests

**Problem:** Tests pass/fail randomly

**Solution:**
```typescript
// Use explicit waits
await page.waitForLoadState('networkidle');

// Increase timeout
await expect(element).toBeVisible({ timeout: 10000 });
```

### Visual Test Failures

**Problem:** Visual tests fail after minor changes

**Solution:**
```bash
# Review diffs
npx playwright show-report

# Update if intentional
npx playwright test tests/visual --update-snapshots
```

### Accessibility Violations

**Problem:** WCAG violations detected

**Solution:**
1. Check violation details in HTML report
2. Read helpUrl for remediation guidance
3. Fix the issue (add alt text, improve contrast, etc.)
4. Re-run tests

---

## Best Practices

### Backend Testing
- ✅ Test happy path + edge cases + error conditions
- ✅ Mock external dependencies
- ✅ Clean up test data after each test
- ✅ Use FluentAssertions for readable assertions

### Frontend E2E Testing
- ✅ Use Page Object Model
- ✅ Create test data via API (faster)
- ✅ Use data-testid for stable selectors
- ✅ Wait for network idle before assertions

### Visual Testing
- ✅ Disable animations for consistency
- ✅ Use appropriate diff thresholds
- ✅ Review diffs before updating baselines
- ✅ Update baselines only for intentional changes

### Accessibility Testing
- ✅ Run axe-core on every page
- ✅ Test keyboard navigation manually
- ✅ Use semantic HTML
- ✅ Ensure 4.5:1 color contrast
- ✅ Provide text alternatives (alt, aria-label)

---

## Resources

### Internal
- [../DOCUMENTATION_INDEX.md](../DOCUMENTATION_INDEX.md) - Main documentation hub
- [../KNOWLEDGE_BASE.md](../KNOWLEDGE_BASE.md) - Codebase reference
- [../frontend/architecture.md](../frontend/architecture.md) - Frontend patterns

### External
- [Playwright Documentation](https://playwright.dev/docs/intro)
- [xUnit Documentation](https://xunit.net/)
- [axe-core Rules](https://github.com/dequelabs/axe-core/blob/develop/doc/rule-descriptions.md)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)

---

## Support

For testing questions:

1. Check this documentation
2. Review test examples in the codebase
3. Consult team in project chat
4. Update documentation with new patterns

---

**Last Updated:** 2026-02-09
**Documents:** 6 files, 4,800+ lines
**Test Coverage:** 6,750+ backend tests, ~514 frontend tests
