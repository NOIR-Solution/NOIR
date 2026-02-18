import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Loader2, Minus, Plus } from 'lucide-react'
import { toast } from 'sonner'
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Textarea,
} from '@uikit'
import {
  useAddLoyaltyPointsMutation,
  useRedeemLoyaltyPointsMutation,
} from '@/portal-app/customers/queries'

interface LoyaltyPointsDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  customerId: string
  customerName: string
  mode: 'add' | 'redeem'
  currentPoints: number
  onSuccess?: () => void
}

const createLoyaltySchema = (t: (key: string, options?: Record<string, unknown>) => string, mode: 'add' | 'redeem', currentPoints: number) =>
  z.object({
    points: z.number({ message: t('validation.required') })
      .int()
      .min(1, t('validation.minValue', { value: 1 }))
      .max(
        mode === 'redeem' ? currentPoints : 1000000,
        mode === 'redeem'
          ? t('customers.insufficientPoints', { available: currentPoints, defaultValue: `Maximum ${currentPoints} points available` })
          : t('validation.maxValue', { value: 1000000 })
      ),
    reason: z.string().max(500, t('validation.maxLength', { count: 500 })).optional().nullable(),
  })

type LoyaltyFormData = z.infer<ReturnType<typeof createLoyaltySchema>>

export const LoyaltyPointsDialog = ({ open, onOpenChange, customerId, customerName, mode, currentPoints, onSuccess }: LoyaltyPointsDialogProps) => {
  const { t } = useTranslation('common')
  const addMutation = useAddLoyaltyPointsMutation()
  const redeemMutation = useRedeemLoyaltyPointsMutation()

  const form = useForm<LoyaltyFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createLoyaltySchema(t, mode, currentPoints)) as unknown as Resolver<LoyaltyFormData>,
    mode: 'onBlur',
    defaultValues: {
      points: 0,
      reason: '',
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({ points: 0, reason: '' })
    }
  }, [open, form])

  const onSubmit = async (data: LoyaltyFormData) => {
    try {
      const request = {
        points: data.points,
        reason: data.reason || null,
      }

      if (mode === 'add') {
        await addMutation.mutateAsync({ id: customerId, request })
        toast.success(t('customers.pointsAddSuccess', { points: data.points, defaultValue: `${data.points} points added successfully` }))
      } else {
        await redeemMutation.mutateAsync({ id: customerId, request })
        toast.success(t('customers.pointsRedeemSuccess', { points: data.points, defaultValue: `${data.points} points redeemed successfully` }))
      }
      onSuccess?.()
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message
        : mode === 'add'
          ? t('customers.pointsAddError', 'Failed to add points')
          : t('customers.pointsRedeemError', 'Failed to redeem points')
      toast.error(message)
    }
  }

  const isSubmitting = addMutation.isPending || redeemMutation.isPending
  const isAdd = mode === 'add'

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[420px]">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className={`p-2 rounded-xl ${isAdd ? 'bg-primary/10 border border-primary/20' : 'bg-orange-500/10 border border-orange-500/20'}`}>
              {isAdd
                ? <Plus className="h-5 w-5 text-primary" />
                : <Minus className="h-5 w-5 text-orange-600 dark:text-orange-400" />
              }
            </div>
            <div>
              <DialogTitle>
                {isAdd
                  ? t('customers.addLoyaltyPoints', 'Add Loyalty Points')
                  : t('customers.redeemLoyaltyPoints', 'Redeem Loyalty Points')
                }
              </DialogTitle>
              <DialogDescription>
                {isAdd
                  ? t('customers.addPointsDescription', { name: customerName, defaultValue: `Add points to ${customerName}'s account` })
                  : t('customers.redeemPointsDescription', { name: customerName, available: currentPoints, defaultValue: `Redeem points from ${customerName}'s account (${currentPoints} available)` })
                }
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="points"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('customers.pointsAmount', 'Points')}</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      {...field}
                      onChange={(e) => field.onChange(parseInt(e.target.value) || 0)}
                      min={1}
                      max={mode === 'redeem' ? currentPoints : undefined}
                      placeholder={t('customers.pointsPlaceholder', 'Enter points amount')}
                    />
                  </FormControl>
                  {mode === 'redeem' && (
                    <FormDescription className="text-xs">
                      {t('customers.availablePoints', { count: currentPoints, defaultValue: `${currentPoints} points available` })}
                    </FormDescription>
                  )}
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="reason"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>{t('customers.pointsReason', 'Reason (optional)')}</FormLabel>
                  <FormControl>
                    <Textarea
                      {...field}
                      value={field.value ?? ''}
                      placeholder={t('customers.pointsReasonPlaceholder', 'Enter reason for this adjustment...')}
                      rows={2}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                className="cursor-pointer"
              >
                {t('labels.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={isSubmitting} className="cursor-pointer">
                {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isAdd
                  ? <><Plus className="h-4 w-4 mr-1" />{t('customers.addPoints', 'Add Points')}</>
                  : <><Minus className="h-4 w-4 mr-1" />{t('customers.redeemPoints', 'Redeem Points')}</>
                }
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
