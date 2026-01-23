import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/** Known acronyms that should stay uppercased */
const ACRONYMS = new Set(['otp', 'api', 'url', 'smtp', 'html', 'css', 'id', 'sso'])

/**
 * Convert PascalCase name to human-readable display name
 * Handles known acronyms (OTP, API, URL, etc.)
 * @example "PasswordResetOtp" → "Password Reset OTP"
 * @example "WelcomeEmail" → "Welcome Email"
 */
export function formatDisplayName(name: string): string {
  // Insert space before each uppercase letter (handles PascalCase)
  const spaced = name.replace(/([A-Z])/g, ' $1').trim()
  // Split into words and handle acronyms
  return spaced
    .split(' ')
    .map(word => ACRONYMS.has(word.toLowerCase()) ? word.toUpperCase() : word)
    .join(' ')
}
