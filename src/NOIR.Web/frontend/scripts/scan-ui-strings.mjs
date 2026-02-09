#!/usr/bin/env node

/**
 * Scan for User-Facing UI Strings
 *
 * Focused scanner that finds actual user-facing strings in JSX.
 * Looks for: JSX text, placeholder, aria-label, title, alt attributes.
 *
 * Usage:
 *   node scripts/scan-ui-strings.mjs
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const SRC_DIR = path.join(__dirname, '..', 'src');
const EXCLUDE_DIRS = ['node_modules', 'dist', 'build', '.git', 'public/locales'];

// Patterns for user-facing strings
const PATTERNS = {
  placeholder: /placeholder=["']([^"']+)["']/g,
  ariaLabel: /aria-label=["']([^"']+)["']/g,
  title: /title=["']([^"']+)["']/g,
  alt: /alt=["']([^"']+)["']/g,
  jsxText: />([A-Z][a-zA-Z\s]{2,})</g,  // JSX text content starting with capital
};

// Ignore if already using translation
const IGNORE_IF_CONTAINS = [
  't(',          // Already using i18n
  'translate(',  // Already using translation
  '{t(',         // JSX expression with translation
  '$t(',         // Template literal translation
];

function shouldIgnore(str, line) {
  // Empty or very short
  if (!str || str.trim().length < 2) return true;

  // Already using translation
  if (IGNORE_IF_CONTAINS.some(pattern => line.includes(pattern))) return true;

  // Technical strings
  if (/^[a-z-]+$/.test(str)) return true;  // kebab-case (CSS)
  if (/^[A-Z_]+$/.test(str)) return true;  // CONSTANT_CASE
  if (/^[a-z][a-zA-Z0-9]*$/.test(str) && str.length < 10) return true;  // camelCase short

  // URLs, paths, etc.
  if (str.startsWith('/') || str.startsWith('http')) return true;
  if (str.includes('://')) return true;

  return false;
}

function scanFile(filePath) {
  const content = fs.readFileSync(filePath, 'utf-8');
  const lines = content.split('\n');
  const findings = [];

  lines.forEach((line, index) => {
    const lineNumber = index + 1;

    // Skip if line already uses translation
    if (IGNORE_IF_CONTAINS.some(pattern => line.includes(pattern))) return;

    // Check each pattern
    Object.entries(PATTERNS).forEach(([type, regex]) => {
      let match;
      const regexCopy = new RegExp(regex.source, regex.flags);

      while ((match = regexCopy.exec(line)) !== null) {
        const str = match[1];

        if (shouldIgnore(str, line)) continue;

        findings.push({
          file: path.relative(SRC_DIR, filePath),
          line: lineNumber,
          type,
          string: str,
          context: line.trim().substring(0, 120)
        });
      }
    });
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
      if (['.tsx', '.jsx'].includes(ext)) {
        const fileFindings = scanFile(fullPath);
        findings.push(...fileFindings);
      }
    }
  }

  return findings;
}

function main() {
  console.log('🔍 Scanning for user-facing UI strings...\n');

  const findings = scanDirectory(SRC_DIR);

  if (findings.length === 0) {
    console.log('✅ No hardcoded UI strings found!\n');
    return;
  }

  console.log(`⚠️  Found ${findings.length} potential hardcoded UI strings:\n`);

  // Group by type
  const byType = {};
  findings.forEach(finding => {
    if (!byType[finding.type]) byType[finding.type] = [];
    byType[finding.type].push(finding);
  });

  // Print summary
  console.log('📊 Summary by type:');
  Object.entries(byType).forEach(([type, typeFindings]) => {
    console.log(`   ${type}: ${typeFindings.length} findings`);
  });
  console.log('');

  // Group by file
  const byFile = {};
  findings.forEach(finding => {
    if (!byFile[finding.file]) byFile[finding.file] = [];
    byFile[finding.file].push(finding);
  });

  // Print top files
  const sortedFiles = Object.entries(byFile)
    .sort((a, b) => b[1].length - a[1].length)
    .slice(0, 20);

  console.log('📄 Top 20 files with most findings:\n');
  sortedFiles.forEach(([file, fileFindings]) => {
    console.log(`   ${file} (${fileFindings.length} findings)`);
    fileFindings.slice(0, 3).forEach(finding => {
      console.log(`      Line ${finding.line} [${finding.type}]: "${finding.string}"`);
    });
    if (fileFindings.length > 3) {
      console.log(`      ... and ${fileFindings.length - 3} more`);
    }
    console.log('');
  });

  console.log('─'.repeat(80));
  console.log(`\nTotal: ${findings.length} hardcoded strings across ${Object.keys(byFile).length} files\n`);
  console.log('💡 Priority fixes:');
  console.log('   1. aria-label attributes (accessibility critical)');
  console.log('   2. placeholder text (user interaction)');
  console.log('   3. JSX text content (visible text)');
  console.log('   4. title and alt attributes (tooltips and images)\n');
}

main();
