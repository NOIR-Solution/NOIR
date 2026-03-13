const { chromium } = require('playwright');

const TARGET_URL = 'http://localhost:3000';

// Pages to test for consistent action column widths
const pagesToTest = [
  { path: '/portal/blog/tags', name: 'Blog Tags' },
  { path: '/portal/brands', name: 'Brands' },
  { path: '/portal/users', name: 'Users' },
  { path: '/portal/customers', name: 'Customers' },
  { path: '/portal/orders', name: 'Orders' },
  { path: '/portal/products', name: 'Products' },
];

(async () => {
  const browser = await chromium.launch({ headless: false, slowMo: 50 });
  const page = await browser.newPage();

  // Set viewport to desktop size
  await page.setViewportSize({ width: 1920, height: 1080 });

  const results = [];

  for (const testPage of pagesToTest) {
    console.log(`\n📋 Testing ${testPage.name}...`);

    try {
      await page.goto(`${TARGET_URL}${testPage.path}`, { waitUntil: 'networkidle' });

      // Wait for the table to load
      await page.waitForSelector('table', { timeout: 10000 });

      // Wait a bit for any dynamic content
      await page.waitForTimeout(1000);

      // Get the first actions column cell width
      const actionsCell = await page.locator('td:first-child, th:first-child').first();
      const width = await actionsCell.evaluate(el => {
        const rect = el.getBoundingClientRect();
        return {
          width: rect.width,
          computedWidth: window.getComputedStyle(el).width,
          minWidth: window.getComputedStyle(el).minWidth,
          maxWidth: window.getComputedStyle(el).maxWidth,
        };
      });

      // Get all visible column widths for comparison
      const columnWidths = await page.evaluate(() => {
        const table = document.querySelector('table');
        if (!table) return [];

        const headerCells = table.querySelectorAll('th');
        return Array.from(headerCells).map((th, index) => {
          const rect = th.getBoundingClientRect();
          return {
            index,
            width: rect.width,
            text: th.textContent?.trim().substring(0, 20) || 'empty',
          };
        });
      });

      results.push({
        page: testPage.name,
        path: testPage.path,
        actionsWidth: width.width,
        computedWidth: width.computedWidth,
        minWidth: width.minWidth,
        maxWidth: width.maxWidth,
        allColumns: columnWidths,
      });

      console.log(`  ✅ Actions column width: ${width.width}px (computed: ${width.computedWidth})`);
      console.log(`  📊 All columns:`, columnWidths.map(c => `${c.text}=${c.width.toFixed(1)}px`).join(', '));

    } catch (error) {
      console.error(`  ❌ Error on ${testPage.name}:`, error.message);
      results.push({
        page: testPage.name,
        path: testPage.path,
        error: error.message,
      });
    }
  }

  console.log('\n\n========== SUMMARY ==========');
  console.log('\nAction Column Widths (first column should be ~44px):');

  results.forEach(r => {
    if (r.error) {
      console.log(`  ❌ ${r.page}: ERROR - ${r.error}`);
    } else {
      const status = r.actionsWidth >= 40 && r.actionsWidth <= 50 ? '✅' : '⚠️';
      console.log(`  ${status} ${r.page}: ${r.actionsWidth}px`);
    }
  });

  // Check consistency
  const widths = results.filter(r => !r.error).map(r => r.actionsWidth);
  const minWidth = Math.min(...widths);
  const maxWidth = Math.max(...widths);
  const isConsistent = maxWidth - minWidth <= 10; // Allow 10px variance

  console.log(`\nConsistency Check:`);
  console.log(`  Range: ${minWidth}px - ${maxWidth}px`);
  console.log(`  ${isConsistent ? '✅ PASS' : '❌ FAIL'}: Columns are ${isConsistent ? 'consistent' : 'inconsistent'}`);

  await browser.close();
})();
