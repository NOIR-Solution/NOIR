import type { Meta, StoryObj } from 'storybook'
import { LogMessageFormatter } from './LogMessageFormatter'

const meta = {
  title: 'UIKit/LogMessageFormatter',
  component: LogMessageFormatter,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
  },
} satisfies Meta<typeof LogMessageFormatter>

export default meta
type Story = StoryObj<typeof meta>

export const HttpGetRequest: Story = {
  args: {
    message: 'HTTP "GET" "/api/products" responded 200 in 5ms',
  },
}

export const HttpPostRequest: Story = {
  args: {
    message: 'HTTP "POST" "/api/orders" responded 201 in 47ms',
  },
}

export const HttpErrorResponse: Story = {
  args: {
    message: 'HTTP "GET" "/api/users/123" responded 404 in 12ms',
  },
}

export const HttpServerError: Story = {
  args: {
    message: 'HTTP "POST" "/api/checkout" responded 500 in 1250ms',
  },
}

export const HandlerStartMessage: Story = {
  args: {
    message: 'Handling "GetProductsQuery"',
  },
}

export const HandlerSuccessMessage: Story = {
  args: {
    message: 'Handled "CreateOrderCommand" successfully in 47ms',
  },
}

export const HandlerFailedMessage: Story = {
  args: {
    message: 'Handled "UpdateUserCommand" failed',
  },
}

export const MessageWithUUID: Story = {
  args: {
    message: 'Processing order 550e8400-e29b-41d4-a716-446655440000 for user',
  },
}

export const MessageWithCorrelationId: Story = {
  args: {
    message: 'Request received CorrelationId: "abc-123-def-456"',
  },
}

export const PlainTextMessage: Story = {
  args: {
    message: 'Application started on port 4000',
  },
}

export const SlowResponse: Story = {
  args: {
    message: 'HTTP "GET" "/api/reports/generate" responded 200 in 2500ms',
  },
}

export const UnquotedFormat: Story = {
  args: {
    message: 'HTTP GET /api/products Responded 200 in 15ms',
  },
}

export const AllMessageTypes: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', fontFamily: 'monospace', fontSize: 13 }}>
      <LogMessageFormatter message='HTTP "GET" "/api/products" responded 200 in 5ms' />
      <LogMessageFormatter message='HTTP "POST" "/api/orders" responded 201 in 47ms' />
      <LogMessageFormatter message='HTTP "DELETE" "/api/users/123" responded 204 in 18ms' />
      <LogMessageFormatter message='HTTP "GET" "/api/reports" responded 500 in 2500ms' />
      <LogMessageFormatter message='Handling "GetProductsQuery"' />
      <LogMessageFormatter message='Handled "GetProductsQuery" successfully in 12ms' />
      <LogMessageFormatter message='Handled "DeleteOrderCommand" failed' />
      <LogMessageFormatter message='Processing order 550e8400-e29b-41d4-a716-446655440000' />
      <LogMessageFormatter message='Request CorrelationId: "req-abc-123"' />
      <LogMessageFormatter message='Application started successfully' />
    </div>
  ),
}
