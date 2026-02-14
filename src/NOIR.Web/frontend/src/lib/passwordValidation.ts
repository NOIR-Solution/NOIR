/**
 * Password validation utilities
 * Matches backend password policy from IdentitySettings
 */

export interface PasswordRequirements {
  length: boolean
  lowercase: boolean
  uppercase: boolean
  digit: boolean
  special: boolean
  uniqueChars: boolean
}

export interface PasswordStrength {
  score: number // 0-100
  level: 'weak' | 'fair' | 'good' | 'strong'
  requirements: PasswordRequirements
  isValid: boolean
}

/**
 * Check password against all requirements
 * Production requirements: 12+ chars, upper, lower, digit, special, 4+ unique chars
 */
export const validatePassword = (password: string): PasswordRequirements => {
  return {
    length: password.length >= 12,
    lowercase: /[a-z]/.test(password),
    uppercase: /[A-Z]/.test(password),
    digit: /[0-9]/.test(password),
    special: /[^A-Za-z0-9]/.test(password),
    uniqueChars: new Set(password).size >= 4,
  }
}

/**
 * Calculate password strength score and level
 */
export const getPasswordStrength = (password: string): PasswordStrength => {
  const requirements = validatePassword(password)

  // Count met requirements (excluding length which is mandatory)
  const metRequirements = Object.values(requirements).filter(Boolean).length
  const totalRequirements = Object.keys(requirements).length

  // Base score from met requirements
  const score = Math.round((metRequirements / totalRequirements) * 100)

  // Determine level
  let level: PasswordStrength['level']
  if (score < 40) {
    level = 'weak'
  } else if (score < 60) {
    level = 'fair'
  } else if (score < 80) {
    level = 'good'
  } else {
    level = 'strong'
  }

  // Password is valid only if ALL requirements are met
  const isValid = Object.values(requirements).every(Boolean)

  return {
    score,
    level,
    requirements,
    isValid,
  }
}

/**
 * Get color classes for password strength indicator
 */
export const getStrengthColor = (level: PasswordStrength['level']): string => {
  switch (level) {
    case 'weak':
      return 'bg-red-500'
    case 'fair':
      return 'bg-orange-500'
    case 'good':
      return 'bg-yellow-500'
    case 'strong':
      return 'bg-green-500'
  }
}

/**
 * Get text color for password strength label
 */
export const getStrengthTextColor = (level: PasswordStrength['level']): string => {
  switch (level) {
    case 'weak':
      return 'text-red-600'
    case 'fair':
      return 'text-orange-600'
    case 'good':
      return 'text-yellow-600'
    case 'strong':
      return 'text-green-600'
  }
}
