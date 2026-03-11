/**
 * NOIR Custom Shiki Theme
 *
 * Based on Material Theme Ocean, adapted to NOIR's brandkit:
 * - Primary: Sapphire Blue #2563EB / #60A5FA / #93C5FD
 * - Accent: Amber Gold #F59E0B / #FFCB6B
 * - Surfaces: Blue-tinted darks (#0E0E1A, #131320, #1A1A2E)
 * - Text: #F2F2F8 (primary), #A8A8C0 (secondary), #6E6E90 (tertiary)
 *
 * Provides both dark and light variants for theme-aware rendering.
 */

import type { ThemeRegistrationRaw } from 'shiki'

export const noirDark: ThemeRegistrationRaw = {
  name: 'noir-dark',
  type: 'dark',
  settings: [],
  colors: {
    'editor.background': '#0E0E1A',
    'editor.foreground': '#C8C8DC',
    'editor.lineHighlightBackground': '#1A1A2E80',
    'editor.selectionBackground': '#2563EB40',
    'editorLineNumber.foreground': '#363660',
    'editorLineNumber.activeForeground': '#6E6E90',
    'editorCursor.foreground': '#F59E0B',
    'editorIndentGuide.background': '#282846',
    'editorIndentGuide.activeBackground': '#363660',
    'editorWhitespace.foreground': '#28284640',
  },
  tokenColors: [
    // Base
    {
      settings: {
        background: '#0E0E1A',
        foreground: '#C8C8DC',
      },
    },
    // Comments — tertiary text, italic
    {
      scope: ['comment', 'punctuation.definition.comment', 'string.quoted.docstring'],
      settings: {
        foreground: '#6E6E90',
        fontStyle: 'italic',
      },
    },
    // Strings — soft green (harmonizes with blue primary)
    {
      scope: 'string',
      settings: {
        foreground: '#A5D6A7',
      },
    },
    // Numbers, constants — amber accent
    {
      scope: ['constant.numeric', 'keyword.other.unit'],
      settings: {
        foreground: '#FFCB6B',
      },
    },
    // Boolean, null — warm coral
    {
      scope: ['constant.language.boolean', 'constant.language.null', 'constant.language.undefined'],
      settings: {
        foreground: '#FF9CAC',
      },
    },
    // Keywords, control flow — sapphire blue (bright)
    {
      scope: ['keyword', 'keyword.control', 'keyword.operator.new', 'keyword.operator.expression',
              'storage.type', 'storage.modifier', 'storage.control',
              'variable.language.this', 'variable.language.super',
              'constant.language'],
      settings: {
        foreground: '#93C5FD',
      },
    },
    // Keyword operators, punctuation — sky blue
    {
      scope: ['keyword.operator', 'punctuation', 'constant.other.symbol'],
      settings: {
        foreground: '#7DD3FC',
      },
    },
    // Functions — bright sapphire
    {
      scope: ['entity.name.function', 'support.function', 'meta.function-call',
              'entity.name.function.call'],
      settings: {
        foreground: '#60A5FA',
      },
    },
    // Types, classes — amber gold
    {
      scope: ['entity.name.type', 'entity.other.inherited-class', 'support.type',
              'support.class', 'entity.name.type.class', 'meta.use',
              'entity.other.attribute-name.class'],
      settings: {
        foreground: '#FCD34D',
      },
    },
    // Variables, parameters — primary text
    {
      scope: ['variable', 'variable.parameter', 'support.variable',
              'variable.language', 'meta.definition.variable'],
      settings: {
        foreground: '#C8C8DC',
      },
    },
    // Object properties, meta fields — light coral/salmon
    {
      scope: ['variable.object.property', 'meta.object-literal.key',
              'meta.field.declaration entity.name.function',
              'support.type.property-name.json',
              'entity.name.tag.yaml'],
      settings: {
        foreground: '#F0A0A0',
      },
    },
    // Storage type modifiers — lavender purple
    {
      scope: ['storage.type.function', 'keyword.control.import', 'keyword.control.export',
              'keyword.control.from'],
      settings: {
        foreground: '#C4B5FD',
      },
    },
    // Template expressions — sky blue
    {
      scope: ['template.expression.begin', 'template.expression.end',
              'punctuation.definition.template-expression.begin',
              'punctuation.definition.template-expression.end',
              'punctuation.section.embedded'],
      settings: {
        foreground: '#7DD3FC',
      },
    },
    // HTML/JSX tags — coral
    {
      scope: ['entity.name.tag', 'meta.tag', 'punctuation.definition.tag'],
      settings: {
        foreground: '#F0A0A0',
      },
    },
    // HTML/JSX attributes — lavender
    {
      scope: 'entity.other.attribute-name',
      settings: {
        foreground: '#C4B5FD',
      },
    },
    // CSS properties — light blue
    {
      scope: 'support.type.property-name.css',
      settings: {
        foreground: '#93C5FD',
      },
    },
    // Markdown headings — sky blue
    {
      scope: 'markup.heading',
      settings: {
        foreground: '#7DD3FC',
      },
    },
    // Markdown bold — bright text
    {
      scope: 'markup.bold',
      settings: {
        fontStyle: 'bold',
        foreground: '#F2F2F8',
      },
    },
    // Markdown italic — amber
    {
      scope: 'markup.italic',
      settings: {
        fontStyle: 'italic',
        foreground: '#FCD34D',
      },
    },
    // Markdown inline code — green
    {
      scope: ['markup.inline.raw.string.markdown', 'markup.fenced_code.block.markdown punctuation.definition.markdown'],
      settings: {
        foreground: '#A5D6A7',
      },
    },
    // Diff added — green
    {
      scope: 'markup.inserted',
      settings: {
        foreground: '#A5D6A7',
      },
    },
    // Diff deleted — coral
    {
      scope: 'markup.deleted',
      settings: {
        foreground: '#F0A0A0',
      },
    },
    // Regex — amber
    {
      scope: ['string.regexp', 'constant.other.character-class.regexp'],
      settings: {
        foreground: '#FFCB6B',
      },
    },
    // Escape characters — foreground
    {
      scope: 'constant.character.escape',
      settings: {
        foreground: '#C8C8DC',
      },
    },
    // C# specific — method names
    {
      scope: ['source.cs meta.method-call meta.method', 'source.cs entity.name.function'],
      settings: {
        foreground: '#60A5FA',
      },
    },
    // C# specific — types
    {
      scope: ['source.cs meta.class.identifier storage.type', 'source.cs storage.type',
              'source.cs meta.method.return-type'],
      settings: {
        foreground: '#FCD34D',
      },
    },
    // C# specific — namespaces
    {
      scope: 'source.cs entity.name.type.namespace',
      settings: {
        foreground: '#C8C8DC',
      },
    },
    // JSON nested keys (alternating colors for readability)
    {
      scope: 'meta.structure.dictionary.json support.type.property-name.json',
      settings: {
        foreground: '#C4B5FD',
      },
    },
    {
      scope: 'meta.structure.dictionary.json meta.structure.dictionary.value.json meta.structure.dictionary.json support.type.property-name.json',
      settings: {
        foreground: '#FCD34D',
      },
    },
    // Constant placeholders, format specifiers
    {
      scope: ['constant.other.placeholder', 'constant.character.format.placeholder'],
      settings: {
        foreground: '#F0A0A0',
      },
    },
    // JSX components — amber (like types)
    {
      scope: 'support.class.component',
      settings: {
        foreground: '#FCD34D',
      },
    },
  ],
}

export const noirLight: ThemeRegistrationRaw = {
  name: 'noir-light',
  type: 'light',
  settings: [],
  colors: {
    'editor.background': '#FAFAFA',
    'editor.foreground': '#1C1C2E',
    'editor.lineHighlightBackground': '#F0F0F8',
    'editor.selectionBackground': '#2563EB25',
    'editorLineNumber.foreground': '#A8A8C0',
    'editorLineNumber.activeForeground': '#6E6E90',
    'editorCursor.foreground': '#2563EB',
    'editorIndentGuide.background': '#E4E4F0',
    'editorIndentGuide.activeBackground': '#C8C8DC',
    'editorWhitespace.foreground': '#E4E4F040',
  },
  tokenColors: [
    // Base
    {
      settings: {
        background: '#FAFAFA',
        foreground: '#1C1C2E',
      },
    },
    // Comments
    {
      scope: ['comment', 'punctuation.definition.comment', 'string.quoted.docstring'],
      settings: {
        foreground: '#8E8EA8',
        fontStyle: 'italic',
      },
    },
    // Strings — deep green
    {
      scope: 'string',
      settings: {
        foreground: '#2E7D32',
      },
    },
    // Numbers — deep amber
    {
      scope: ['constant.numeric', 'keyword.other.unit'],
      settings: {
        foreground: '#B45309',
      },
    },
    // Boolean, null — coral
    {
      scope: ['constant.language.boolean', 'constant.language.null', 'constant.language.undefined'],
      settings: {
        foreground: '#BE185D',
      },
    },
    // Keywords — sapphire blue (primary)
    {
      scope: ['keyword', 'keyword.control', 'keyword.operator.new', 'keyword.operator.expression',
              'storage.type', 'storage.modifier', 'storage.control',
              'variable.language.this', 'variable.language.super',
              'constant.language'],
      settings: {
        foreground: '#2563EB',
      },
    },
    // Operators, punctuation
    {
      scope: ['keyword.operator', 'punctuation', 'constant.other.symbol'],
      settings: {
        foreground: '#1D4ED8',
      },
    },
    // Functions — deep blue
    {
      scope: ['entity.name.function', 'support.function', 'meta.function-call',
              'entity.name.function.call'],
      settings: {
        foreground: '#1E40AF',
      },
    },
    // Types, classes — deep amber
    {
      scope: ['entity.name.type', 'entity.other.inherited-class', 'support.type',
              'support.class', 'entity.name.type.class', 'meta.use',
              'entity.other.attribute-name.class'],
      settings: {
        foreground: '#92400E',
      },
    },
    // Variables
    {
      scope: ['variable', 'variable.parameter', 'support.variable'],
      settings: {
        foreground: '#1C1C2E',
      },
    },
    // Object properties
    {
      scope: ['variable.object.property', 'meta.object-literal.key',
              'support.type.property-name.json', 'entity.name.tag.yaml'],
      settings: {
        foreground: '#9F1239',
      },
    },
    // Import/export
    {
      scope: ['storage.type.function', 'keyword.control.import', 'keyword.control.export',
              'keyword.control.from'],
      settings: {
        foreground: '#7C3AED',
      },
    },
    // HTML/JSX tags
    {
      scope: ['entity.name.tag', 'meta.tag', 'punctuation.definition.tag'],
      settings: {
        foreground: '#9F1239',
      },
    },
    // HTML/JSX attributes
    {
      scope: 'entity.other.attribute-name',
      settings: {
        foreground: '#7C3AED',
      },
    },
    // CSS properties
    {
      scope: 'support.type.property-name.css',
      settings: {
        foreground: '#2563EB',
      },
    },
    // Markdown headings
    {
      scope: 'markup.heading',
      settings: {
        foreground: '#1D4ED8',
      },
    },
    // Inline code
    {
      scope: ['markup.inline.raw.string.markdown', 'markup.fenced_code.block.markdown punctuation.definition.markdown'],
      settings: {
        foreground: '#2E7D32',
      },
    },
    // Diff
    {
      scope: 'markup.inserted',
      settings: { foreground: '#2E7D32' },
    },
    {
      scope: 'markup.deleted',
      settings: { foreground: '#9F1239' },
    },
    // JSON keys
    {
      scope: 'meta.structure.dictionary.json support.type.property-name.json',
      settings: {
        foreground: '#7C3AED',
      },
    },
    // JSX components
    {
      scope: 'support.class.component',
      settings: {
        foreground: '#92400E',
      },
    },
  ],
}
