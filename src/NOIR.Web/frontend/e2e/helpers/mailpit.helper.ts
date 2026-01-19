import { APIRequestContext } from '@playwright/test'

const MAILPIT_API_URL = 'http://localhost:8025'

/**
 * Helper for interacting with Mailpit API to retrieve OTPs from test emails.
 * Mailpit runs on port 8025 in development (SMTP on port 1025).
 */
export class MailpitHelper {
  constructor(private request: APIRequestContext) {}

  /**
   * Get the latest email sent to a specific address.
   */
  async getLatestEmail(toEmail: string): Promise<MailpitMessage | null> {
    const response = await this.request.get(`${MAILPIT_API_URL}/api/v1/search`, {
      params: { query: `to:${toEmail}` },
    })

    if (!response.ok()) {
      console.warn(`Mailpit search failed: ${response.status()}`)
      return null
    }

    const data: MailpitSearchResult = await response.json()
    if (data.messages.length === 0) {
      return null
    }

    // Return the most recent message (first in list)
    return data.messages[0]
  }

  /**
   * Get the full email content including body.
   */
  async getEmailContent(messageId: string): Promise<MailpitMessageDetail | null> {
    const response = await this.request.get(`${MAILPIT_API_URL}/api/v1/message/${messageId}`)

    if (!response.ok()) {
      console.warn(`Failed to get email content: ${response.status()}`)
      return null
    }

    return response.json()
  }

  /**
   * Extract OTP code from email body.
   * Looks for 6-digit codes in the email content.
   */
  extractOtpFromBody(body: string): string | null {
    // Look for 6-digit OTP code - usually standalone or in a prominent format
    // Common patterns: "123456", "Your code is: 123456", etc.
    const patterns = [
      /\b(\d{6})\b/, // Generic 6-digit number
      /code[:\s]+(\d{6})/i, // "code: 123456" or "code 123456"
      /OTP[:\s]+(\d{6})/i, // "OTP: 123456"
      /verification[:\s]+(\d{6})/i, // "verification: 123456"
    ]

    for (const pattern of patterns) {
      const match = body.match(pattern)
      if (match) {
        return match[1]
      }
    }

    return null
  }

  /**
   * Wait for and retrieve OTP from email sent to the specified address.
   * Retries several times with delays to handle async email delivery.
   */
  async waitForOtp(toEmail: string, maxAttempts = 10, delayMs = 1000): Promise<string | null> {
    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      const email = await this.getLatestEmail(toEmail)

      if (email) {
        const detail = await this.getEmailContent(email.ID)
        if (detail?.Text) {
          const otp = this.extractOtpFromBody(detail.Text)
          if (otp) {
            return otp
          }
        }
        if (detail?.HTML) {
          const otp = this.extractOtpFromBody(detail.HTML)
          if (otp) {
            return otp
          }
        }
      }

      // Wait before retrying
      await new Promise(resolve => setTimeout(resolve, delayMs))
    }

    return null
  }

  /**
   * Delete all emails in Mailpit.
   * Useful for cleaning up before tests.
   */
  async deleteAllEmails(): Promise<void> {
    await this.request.delete(`${MAILPIT_API_URL}/api/v1/messages`)
  }

  /**
   * Delete emails sent to a specific address.
   */
  async deleteEmailsTo(toEmail: string): Promise<void> {
    const response = await this.request.get(`${MAILPIT_API_URL}/api/v1/search`, {
      params: { query: `to:${toEmail}` },
    })

    if (response.ok()) {
      const data: MailpitSearchResult = await response.json()
      for (const message of data.messages) {
        await this.request.delete(`${MAILPIT_API_URL}/api/v1/messages`, {
          data: { ids: [message.ID] },
        })
      }
    }
  }
}

// Mailpit API types
interface MailpitSearchResult {
  total: number
  messages: MailpitMessage[]
}

interface MailpitMessage {
  ID: string
  MessageID: string
  From: MailpitAddress
  To: MailpitAddress[]
  Subject: string
  Date: string
  Size: number
  Attachments: number
}

interface MailpitAddress {
  Name: string
  Address: string
}

interface MailpitMessageDetail extends MailpitMessage {
  HTML: string
  Text: string
  Attachments: MailpitAttachment[]
}

interface MailpitAttachment {
  PartID: string
  FileName: string
  ContentType: string
  Size: number
}
