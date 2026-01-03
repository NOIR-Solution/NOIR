# NOIR Localization Guide

This guide explains how to manage translations and add new languages to the NOIR system.

## Overview

NOIR uses a JSON-based localization system that supports:
- **English (en)** - Default language
- **Vietnamese (vi)** - Second language

The system automatically detects the user's browser language and falls back to English if not supported.

## Translation File Structure

### Frontend Translations

Translation files are located in:
```
src/NOIR.Web/frontend/public/locales/
├── en/                    # English translations
│   ├── common.json        # Shared UI elements (buttons, labels, messages)
│   ├── auth.json          # Authentication screens
│   ├── errors.json        # Error messages
│   └── nav.json           # Navigation menu
└── vi/                    # Vietnamese translations
    ├── common.json
    ├── auth.json
    ├── errors.json
    └── nav.json
```

### Backend Translations

Backend translations are located in:
```
src/NOIR.Web/Resources/Localization/
├── en/
│   ├── common.json
│   ├── auth.json
│   └── errors.json
└── vi/
    ├── common.json
    ├── auth.json
    └── errors.json
```

## How to Edit Translations

### Step 1: Locate the Translation File

1. Identify which namespace contains your text:
   - `common.json` - Buttons, labels, common messages
   - `auth.json` - Login, registration, password screens
   - `errors.json` - Error and validation messages
   - `nav.json` - Navigation and menu items

2. Open the file for your target language (e.g., `locales/vi/common.json`)

### Step 2: Edit the Translation

JSON files use a key-value structure:

```json
{
  "buttons": {
    "save": "Lưu",
    "cancel": "Hủy"
  }
}
```

**Important Rules:**
- Only modify the **values** (right side), never the keys (left side)
- Keep the JSON structure intact
- Use `{{variable}}` for dynamic values (e.g., `"Welcome, {{name}}"`)

### Step 3: Save and Test

1. Save the file
2. Refresh your browser to see changes
3. Switch languages using the language switcher to verify

## Adding a New Language

### Step 1: Create Translation Folders

Create a new folder for your language using its ISO 639-1 code:

```bash
# Frontend
mkdir -p src/NOIR.Web/frontend/public/locales/fr

# Backend
mkdir -p src/NOIR.Web/Resources/Localization/fr
```

### Step 2: Copy English Files as Templates

```bash
# Frontend
cp src/NOIR.Web/frontend/public/locales/en/*.json src/NOIR.Web/frontend/public/locales/fr/

# Backend
cp src/NOIR.Web/Resources/Localization/en/*.json src/NOIR.Web/Resources/Localization/fr/
```

### Step 3: Update Configuration

**Frontend (`src/NOIR.Web/frontend/src/i18n/index.ts`):**
```typescript
export const supportedLanguages = {
  en: { name: 'English', nativeName: 'English', dir: 'ltr' },
  vi: { name: 'Vietnamese', nativeName: 'Tiếng Việt', dir: 'ltr' },
  fr: { name: 'French', nativeName: 'Français', dir: 'ltr' },  // Add new language
} as const;
```

**Backend (`src/NOIR.Web/appsettings.json`):**
```json
{
  "Localization": {
    "SupportedCultures": ["en", "vi", "fr"]
  }
}
```

### Step 4: Translate Files

Open each JSON file and translate the values.

## Translation Key Naming Convention

Keys follow this structure:
```
{namespace}.{feature}.{element}.{state?}
```

Examples:
```
common.buttons.save         → "Save" button text
auth.login.title            → Login page title
errors.validation.required  → Required field error
nav.menu.dashboard          → Dashboard menu item
```

## Dynamic Values (Interpolation)

Use `{{variable}}` for dynamic content:

```json
{
  "welcome": "Welcome, {{name}}!",
  "itemCount": "You have {{count}} items"
}
```

Usage in code:
```typescript
t('welcome', { name: 'John' })  // → "Welcome, John!"
```

## Pluralization

For plural forms, use the `_one` and `_other` suffixes:

```json
{
  "item_one": "{{count}} item",
  "item_other": "{{count}} items"
}
```

## Best Practices

### DO:
- Keep translations consistent across languages
- Use the common namespace for reusable text
- Test translations in context (not just in isolation)
- Provide context for translators when strings are ambiguous

### DON'T:
- Hardcode text in components
- Split sentences for translation
- Use language-specific punctuation in keys
- Forget to translate placeholder text

## Troubleshooting

### Translation Not Showing

1. Check the browser console for loading errors
2. Verify the JSON file is valid (use a JSON validator)
3. Ensure the key exists in all language files
4. Clear browser cache and refresh

### Language Not Switching

1. Check localStorage for `noir-language` key
2. Verify the language code is in `supportedLanguages`
3. Check console for i18next errors

## File Format Reference

### common.json Structure
```json
{
  "buttons": {
    "save": "Save",
    "cancel": "Cancel"
    // ... more buttons
  },
  "labels": {
    "loading": "Loading...",
    "name": "Name"
    // ... more labels
  },
  "messages": {
    "saveSuccess": "Saved successfully"
    // ... more messages
  }
}
```

### errors.json Structure
```json
{
  "validation": {
    "required": "This field is required",
    "email": "Please enter a valid email"
    // ... more validation errors
  },
  "api": {
    "notFound": "Not found",
    "serverError": "Server error"
    // ... more API errors
  }
}
```

## Support

For questions about translations or localization, please contact the development team or create an issue in the project repository.
