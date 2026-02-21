import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { toast } from 'sonner'
import { CreditCard, Loader2 } from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Textarea,
} from '@uikit'
import { useRecordManualPaymentMutation } from '@/portal-app/payments/queries'
import type { PaymentMethod } from '@/services/payments'

const PAYMENT_METHODS: PaymentMethod[] = [
  'BankTransfer', 'COD', 'CreditCard', 'DebitCard',
  'EWallet', 'QRCode', 'Installment', 'BuyNowPayLater',
]

const createRecordPaymentSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  z.object({
    orderId: z.string().min(1, t('validation.required')).uuid(t('payments.recordPayment.invalidOrderId')),
    amount: z.number({ error: t('validation.required') }).positive(t('validation.required')),
    currency: z.string().min(1, t('validation.required')).default('VND'),
    paymentMethod: z.string().min(1, t('validation.required')),
    referenceNumber: z.string().optional(),
    notes: z.string().optional(),
  })

type RecordPaymentFormData = z.infer<ReturnType<typeof createRecordPaymentSchema>>

interface RecordManualPaymentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess?: () => void
}

export const RecordManualPaymentDialog = ({ open, onOpenChange, onSuccess }: RecordManualPaymentDialogProps) => {
  const { t } = useTranslation('common')
  const recordMutation = useRecordManualPaymentMutation()

  const form = useForm<RecordPaymentFormData>({
    // TypeScript cannot infer resolver types from dynamic schema factories
    // Using 'as unknown as Resolver<T>' for type-safe assertion
    resolver: zodResolver(createRecordPaymentSchema(t)) as unknown as Resolver<RecordPaymentFormData>,
    mode: 'onBlur',
    defaultValues: {
      orderId: '',
      amount: undefined as unknown as number,
      currency: 'VND',
      paymentMethod: '',
      referenceNumber: '',
      notes: '',
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        orderId: '',
        amount: undefined as unknown as number,
        currency: 'VND',
        paymentMethod: '',
        referenceNumber: '',
        notes: '',
      })
    }
  }, [open, form])

  const onSubmit = async (data: RecordPaymentFormData) => {
    try {
      await recordMutation.mutateAsync({
        orderId: data.orderId,
        amount: data.amount,
        currency: data.currency,
        paymentMethod: data.paymentMethod as PaymentMethod,
        referenceNumber: data.referenceNumber || undefined,
        notes: data.notes || undefined,
      })
      toast.success(t('payments.recordPayment.success'))
      onSuccess?.()
      onOpenChange(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('payments.recordPayment.error')
      toast.error(message)
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[500px]">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <CreditCard className="h-5 w-5 text-primary" />
            </div>
            <div>
              <CredenzaTitle>{t('payments.recordPayment.title')}</CredenzaTitle>
              <CredenzaDescription>{t('payments.recordPayment.description')}</CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <CredenzaBody>
              <FormField
                control={form.control}
                name="orderId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('payments.recordPayment.orderId')}</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        placeholder={t('payments.recordPayment.orderIdPlaceholder')}
                        className="font-mono text-sm"
                      />
                    </FormControl>
                    <FormDescription>{t('payments.recordPayment.orderIdHint')}</FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="grid grid-cols-2 gap-4">
                <FormField
                  control={form.control}
                  name="amount"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('payments.recordPayment.amount')}</FormLabel>
                      <FormControl>
                        <Input
                          type="number"
                          {...field}
                          value={field.value ?? ''}
                          onChange={(e) => field.onChange(e.target.value ? parseFloat(e.target.value) : undefined)}
                          placeholder={t('payments.recordPayment.amountPlaceholder')}
                          min={0}
                          step="any"
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="currency"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>{t('payments.recordPayment.currency')}</FormLabel>
                      <FormControl>
                        <Input {...field} placeholder="VND" />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>

              <FormField
                control={form.control}
                name="paymentMethod"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('payments.recordPayment.paymentMethod')}</FormLabel>
                    <Select value={field.value} onValueChange={field.onChange}>
                      <FormControl>
                        <SelectTrigger className="cursor-pointer">
                          <SelectValue placeholder={t('payments.recordPayment.selectPaymentMethod')} />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {PAYMENT_METHODS.map((method) => (
                          <SelectItem key={method} value={method} className="cursor-pointer">
                            {t(`payments.methods.${method}`, method)}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="referenceNumber"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('payments.recordPayment.referenceNumber')}</FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        placeholder={t('payments.recordPayment.referenceNumberPlaceholder')}
                      />
                    </FormControl>
                    <FormDescription>{t('payments.recordPayment.referenceNumberHint')}</FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="notes"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t('payments.recordPayment.notes')}</FormLabel>
                    <FormControl>
                      <Textarea
                        {...field}
                        placeholder={t('payments.recordPayment.notesPlaceholder')}
                        rows={3}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </CredenzaBody>

            <CredenzaFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                className="cursor-pointer"
              >
                {t('labels.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={recordMutation.isPending} className="cursor-pointer">
                {recordMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {t('payments.recordPayment.submit')}
              </Button>
            </CredenzaFooter>
          </form>
        </Form>
      </CredenzaContent>
    </Credenza>
  )
}
