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
import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { FormField, FormError } from "@/components/ui/form-field"
import { useValidatedForm } from "@/hooks/useValidatedForm"
import { createTenantSchema, updateTenantSchema } from "@/validation/schemas.generated"
import type { Tenant } from "@/types"
import { z } from "zod"

// Extended schema for update form (includes isActive which isn't validated on server)
const updateTenantFormSchema = updateTenantSchema.extend({
  isActive: z.boolean().optional(),
})

export type CreateTenantFormData = z.infer<typeof createTenantSchema>
export type UpdateTenantFormData = z.infer<typeof updateTenantFormSchema>

interface TenantFormValidatedProps {
  tenant?: Tenant | null
  onSubmit: (data: CreateTenantFormData | UpdateTenantFormData) => Promise<void>
  onCancel: () => void
}

export function TenantFormValidated({ tenant, onSubmit, onCancel }: TenantFormValidatedProps) {
  const { t } = useTranslation("common")
  const isEditing = !!tenant

  // Use the appropriate schema based on whether we're creating or editing
  const { form, handleSubmit, isSubmitting, serverError } = useValidatedForm<
    CreateTenantFormData | UpdateTenantFormData
  >({
    schema: isEditing ? updateTenantFormSchema : createTenantSchema,
    defaultValues: isEditing
      ? {
          tenantId: tenant.id,
          identifier: tenant.identifier,
          name: tenant.name || "",
          isActive: tenant.isActive,
        }
      : {
          identifier: "",
          name: "",
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
