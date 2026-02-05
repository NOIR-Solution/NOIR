import { useTranslation } from 'react-i18next'
import { Check, X } from 'lucide-react'
import { cn } from '@/lib/utils'
import {
  getPasswordStrength,
  getStrengthColor,
  getStrengthTextColor,
} from '@/lib/passwordValidation'

interface PasswordStrengthIndicatorProps {
  password: string
  showRequirements?: boolean
}

/**
 * Password strength indicator with visual bar and requirement checklist
 */
export function PasswordStrengthIndicator({
  password,
  showRequirements = true,
}: PasswordStrengthIndicatorProps) {
  const { t } = useTranslation('auth')
  const strength = getPasswordStrength(password)

  if (!password) {
    return null
  }

  return (
    <div className="space-y-3 animate-fade-in">
      {/* Strength Bar */}
      <div className="space-y-1.5">
        <div className="flex justify-between text-xs">
          <span className="text-muted-foreground">
            {t('forgotPassword.reset.passwordStrength')}
          </span>
          <span
            className={cn('font-medium', getStrengthTextColor(strength.level))}
            data-testid="password-strength-level"
          >
            {t(`forgotPassword.reset.strength.${strength.level}`)}
          </span>
        </div>
        <div className="h-2 bg-muted rounded-full overflow-hidden">
          <div
            className={cn(
              'h-full transition-all duration-300 rounded-full',
              getStrengthColor(strength.level)
            )}
            style={{ width: `${strength.score}%` }}
          />
        </div>
      </div>

      {/* Requirements Checklist */}
      {showRequirements && (
        <div className="grid grid-cols-2 gap-2 text-xs">
          <RequirementItem
            met={strength.requirements.length}
            label={t('forgotPassword.reset.requirements.length')}
          />
          <RequirementItem
            met={strength.requirements.lowercase}
            label={t('forgotPassword.reset.requirements.lowercase')}
          />
          <RequirementItem
            met={strength.requirements.uppercase}
            label={t('forgotPassword.reset.requirements.uppercase')}
          />
          <RequirementItem
            met={strength.requirements.digit}
            label={t('forgotPassword.reset.requirements.digit')}
          />
          <RequirementItem
            met={strength.requirements.special}
            label={t('forgotPassword.reset.requirements.special')}
          />
          <RequirementItem
            met={strength.requirements.uniqueChars}
            label={t('forgotPassword.reset.requirements.uniqueChars')}
          />
        </div>
      )}
    </div>
  )
}

interface RequirementItemProps {
  met: boolean
  label: string
}

function RequirementItem({ met, label }: RequirementItemProps) {
  return (
    <div className={cn(
      'flex items-center gap-1.5 transition-colors',
      met ? 'text-green-600' : 'text-muted-foreground'
    )}>
      {met ? (
        <Check className="w-3.5 h-3.5 flex-shrink-0" />
      ) : (
        <X className="w-3.5 h-3.5 flex-shrink-0" />
      )}
      <span>{label}</span>
    </div>
  )
}
