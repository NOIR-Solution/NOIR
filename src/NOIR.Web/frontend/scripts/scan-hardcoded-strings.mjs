#!/usr/bin/env node

/**
 * Scan for Hardcoded User-Facing Strings
 *
 * Detects hardcoded strings in React/TypeScript components that should use i18n.
 * Excludes technical strings, imports, and component names.
 *
 * Usage:
 *   node scripts/scan-hardcoded-strings.mjs
 *   node scripts/scan-hardcoded-strings.mjs --fix  # Auto-generate translation keys (not implemented yet)
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const SRC_DIR = path.join(__dirname, '..', 'src');
const EXCLUDE_DIRS = ['node_modules', 'dist', 'build', '.git', 'public/locales'];
const INCLUDE_EXTENSIONS = ['.tsx', '.ts', '.jsx', '.js'];

// Patterns to ignore (technical strings, not user-facing)
const IGNORE_PATTERNS = [
  /^import\s/,                      // import statements
  /^export\s/,                      // export statements
  /^const\s/,                       // const declarations
  /^let\s/,                         // let declarations
  /^var\s/,                         // var declarations
  /^function\s/,                    // function declarations
  /^class\s/,                       // class declarations
  /^interface\s/,                   // interface declarations
  /^type\s/,                        // type declarations
  /^enum\s/,                        // enum declarations
  /className=/,                     // CSS classes
  /^[a-z-]+:[a-z-]+$/,             // CSS properties (e.g., "flex:1")
  /^#[0-9A-Fa-f]{3,8}$/,           // Color codes
  /^\/[^\/]+\//,                    // Regex patterns
  /^\d+(\.\d+)?(px|em|rem|%|vh|vw)$/, // CSS units
  /^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)$/i, // HTTP methods
  /^application\//,                 // MIME types
  /^[A-Z_]+$/,                      // Constants (ALL_CAPS)
  /^[a-z][a-zA-Z0-9]*$/,           // camelCase (likely props/variables)
  /^\.[a-z]/,                       // File extensions
  /^\/api\//,                       // API paths
  /^https?:\/\//,                   // URLs
  /^data:/,                         // Data URLs
  /^aria-/,                         // ARIA attributes (handled separately)
  /^role$/,                         // ARIA role
  /^id$/,                           // HTML id
  /^name$/,                         // HTML name
  /^value$/,                        // HTML value
  /^key$/,                          // React key
  /^ref$/,                          // React ref
  /^children$/,                     // React children
  /^\{/,                            // JSX expressions
  /^t\(/,                           // Already using i18n
  /^[0-9]+$/,                       // Pure numbers
  /^true|false$/,                   // Booleans
  /^null|undefined$/,               // Null/undefined
];

// Strings that are likely user-facing
const USER_FACING_INDICATORS = [
  /[A-Z][a-z]+\s+[A-Z][a-z]+/,     // Title Case with spaces
  /^[A-Z][a-z]+$/,                  // Single capitalized word (e.g., "Delete", "Save")
  /\s{2,}/,                         // Multiple spaces (likely sentences)
  /[.!?]$/,                         // Ends with punctuation
  /^(The|A|An)\s/,                  // Starts with article
];

// Known user-facing keywords
const USER_FACING_KEYWORDS = [
  'button', 'label', 'placeholder', 'title', 'description', 'message', 'error', 'warning',
  'success', 'info', 'confirm', 'cancel', 'delete', 'edit', 'create', 'update', 'save',
  'loading', 'empty', 'no results', 'search', 'filter', 'sort', 'page', 'item', 'items'
];

function shouldIgnore(str, context) {
  // Ignore empty strings
  if (!str || str.trim().length === 0) return true;

  // Ignore very short strings (likely technical)
  if (str.length < 3) return true;

  // Check ignore patterns
  if (IGNORE_PATTERNS.some(pattern => pattern.test(str))) return true;

  // Ignore if context suggests it's technical
  if (context.includes('import ') || context.includes('from ')) return true;
  if (context.includes('className=') || context.includes('class=')) return true;

  return false;
}

function isLikelyUserFacing(str) {
  // Check user-facing indicators
  if (USER_FACING_INDICATORS.some(pattern => pattern.test(str))) return true;

  // Check user-facing keywords
  const lowerStr = str.toLowerCase();
  if (USER_FACING_KEYWORDS.some(keyword => lowerStr.includes(keyword))) return true;

  // Strings with spaces and capitals are likely user-facing
  if (/[A-Z]/.test(str) && /\s/.test(str)) return true;

  return false;
}

function scanFile(filePath) {
  const content = fs.readFileSync(filePath, 'utf-8');
  const lines = content.split('\n');
  const findings = [];

  // Regex to find string literals
  const stringRegex = /["'`]([^"'`]+)["'`]/g;

  lines.forEach((line, index) => {
    const lineNumber = index + 1;
    let match;

    while ((match = stringRegex.exec(line)) !== null) {
      const str = match[1];
      const context = line.trim();

      if (shouldIgnore(str, context)) continue;
      if (!isLikelyUserFacing(str)) continue;

      findings.push({
        file: path.relative(SRC_DIR, filePath),
        line: lineNumber,
        string: str,
        context: context.substring(0, 100)
      });
    }
  });

  return findings;
}

function scanDirectory(dir, findings = []) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });

  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name);

    if (entry.isDirectory()) {
      if (!EXCLUDE_DIRS.includes(entry.name)) {
        scanDirectory(fullPath, findings);
      }
    } else if (entry.isFile()) {
      const ext = path.extname(entry.name);
      if (INCLUDE_EXTENSIONS.includes(ext)) {
        const fileFindings = scanFile(fullPath);
        findings.push(...fileFindings);
      }
    }
  }

  return findings;
}

function main() {
  console.log('🔍 Scanning for hardcoded user-facing strings...\n');

  const findings = scanDirectory(SRC_DIR);

  if (findings.length === 0) {
    console.log('✅ No hardcoded strings found!\n');
    return;
  }

  console.log(`⚠️  Found ${findings.length} potential hardcoded strings:\n`);

  // Group by file
  const byFile = {};
  findings.forEach(finding => {
    if (!byFile[finding.file]) byFile[finding.file] = [];
    byFile[finding.file].push(finding);
  });

  // Print findings
  Object.entries(byFile).forEach(([file, fileFindings]) => {
    console.log(`📄 ${file} (${fileFindings.length} findings)`);
    fileFindings.forEach(finding => {
      console.log(`   Line ${finding.line}: "${finding.string}"`);
      console.log(`   Context: ${finding.context}`);
      console.log('');
    });
  });

  // Summary
  console.log('─'.repeat(80));
  console.log(`\nTotal: ${findings.length} hardcoded strings across ${Object.keys(byFile).length} files\n`);
  console.log('💡 Next steps:');
  console.log('   1. Review the findings above');
  console.log('   2. Add translation keys to public/locales/en/*.json and public/locales/vi/*.json');
  console.log('   3. Replace hardcoded strings with t("namespace.key")');
  console.log('   4. See .claude/rules/localization-check.md for guidelines\n');
}

main();
