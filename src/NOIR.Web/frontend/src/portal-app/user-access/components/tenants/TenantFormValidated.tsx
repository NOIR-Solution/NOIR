/**
 * TenantFormValidated - Tenant form with Zod validation
 *
 * This demonstrates the new validation pattern using:
 * - useValidatedForm hook
 * - Auto-generated Zod schemas from FluentValidation
 * - FormField component for consistent error handling
 *
 * Key benefits:
 * - Real-time validation matching server-side rules
 * - Type-safe form handling
 * - Automatic error display
 * - Server error integration
 */

import { useTranslation } from "react-i18next"
import { Button, Label, SimpleFormField as FormField, FormTextarea, FormError } from '@uikit'
import { useValidatedForm } from "@/hooks/useValidatedForm"
import { updateTenantSchema } from "@/validation/schemas.generated"
import type { Tenant } from "@/types"
import { z } from "zod"
import { UserPlus } from "lucide-react"

// Schema factory for provisioning a new tenant (create mode)
// Admin user is REQUIRED for all new tenants
const createProvisionTenantSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    identifier: z
      .string()
      .min(1, { message: t('validation.required') })
      .min(2, { message: t('validation.minLength', { count: 2 }) })
      .max(64, { message: t('validation.maxLength', { count: 64 }) })
      .regex(/^[a-z0-9-]+$/, { message: t('validation.identifierFormat') }),
    name: z
      .string()
      .min(1, { message: t('validation.required') })
      .min(2, { message: t('validation.minLength', { count: 2 }) })
      .max(256, { message: t('validation.maxLength', { count: 256 }) }),
    description: z
      .string()
      .max(1024, { message: t('validation.maxLength', { count: 1024 }) })
      .optional()
      .or(z.literal("")),
    note: z
      .string()
      .max(4096, { message: t('validation.maxLength', { count: 4096 }) })
      .optional()
      .or(z.literal("")),
    // Admin user fields - always required for provisioning
    adminEmail: z
      .string()
      .min(1, { message: t('validation.required') })
      .email({ message: t('validation.invalidEmail') }),
    adminPassword: z
      .string()
      .min(1, { message: t('validation.required') })
      .min(6, { message: t('validation.minLength', { count: 6 }) }),
    adminFirstName: z
      .string()
      .max(64, { message: t('validation.maxLength', { count: 64 }) })
      .optional()
      .or(z.literal("")),
    adminLastName: z
      .string()
      .max(64, { message: t('validation.maxLength', { count: 64 }) })
      .optional()
      .or(z.literal("")),
  })

// Extended schema factory for update form (includes fields not validated on server)
const createUpdateTenantFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  updateTenantSchema.extend({
    description: z
      .string()
      .max(1024, { message: t('validation.maxLength', { count: 1024 }) })
      .optional()
      .or(z.literal("")),
    note: z
      .string()
      .max(4096, { message: t('validation.maxLength', { count: 4096 }) })
      .optional()
      .or(z.literal("")),
    isActive: z.boolean().optional(),
  })

export type ProvisionTenantFormData = z.infer<ReturnType<typeof createProvisionTenantSchema>>
export type UpdateTenantFormData = z.infer<ReturnType<typeof createUpdateTenantFormSchema>>

interface TenantFormValidatedProps {
  tenant?: Tenant | null
  onSubmit: (data: ProvisionTenantFormData | UpdateTenantFormData) => Promise<void>
  onCancel: () => void
}

export const TenantFormValidated = ({ tenant, onSubmit, onCancel }: TenantFormValidatedProps) => {
  const { t } = useTranslation("common")
  const isEditing = !!tenant

  // Use the appropriate schema based on whether we're creating or editing
  const { form, handleSubmit, isSubmitting, serverError } = useValidatedForm<
    ProvisionTenantFormData | UpdateTenantFormData
  >({
    schema: isEditing ? createUpdateTenantFormSchema(t) : createProvisionTenantSchema(t),
    defaultValues: isEditing
      ? {
          tenantId: tenant.id,
          identifier: tenant.identifier,
          name: tenant.name || "",
          description: tenant.description || "",
          note: tenant.note || "",
          isActive: tenant.isActive,
        }
      : {
          identifier: "",
          name: "",
          description: "",
          note: "",
          adminEmail: "",
          adminPassword: "",
          adminFirstName: "",
          adminLastName: "",
        },
    onSubmit,
  })

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <FormError message={serverError} />

      <FormField
        form={form}
        name="identifier"
        label={t("tenants.form.identifier")}
        placeholder={t("tenants.form.identifierPlaceholder")}
        description={t("tenants.form.identifierHint")}
        required
        disabled={isSubmitting}
      />

      <FormField
        form={form}
        name="name"
        label={t("tenants.form.name")}
        placeholder={t("tenants.form.namePlaceholder")}
        required
        disabled={isSubmitting}
      />

      {/* Description and Note fields - shown in both create and edit modes */}
      <FormTextarea
        form={form}
        name="description"
        label={t("tenants.form.description", "Description")}
        placeholder={t("tenants.form.descriptionPlaceholder", "Brief description of the tenant")}
        disabled={isSubmitting}
        rows={2}
      />

      <FormTextarea
        form={form}
        name="note"
        label={t("tenants.form.note", "Internal Note")}
        placeholder={t("tenants.form.notePlaceholder", "Internal notes (not visible to tenant)")}
        disabled={isSubmitting}
        rows={2}
      />

      {/* Admin User Section - Only for create mode */}
      {!isEditing && (
        <>
          <div className="my-4 border-t border-border" />

          {/* Admin User Section - Required for all new tenants */}
          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <UserPlus className="h-4 w-4 text-muted-foreground" />
              <Label className="text-sm font-medium">
                {t("tenants.form.adminUserSection", "Tenant Administrator")}
              </Label>
            </div>
            <p className="text-xs text-muted-foreground">
              {t("tenants.form.adminUserHint", "Create the initial administrator account for this tenant")}
            </p>

            <div className="grid grid-cols-2 gap-4">
              <FormField
                form={form}
                name="adminFirstName"
                label={t("tenants.form.adminFirstName", "First Name")}
                placeholder={t("tenants.form.adminFirstNamePlaceholder", "John")}
                disabled={isSubmitting}
              />
              <FormField
                form={form}
                name="adminLastName"
                label={t("tenants.form.adminLastName", "Last Name")}
                placeholder={t("tenants.form.adminLastNamePlaceholder", "Doe")}
                disabled={isSubmitting}
              />
            </div>

            <FormField
              form={form}
              name="adminEmail"
              label={t("tenants.form.adminEmail", "Admin Email")}
              placeholder={t("tenants.form.adminEmailPlaceholder", "admin@example.com")}
              required
              disabled={isSubmitting}
            />

            <FormField
              form={form}
              name="adminPassword"
              label={t("tenants.form.adminPassword", "Admin Password")}
              placeholder={t("tenants.form.adminPasswordPlaceholder", "••••••••")}
              type="password"
              required
              disabled={isSubmitting}
            />
          </div>
        </>
      )}

      {isEditing && (
        <div className="flex items-center space-x-2">
          <input
            type="checkbox"
            id="isActive"
            {...form.register("isActive" as never)}
            disabled={isSubmitting}
            className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
          />
          <Label htmlFor="isActive">{t("labels.active")}</Label>
        </div>
      )}

      <div className="flex justify-end space-x-2 pt-4">
        <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
          {t("buttons.cancel")}
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting
            ? t("labels.loading")
            : isEditing
              ? t("buttons.update")
              : t("buttons.create")}
        </Button>
      </div>
    </form>
  )
}
