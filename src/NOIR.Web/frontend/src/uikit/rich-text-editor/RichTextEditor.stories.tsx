import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { RichTextEditor } from './RichTextEditor'

const sampleHtml = `
<h2>Sample Blog Post</h2>
<p>This is a <strong>rich text editor</strong> powered by <a href="https://tiptap.dev">Tiptap</a>.</p>
<ul>
  <li>Bold, italic, underline formatting</li>
  <li>Headings (H1-H4)</li>
  <li>Links and images</li>
  <li>Tables</li>
  <li>Code blocks</li>
</ul>
<blockquote>This is a blockquote for emphasis.</blockquote>
<p>Text can be <em>italic</em>, <strong>bold</strong>, or <u>underlined</u>.</p>
`

const RichTextEditorDemo = (props: {
  preset?: 'full' | 'minimal'
  height?: number
  readOnly?: boolean
  hasError?: boolean
  placeholder?: string
  variables?: string[]
}) => {
  const [value, setValue] = useState(props.readOnly ? sampleHtml : '')
  return (
    <div style={{ maxWidth: 800 }}>
      <RichTextEditor
        value={value}
        onChange={setValue}
        {...props}
      />
      {!props.readOnly && (
        <details className="mt-4">
          <summary className="cursor-pointer text-sm text-muted-foreground">HTML Output</summary>
          <pre className="mt-2 max-h-48 overflow-auto rounded bg-muted p-3 text-xs">{value}</pre>
        </details>
      )}
    </div>
  )
}

const meta = {
  title: 'UIKit/RichTextEditor',
  component: RichTextEditor,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <div style={{ padding: 24 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof RichTextEditor>

export default meta
type Story = StoryObj<typeof meta>

export const FullPreset: Story = {
  render: () => <RichTextEditorDemo preset="full" height={400} placeholder="Start writing..." />,
}

export const MinimalPreset: Story = {
  render: () => <RichTextEditorDemo preset="minimal" height={300} placeholder="Write a description..." />,
}

export const WithContent: Story = {
  render: () => {
    const [value, setValue] = useState(sampleHtml)
    return (
      <div style={{ maxWidth: 800 }}>
        <RichTextEditor value={value} onChange={setValue} preset="full" height={400} />
      </div>
    )
  },
}

export const ReadOnly: Story = {
  render: () => <RichTextEditorDemo readOnly preset="full" height={300} />,
}

export const ErrorState: Story = {
  render: () => <RichTextEditorDemo hasError preset="full" height={300} placeholder="This field has an error" />,
}

export const WithVariables: Story = {
  render: () => (
    <RichTextEditorDemo
      preset="full"
      height={400}
      variables={['UserName', 'CompanyName', 'ResetLink', 'ExpiryDate', 'SupportEmail']}
      placeholder="Write your email template..."
    />
  ),
}
