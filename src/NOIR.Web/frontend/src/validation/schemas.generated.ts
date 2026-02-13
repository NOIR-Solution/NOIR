/**
 * Zod Validation Schemas
 * 
 * Auto-generated from FluentValidation rules.
 * DO NOT EDIT - run 'pnpm run generate:validation' to regenerate.
 * 
 * @generated 2026-01-14T05:57:39.508Z
 */

import { z } from "zod"

/**
 * Validation schema for UpdateUserCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const updateUserSchema = z.object({
  userId: z.string().min(1, { message: 'This field is required' }),
  displayName: z.string().max(100, { message: 'Maximum 100 characters allowed' }).optional().or(z.literal('')),
  firstName: z.string().max(50, { message: 'Maximum 50 characters allowed' }).optional().or(z.literal('')),
  lastName: z.string().max(50, { message: 'Maximum 50 characters allowed' }).optional().or(z.literal('')),
})

export type UpdateUserInput = z.infer<typeof updateUserSchema>

/**
 * Validation schema for DeleteUserCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const deleteUserSchema = z.object({
  userId: z.string().min(1, { message: 'This field is required' }),
})

export type DeleteUserInput = z.infer<typeof deleteUserSchema>

/**
 * Validation schema for AssignRolesToUserCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const assignRolesToUserSchema = z.object({
  userId: z.string().min(1, { message: 'This field is required' }),
  roleNames: z.record(z.string(), z.unknown()),
})

export type AssignRolesToUserInput = z.infer<typeof assignRolesToUserSchema>

/**
 * Validation schema for UpdateTenantCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const updateTenantSchema = z.object({
  tenantId: z.string().min(1, { message: 'This field is required' }),
  identifier: z.string().min(1, { message: 'This field is required' }).min(2, { message: 'Minimum 2 characters required' }).max(100, { message: 'Maximum 100 characters allowed' }).regex(/^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$/, { message: 'Invalid format' }),
  name: z.string().min(1, { message: 'This field is required' }).min(2, { message: 'Minimum 2 characters required' }).max(200, { message: 'Maximum 200 characters allowed' }),
})

export type UpdateTenantInput = z.infer<typeof updateTenantSchema>

/**
 * Validation schema for DeleteTenantCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const deleteTenantSchema = z.object({
  tenantId: z.string().min(1, { message: 'This field is required' }),
})

export type DeleteTenantInput = z.infer<typeof deleteTenantSchema>

/**
 * Validation schema for CreateTenantCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const createTenantSchema = z.object({
  identifier: z.string().min(1, { message: 'This field is required' }).min(2, { message: 'Minimum 2 characters required' }).max(100, { message: 'Maximum 100 characters allowed' }).regex(/^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$/, { message: 'Invalid format' }),
  name: z.string().min(1, { message: 'This field is required' }).min(2, { message: 'Minimum 2 characters required' }).max(200, { message: 'Maximum 200 characters allowed' }),
})

export type CreateTenantInput = z.infer<typeof createTenantSchema>

/**
 * Validation schema for UpdateRoleCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const updateRoleSchema = z.object({
  roleId: z.string().min(1, { message: 'This field is required' }),
  name: z.string().min(1, { message: 'This field is required' }).min(2, { message: 'Minimum 2 characters required' }).max(50, { message: 'Maximum 50 characters allowed' }).regex(/^[a-zA-Z][a-zA-Z0-9_-]*$/, { message: 'Invalid format' }),
})

export type UpdateRoleInput = z.infer<typeof updateRoleSchema>

/**
 * Validation schema for DeleteRoleCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const deleteRoleSchema = z.object({
  roleId: z.string().min(1, { message: 'This field is required' }),
})

export type DeleteRoleInput = z.infer<typeof deleteRoleSchema>

/**
 * Validation schema for CreateRoleCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const createRoleSchema = z.object({
  name: z.string().min(1, { message: 'This field is required' }).min(2, { message: 'Minimum 2 characters required' }).max(50, { message: 'Maximum 50 characters allowed' }).regex(/^[a-zA-Z][a-zA-Z0-9_-]*$/, { message: 'Invalid format' }),
  permissions: z.record(z.string(), z.unknown()),
})

export type CreateRoleInput = z.infer<typeof createRoleSchema>

/**
 * Validation schema for RemovePermissionFromRoleCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const removePermissionFromRoleSchema = z.object({
  roleId: z.string().min(1, { message: 'This field is required' }),
  permissions: z.record(z.string(), z.unknown()),
})

export type RemovePermissionFromRoleInput = z.infer<typeof removePermissionFromRoleSchema>

/**
 * Validation schema for AssignPermissionToRoleCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const assignPermissionToRoleSchema = z.object({
  roleId: z.string().min(1, { message: 'This field is required' }),
  permissions: z.record(z.string(), z.unknown()),
})

export type AssignPermissionToRoleInput = z.infer<typeof assignPermissionToRoleSchema>

/**
 * Validation schema for UpdatePreferencesCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const updatePreferencesSchema = z.object({
  preferences: z.array(z.unknown()),
})

export type UpdatePreferencesInput = z.infer<typeof updatePreferencesSchema>

/**
 * Validation schema for UpdateEmailTemplateCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const updateEmailTemplateSchema = z.object({
  id: z.string().min(1, { message: 'This field is required' }),
  subject: z.string().min(1, { message: 'This field is required' }).max(500, { message: 'Maximum 500 characters allowed' }),
  htmlBody: z.string().min(1, { message: 'This field is required' }),
  description: z.string().max(1000, { message: 'Maximum 1000 characters allowed' }).optional().or(z.literal('')),
})

export type UpdateEmailTemplateInput = z.infer<typeof updateEmailTemplateSchema>

/**
 * Validation schema for SendTestEmailCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const sendTestEmailSchema = z.object({
  templateId: z.string().min(1, { message: 'This field is required' }),
  recipientEmail: z.string().min(1, { message: 'This field is required' }).email({ message: 'Invalid email address' }),
  sampleData: z.record(z.string(), z.unknown()),
})

export type SendTestEmailInput = z.infer<typeof sendTestEmailSchema>

/**
 * Validation schema for UploadAvatarCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const uploadAvatarSchema = z.object({
  fileName: z.string().min(1, { message: 'This field is required' }),
  fileSize: z.number().lte(2097152, { message: 'Must be at most 2097152' }).optional(),
  contentType: z.string().min(1, { message: 'This field is required' }),
})

export type UploadAvatarInput = z.infer<typeof uploadAvatarSchema>

/**
 * Validation schema for UpdateUserProfileCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const updateUserProfileSchema = z.object({
  firstName: z.string().max(50, { message: 'Maximum 50 characters allowed' }).optional().or(z.literal('')),
  lastName: z.string().max(50, { message: 'Maximum 50 characters allowed' }).optional().or(z.literal('')),
  displayName: z.string().max(100, { message: 'Maximum 100 characters allowed' }).optional().or(z.literal('')),
  phoneNumber: z.string().max(20, { message: 'Maximum 20 characters allowed' }).optional().or(z.literal('')),
})

export type UpdateUserProfileInput = z.infer<typeof updateUserProfileSchema>

/**
 * Validation schema for RegisterCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const registerSchema = z.object({
  email: z.string().min(1, { message: 'This field is required' }).email({ message: 'Invalid email address' }),
  password: z.string().min(1, { message: 'This field is required' }).min(6, { message: 'Minimum 6 characters required' }),
  firstName: z.string().max(100, { message: 'Maximum 100 characters allowed' }).optional().or(z.literal('')),
  lastName: z.string().max(100, { message: 'Maximum 100 characters allowed' }).optional().or(z.literal('')),
})

export type RegisterInput = z.infer<typeof registerSchema>

/**
 * Validation schema for RefreshTokenCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const refreshTokenSchema = z.object({
  accessToken: z.string().min(1, { message: 'This field is required' }),
  refreshToken: z.string().min(1, { message: 'This field is required' }),
})

export type RefreshTokenInput = z.infer<typeof refreshTokenSchema>

/**
 * Validation schema for VerifyPasswordResetOtpCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const verifyPasswordResetOtpSchema = z.object({
  sessionToken: z.string().min(1, { message: 'This field is required' }),
  otp: z.string().min(1, { message: 'This field is required' }).length(6, { message: 'Must be exactly 6 characters' }),
})

export type VerifyPasswordResetOtpInput = z.infer<typeof verifyPasswordResetOtpSchema>

/**
 * Validation schema for ResetPasswordCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const resetPasswordSchema = z.object({
  resetToken: z.string().min(1, { message: 'This field is required' }),
  newPassword: z.string().min(1, { message: 'This field is required' }).min(6, { message: 'Minimum 6 characters required' }),
})

export type ResetPasswordInput = z.infer<typeof resetPasswordSchema>

/**
 * Validation schema for ResendPasswordResetOtpCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const resendPasswordResetOtpSchema = z.object({
  sessionToken: z.string().min(1, { message: 'This field is required' }),
})

export type ResendPasswordResetOtpInput = z.infer<typeof resendPasswordResetOtpSchema>

/**
 * Validation schema for RequestPasswordResetCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const requestPasswordResetSchema = z.object({
  email: z.string().min(1, { message: 'This field is required' }).max(256, { message: 'Maximum 256 characters allowed' }).email({ message: 'Invalid email address' }),
})

export type RequestPasswordResetInput = z.infer<typeof requestPasswordResetSchema>

/**
 * Validation schema for LoginCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const loginSchema = z.object({
  email: z.string().min(1, { message: 'This field is required' }).email({ message: 'Invalid email address' }),
  password: z.string().min(1, { message: 'This field is required' }),
})

export type LoginInput = z.infer<typeof loginSchema>

/**
 * Validation schema for ChangePasswordCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const changePasswordSchema = z.object({
  currentPassword: z.string().min(1, { message: 'This field is required' }),
  newPassword: z.string().min(1, { message: 'This field is required' }).min(6, { message: 'Minimum 6 characters required' }),
})

export type ChangePasswordInput = z.infer<typeof changePasswordSchema>

/**
 * Validation schema for RequestEmailChangeCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const requestEmailChangeSchema = z.object({
  newEmail: z.string().min(1, { message: 'This field is required' }).max(256, { message: 'Maximum 256 characters allowed' }).email({ message: 'Invalid email address' }),
})

export type RequestEmailChangeInput = z.infer<typeof requestEmailChangeSchema>

/**
 * Validation schema for ResendEmailChangeOtpCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const resendEmailChangeOtpSchema = z.object({
  sessionToken: z.string().min(1, { message: 'This field is required' }),
})

export type ResendEmailChangeOtpInput = z.infer<typeof resendEmailChangeOtpSchema>

/**
 * Validation schema for VerifyEmailChangeCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const verifyEmailChangeSchema = z.object({
  sessionToken: z.string().min(1, { message: 'This field is required' }),
  otp: z.string().min(1, { message: 'This field is required' }).length(6, { message: 'Must be exactly 6 characters' }),
})

export type VerifyEmailChangeInput = z.infer<typeof verifyEmailChangeSchema>

/**
 * Validation schema for UpdateRetentionPolicyCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const updateRetentionPolicySchema = z.object({
  id: z.string().min(1, { message: 'This field is required' }),
  name: z.string().min(1, { message: 'This field is required' }).max(200, { message: 'Maximum 200 characters allowed' }),
  description: z.string().max(1000, { message: 'Maximum 1000 characters allowed' }).optional().or(z.literal('')),
  hotStorageDays: z.number().gte(0, { message: 'Must be at least 0' }).optional(),
  warmStorageDays: z.number().optional(),
  coldStorageDays: z.number().optional(),
  deleteAfterDays: z.number().optional(),
})

export type UpdateRetentionPolicyInput = z.infer<typeof updateRetentionPolicySchema>

/**
 * Validation schema for DeleteRetentionPolicyCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const deleteRetentionPolicySchema = z.object({
  id: z.string().min(1, { message: 'This field is required' }),
})

export type DeleteRetentionPolicyInput = z.infer<typeof deleteRetentionPolicySchema>

/**
 * Validation schema for CreateRetentionPolicyCommand
 * @generated from FluentValidation - DO NOT EDIT
 */
export const createRetentionPolicySchema = z.object({
  name: z.string().min(1, { message: 'This field is required' }).max(200, { message: 'Maximum 200 characters allowed' }),
  description: z.string().max(1000, { message: 'Maximum 1000 characters allowed' }).optional().or(z.literal('')),
  hotStorageDays: z.number().gte(0, { message: 'Must be at least 0' }).optional(),
  warmStorageDays: z.number().optional(),
  coldStorageDays: z.number().optional(),
  deleteAfterDays: z.number().optional(),
  compliancePreset: z.string().optional().or(z.literal('')),
})

export type CreateRetentionPolicyInput = z.infer<typeof createRetentionPolicySchema>

/**
 * Map of command names to their validation schemas
 */
export const validationSchemas = {
  "UpdateUserCommand": updateUserSchema,
  "DeleteUserCommand": deleteUserSchema,
  "AssignRolesToUserCommand": assignRolesToUserSchema,
  "UpdateTenantCommand": updateTenantSchema,
  "DeleteTenantCommand": deleteTenantSchema,
  "CreateTenantCommand": createTenantSchema,
  "UpdateRoleCommand": updateRoleSchema,
  "DeleteRoleCommand": deleteRoleSchema,
  "CreateRoleCommand": createRoleSchema,
  "RemovePermissionFromRoleCommand": removePermissionFromRoleSchema,
  "AssignPermissionToRoleCommand": assignPermissionToRoleSchema,
  "UpdatePreferencesCommand": updatePreferencesSchema,
  "UpdateEmailTemplateCommand": updateEmailTemplateSchema,
  "SendTestEmailCommand": sendTestEmailSchema,
  "UploadAvatarCommand": uploadAvatarSchema,
  "UpdateUserProfileCommand": updateUserProfileSchema,
  "RegisterCommand": registerSchema,
  "RefreshTokenCommand": refreshTokenSchema,
  "VerifyPasswordResetOtpCommand": verifyPasswordResetOtpSchema,
  "ResetPasswordCommand": resetPasswordSchema,
  "ResendPasswordResetOtpCommand": resendPasswordResetOtpSchema,
  "RequestPasswordResetCommand": requestPasswordResetSchema,
  "LoginCommand": loginSchema,
  "ChangePasswordCommand": changePasswordSchema,
  "RequestEmailChangeCommand": requestEmailChangeSchema,
  "ResendEmailChangeOtpCommand": resendEmailChangeOtpSchema,
  "VerifyEmailChangeCommand": verifyEmailChangeSchema,
  "UpdateRetentionPolicyCommand": updateRetentionPolicySchema,
  "DeleteRetentionPolicyCommand": deleteRetentionPolicySchema,
  "CreateRetentionPolicyCommand": createRetentionPolicySchema,
} as const

export type ValidatedCommandName = keyof typeof validationSchemas
