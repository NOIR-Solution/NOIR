import { useState, useTransition } from 'react'
import { useTranslation } from 'react-i18next'
import { useParams, useNavigate } from 'react-router-dom'
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  Crown,
  Eye,
  Gift,
  Mail,
  MapPin,
  Minus,
  Pencil,
  Phone,
  Plus,
  ShoppingCart,
  Star,
  Trash2,
  User,
  Users,
} from 'lucide-react'
import { toast } from 'sonner'
import { usePageContext } from '@/hooks/usePageContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  PageHeader,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Separator,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@uikit'
import {
  useCustomerQuery,
  useCustomerOrdersQuery,
  useUpdateCustomerSegmentMutation,
} from '@/portal-app/customers/queries'
import type { CustomerSegment, CustomerAddressDto } from '@/types/customer'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { CustomerFormDialog } from '../../components/CustomerFormDialog'
import { AddressFormDialog } from '../../components/AddressFormDialog'
import { LoyaltyPointsDialog } from '../../components/LoyaltyPointsDialog'
import { DeleteCustomerDialog } from '../../components/DeleteCustomerDialog'

const CUSTOMER_SEGMENTS: CustomerSegment[] = ['New', 'Active', 'AtRisk', 'Dormant', 'Lost', 'VIP']

const getSegmentBadgeClass = (segment: CustomerSegment): string => {
  switch (segment) {
    case 'New': return 'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400 dark:border-blue-800'
    case 'Active': return 'bg-green-100 text-green-800 border-green-200 dark:bg-green-900/30 dark:text-green-400 dark:border-green-800'
    case 'VIP': return 'bg-purple-100 text-purple-800 border-purple-200 dark:bg-purple-900/30 dark:text-purple-400 dark:border-purple-800'
    case 'AtRisk': return 'bg-orange-100 text-orange-800 border-orange-200 dark:bg-orange-900/30 dark:text-orange-400 dark:border-orange-800'
    case 'Dormant': return 'bg-yellow-100 text-yellow-800 border-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-400 dark:border-yellow-800'
    case 'Lost': return 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-400 dark:border-red-800'
    default: return 'bg-gray-100 text-gray-800 border-gray-200'
  }
}

const getTierBadgeClass = (tier: string): string => {
  switch (tier) {
    case 'Standard': return 'bg-slate-100 text-slate-700 border-slate-200 dark:bg-slate-900/30 dark:text-slate-400 dark:border-slate-800'
    case 'Silver': return 'bg-gray-100 text-gray-700 border-gray-300 dark:bg-gray-900/30 dark:text-gray-300 dark:border-gray-700'
    case 'Gold': return 'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-900/30 dark:text-amber-400 dark:border-amber-800'
    case 'Platinum': return 'bg-cyan-100 text-cyan-800 border-cyan-200 dark:bg-cyan-900/30 dark:text-cyan-400 dark:border-cyan-800'
    case 'Diamond': return 'bg-violet-100 text-violet-800 border-violet-200 dark:bg-violet-900/30 dark:text-violet-400 dark:border-violet-800'
    default: return 'bg-gray-100 text-gray-800 border-gray-200'
  }
}

export const CustomerDetailPage = () => {
  const { t } = useTranslation('common')
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { hasPermission } = usePermissions()
  const { formatDateTime } = useRegionalSettings()
  usePageContext('Customers')

  const canUpdate = hasPermission(Permissions.CustomersUpdate)
  const canDelete = hasPermission(Permissions.CustomersDelete)
  const canManage = hasPermission(Permissions.CustomersManage)

  const { data: customer, isLoading, error: queryError } = useCustomerQuery(id)
  const updateSegmentMutation = useUpdateCustomerSegmentMutation()

  // Orders pagination
  const [orderParams, setOrderParams] = useState({ page: 1, pageSize: 10 })
  const [isOrderPending, startOrderTransition] = useTransition()
  const { data: ordersResponse, isLoading: ordersLoading } = useCustomerOrdersQuery(id, orderParams)
  const orders = ordersResponse?.items ?? []
  const ordersTotalPages = ordersResponse?.totalPages ?? 1
  const ordersCurrentPage = orderParams.page

  // Dialog states
  const [showEditDialog, setShowEditDialog] = useState(false)
  const [showDeleteDialog, setShowDeleteDialog] = useState(false)
  const [showAddressDialog, setShowAddressDialog] = useState(false)
  const [addressToEdit, setAddressToEdit] = useState<CustomerAddressDto | null>(null)
  const [loyaltyMode, setLoyaltyMode] = useState<'add' | 'redeem' | null>(null)

  const handleSegmentChange = async (segment: string) => {
    if (!customer) return
    try {
      await updateSegmentMutation.mutateAsync({
        id: customer.id,
        request: { segment: segment as CustomerSegment },
      })
      toast.success(t('customers.segmentUpdateSuccess', 'Customer segment updated'))
    } catch (err) {
      const message = err instanceof Error ? err.message : t('customers.segmentUpdateError', 'Failed to update segment')
      toast.error(message)
    }
  }

  const handleOrderPageChange = (page: number) => {
    startOrderTransition(() => {
      setOrderParams(prev => ({ ...prev, page }))
    })
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-10 w-10 rounded" />
          <div className="space-y-2">
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-72" />
          </div>
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <Skeleton className="h-64 w-full rounded-lg" />
            <Skeleton className="h-48 w-full rounded-lg" />
          </div>
          <div className="space-y-6">
            <Skeleton className="h-48 w-full rounded-lg" />
            <Skeleton className="h-48 w-full rounded-lg" />
          </div>
        </div>
      </div>
    )
  }

  if (queryError || !customer) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" onClick={() => navigate('/portal/ecommerce/customers')} className="cursor-pointer">
          <ArrowLeft className="h-4 w-4 mr-2" />
          {t('customers.backToCustomers', 'Back to Customers')}
        </Button>
        <div className="p-8 text-center">
          <p className="text-destructive">{queryError?.message || t('customers.customerNotFound', 'Customer not found')}</p>
        </div>
      </div>
    )
  }

  const fullName = `${customer.firstName} ${customer.lastName}`

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate('/portal/ecommerce/customers')} className="cursor-pointer" aria-label={t('customers.backToCustomers', 'Back to Customers')}>
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <PageHeader
          icon={Users}
          title={fullName}
          description={customer.email}
          responsive
          action={
            <div className="flex items-center gap-2">
              <Badge variant="outline" className={`text-sm px-3 py-1 ${getSegmentBadgeClass(customer.segment)}`}>
                {t(`customers.segment.${customer.segment.toLowerCase()}`, customer.segment)}
              </Badge>
              <Badge variant="outline" className={`text-sm px-3 py-1 ${getTierBadgeClass(customer.tier)}`}>
                {t(`customers.tier.${customer.tier.toLowerCase()}`, customer.tier)}
              </Badge>
            </div>
          }
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column - Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Stats Cards */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <Card className="shadow-sm">
              <CardContent className="p-4 text-center">
                <p className="text-sm text-muted-foreground">{t('customers.totalOrders', 'Total Orders')}</p>
                <p className="text-2xl font-bold">{customer.totalOrders}</p>
              </CardContent>
            </Card>
            <Card className="shadow-sm">
              <CardContent className="p-4 text-center">
                <p className="text-sm text-muted-foreground">{t('customers.totalSpent', 'Total Spent')}</p>
                <p className="text-2xl font-bold">{formatCurrency(customer.totalSpent, 'VND')}</p>
              </CardContent>
            </Card>
            <Card className="shadow-sm">
              <CardContent className="p-4 text-center">
                <p className="text-sm text-muted-foreground">{t('customers.avgOrderValue', 'Avg Order')}</p>
                <p className="text-2xl font-bold">{formatCurrency(customer.averageOrderValue, 'VND')}</p>
              </CardContent>
            </Card>
            <Card className="shadow-sm">
              <CardContent className="p-4 text-center">
                <p className="text-sm text-muted-foreground">{t('customers.loyaltyPoints', 'Points')}</p>
                <p className="text-2xl font-bold">{customer.loyaltyPoints.toLocaleString()}</p>
              </CardContent>
            </Card>
          </div>

          {/* Tabs */}
          <Tabs defaultValue="orders" className="w-full">
            <TabsList>
              <TabsTrigger value="orders" className="cursor-pointer">
                <ShoppingCart className="h-4 w-4 mr-2" />
                {t('customers.ordersTab', 'Orders')}
              </TabsTrigger>
              <TabsTrigger value="addresses" className="cursor-pointer">
                <MapPin className="h-4 w-4 mr-2" />
                {t('customers.addressesTab', 'Addresses')}
              </TabsTrigger>
            </TabsList>

            {/* Orders Tab */}
            <TabsContent value="orders">
              <Card className="shadow-sm">
                <CardHeader className="pb-3">
                  <CardTitle className="text-sm flex items-center gap-2">
                    <ShoppingCart className="h-4 w-4" />
                    {t('customers.orderHistory', 'Order History')}
                  </CardTitle>
                  <CardDescription>
                    {t('customers.orderCount', { count: ordersResponse?.totalCount ?? 0, defaultValue: `${ordersResponse?.totalCount ?? 0} orders` })}
                  </CardDescription>
                </CardHeader>
                <CardContent className={isOrderPending ? 'opacity-70 transition-opacity duration-200' : 'transition-opacity duration-200'}>
                  <div className="rounded-lg border overflow-hidden">
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>{t('orders.orderNumber', 'Order #')}</TableHead>
                          <TableHead>{t('labels.status', 'Status')}</TableHead>
                          <TableHead className="text-center">{t('orders.items', 'Items')}</TableHead>
                          <TableHead className="text-right">{t('orders.total', 'Total')}</TableHead>
                          <TableHead>{t('labels.date', 'Date')}</TableHead>
                          <TableHead className="text-right">{t('labels.actions', 'Actions')}</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {ordersLoading ? (
                          [...Array(3)].map((_, i) => (
                            <TableRow key={i} className="animate-pulse">
                              <TableCell><Skeleton className="h-4 w-24" /></TableCell>
                              <TableCell><Skeleton className="h-5 w-20 rounded-full" /></TableCell>
                              <TableCell className="text-center"><Skeleton className="h-5 w-8 mx-auto" /></TableCell>
                              <TableCell className="text-right"><Skeleton className="h-4 w-24 ml-auto" /></TableCell>
                              <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                              <TableCell className="text-right"><Skeleton className="h-8 w-8 ml-auto" /></TableCell>
                            </TableRow>
                          ))
                        ) : orders.length === 0 ? (
                          <TableRow>
                            <TableCell colSpan={6} className="p-0">
                              <EmptyState
                                icon={ShoppingCart}
                                title={t('customers.noOrders', 'No orders yet')}
                                description={t('customers.noOrdersDescription', 'Orders will appear here when this customer places them.')}
                                className="border-0 rounded-none px-4 py-12"
                              />
                            </TableCell>
                          </TableRow>
                        ) : (
                          orders.map((order) => (
                            <TableRow key={order.id} className="group transition-colors hover:bg-muted/50">
                              <TableCell>
                                <span className="font-mono font-medium text-sm">{order.orderNumber}</span>
                              </TableCell>
                              <TableCell>
                                <Badge variant="outline">{order.status}</Badge>
                              </TableCell>
                              <TableCell className="text-center">
                                <Badge variant="secondary">{order.itemCount}</Badge>
                              </TableCell>
                              <TableCell className="text-right">
                                <span className="font-medium">{formatCurrency(order.grandTotal, order.currency)}</span>
                              </TableCell>
                              <TableCell>
                                <span className="text-sm text-muted-foreground">{formatDateTime(order.createdAt)}</span>
                              </TableCell>
                              <TableCell className="text-right">
                                <Button
                                  variant="ghost"
                                  size="sm"
                                  className="cursor-pointer h-9 w-9 p-0 transition-all duration-200 hover:bg-primary/10 hover:text-primary"
                                  aria-label={t('orders.viewOrder', { orderNumber: order.orderNumber, defaultValue: `View order ${order.orderNumber}` })}
                                  onClick={() => navigate(`/portal/ecommerce/orders/${order.id}`)}
                                >
                                  <Eye className="h-4 w-4" />
                                </Button>
                              </TableCell>
                            </TableRow>
                          ))
                        )}
                      </TableBody>
                    </Table>
                  </div>

                  {ordersTotalPages > 1 && (
                    <div className="flex items-center justify-between mt-4">
                      <p className="text-sm text-muted-foreground">
                        {t('labels.pageOf', { current: ordersCurrentPage, total: ordersTotalPages, defaultValue: `Page ${ordersCurrentPage} of ${ordersTotalPages}` })}
                      </p>
                      <div className="flex items-center gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          className="cursor-pointer"
                          disabled={ordersCurrentPage <= 1}
                          onClick={() => handleOrderPageChange(ordersCurrentPage - 1)}
                          aria-label={t('labels.previousPage', 'Previous page')}
                        >
                          <ChevronLeft className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          className="cursor-pointer"
                          disabled={ordersCurrentPage >= ordersTotalPages}
                          onClick={() => handleOrderPageChange(ordersCurrentPage + 1)}
                          aria-label={t('labels.nextPage', 'Next page')}
                        >
                          <ChevronRight className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
            </TabsContent>

            {/* Addresses Tab */}
            <TabsContent value="addresses">
              <Card className="shadow-sm">
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <div>
                      <CardTitle className="text-sm flex items-center gap-2">
                        <MapPin className="h-4 w-4" />
                        {t('customers.addresses', 'Addresses')}
                      </CardTitle>
                      <CardDescription>
                        {t('customers.addressCount', { count: customer.addresses.length, defaultValue: `${customer.addresses.length} addresses` })}
                      </CardDescription>
                    </div>
                    {canUpdate && (
                      <Button
                        size="sm"
                        className="cursor-pointer"
                        onClick={() => {
                          setAddressToEdit(null)
                          setShowAddressDialog(true)
                        }}
                      >
                        <Plus className="h-4 w-4 mr-1" />
                        {t('customers.addAddress', 'Add Address')}
                      </Button>
                    )}
                  </div>
                </CardHeader>
                <CardContent>
                  {customer.addresses.length === 0 ? (
                    <EmptyState
                      icon={MapPin}
                      title={t('customers.noAddresses', 'No addresses')}
                      description={t('customers.noAddressesDescription', 'Add a shipping or billing address for this customer.')}
                      action={canUpdate ? {
                        label: t('customers.addAddress', 'Add Address'),
                        onClick: () => {
                          setAddressToEdit(null)
                          setShowAddressDialog(true)
                        },
                      } : undefined}
                      className="border-0 rounded-none px-4 py-12"
                    />
                  ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      {customer.addresses.map((address) => (
                        <Card key={address.id} className="relative shadow-sm">
                          <CardHeader className="pb-2">
                            <div className="flex items-center justify-between">
                              <div className="flex items-center gap-2">
                                <Badge variant="outline">
                                  {t(`customers.addressType.${address.addressType.toLowerCase()}`, address.addressType)}
                                </Badge>
                                {address.isDefault && (
                                  <Badge variant="secondary" className="text-xs">
                                    {t('customers.defaultAddress', 'Default')}
                                  </Badge>
                                )}
                              </div>
                              {canUpdate && (
                                <div className="flex items-center gap-1">
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="cursor-pointer h-8 w-8 p-0"
                                    aria-label={t('customers.editAddress', { name: address.fullName, defaultValue: `Edit address for ${address.fullName}` })}
                                    onClick={() => {
                                      setAddressToEdit(address)
                                      setShowAddressDialog(true)
                                    }}
                                  >
                                    <Pencil className="h-3.5 w-3.5" />
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="cursor-pointer h-8 w-8 p-0 text-destructive hover:text-destructive"
                                    aria-label={t('customers.deleteAddress', { name: address.fullName, defaultValue: `Delete address for ${address.fullName}` })}
                                    onClick={() => {
                                      // Address deletion is handled inline
                                      setAddressToEdit(address)
                                    }}
                                  >
                                    <Trash2 className="h-3.5 w-3.5" />
                                  </Button>
                                </div>
                              )}
                            </div>
                          </CardHeader>
                          <CardContent className="space-y-1 text-sm">
                            <p className="font-medium">{address.fullName}</p>
                            <p className="text-muted-foreground">{address.phone}</p>
                            <p className="text-muted-foreground">{address.addressLine1}</p>
                            {address.addressLine2 && <p className="text-muted-foreground">{address.addressLine2}</p>}
                            <p className="text-muted-foreground">
                              {[address.ward, address.district, address.province].filter(Boolean).join(', ')}
                            </p>
                            {address.postalCode && <p className="text-muted-foreground">{address.postalCode}</p>}
                          </CardContent>
                        </Card>
                      ))}
                    </div>
                  )}
                </CardContent>
              </Card>
            </TabsContent>
          </Tabs>
        </div>

        {/* Right Column - Customer Info & Actions */}
        <div className="space-y-6">
          {/* Customer Info */}
          <Card className="shadow-sm">
            <CardHeader className="pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="text-sm">{t('customers.customerInfo', 'Customer Information')}</CardTitle>
                <div className="flex items-center gap-1">
                  {canUpdate && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="cursor-pointer h-8 w-8 p-0"
                      aria-label={t('customers.editCustomer', { name: fullName, defaultValue: `Edit ${fullName}` })}
                      onClick={() => setShowEditDialog(true)}
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </Button>
                  )}
                  {canDelete && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="cursor-pointer h-8 w-8 p-0 text-destructive hover:text-destructive"
                      aria-label={t('customers.deleteCustomer', { name: fullName, defaultValue: `Delete ${fullName}` })}
                      onClick={() => setShowDeleteDialog(true)}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  )}
                </div>
              </div>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium">{fullName}</span>
              </div>
              <div className="flex items-center gap-2">
                <Mail className="h-4 w-4 text-muted-foreground" />
                <span>{customer.email}</span>
              </div>
              {customer.phone && (
                <div className="flex items-center gap-2">
                  <Phone className="h-4 w-4 text-muted-foreground" />
                  <span>{customer.phone}</span>
                </div>
              )}
              <Separator />
              <div>
                <p className="text-muted-foreground text-xs mb-1">{t('labels.status', 'Status')}</p>
                <Badge variant={customer.isActive ? 'default' : 'secondary'}>
                  {customer.isActive ? t('labels.active', 'Active') : t('labels.inactive', 'Inactive')}
                </Badge>
              </div>
              <div>
                <p className="text-muted-foreground text-xs mb-1">{t('labels.createdAt', 'Created At')}</p>
                <p className="font-medium">{formatDateTime(customer.createdAt)}</p>
              </div>
              {customer.lastOrderDate && (
                <div>
                  <p className="text-muted-foreground text-xs mb-1">{t('customers.lastOrderDate', 'Last Order Date')}</p>
                  <p className="font-medium">{formatDateTime(customer.lastOrderDate)}</p>
                </div>
              )}
              {customer.tags && (
                <div>
                  <p className="text-muted-foreground text-xs mb-1">{t('labels.tags', 'Tags')}</p>
                  <div className="flex flex-wrap gap-1">
                    {customer.tags.split(',').map((tag) => (
                      <Badge key={tag.trim()} variant="outline" className="text-xs">
                        {tag.trim()}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}
              {customer.notes && (
                <div>
                  <p className="text-muted-foreground text-xs mb-1">{t('labels.notes', 'Notes')}</p>
                  <p className="text-sm whitespace-pre-wrap">{customer.notes}</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Segment Management */}
          {canManage && (
            <Card className="shadow-sm">
              <CardHeader className="pb-3">
                <CardTitle className="text-sm flex items-center gap-2">
                  <Crown className="h-4 w-4" />
                  {t('customers.segmentManagement', 'Segment Management')}
                </CardTitle>
                <CardDescription className="text-xs">
                  {t('customers.segmentManagementDescription', 'Manually override the customer segment')}
                </CardDescription>
              </CardHeader>
              <CardContent>
                <Select
                  value={customer.segment}
                  onValueChange={handleSegmentChange}
                  disabled={updateSegmentMutation.isPending}
                >
                  <SelectTrigger className="cursor-pointer">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {CUSTOMER_SEGMENTS.map((segment) => (
                      <SelectItem key={segment} value={segment} className="cursor-pointer">
                        {t(`customers.segment.${segment.toLowerCase()}`, segment)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </CardContent>
            </Card>
          )}

          {/* Loyalty Points */}
          <Card className="shadow-sm">
            <CardHeader className="pb-3">
              <CardTitle className="text-sm flex items-center gap-2">
                <Gift className="h-4 w-4" />
                {t('customers.loyalty', 'Loyalty Points')}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-sm text-muted-foreground">{t('customers.currentPoints', 'Current Points')}</span>
                <span className="text-lg font-bold">{customer.loyaltyPoints.toLocaleString()}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-muted-foreground">{t('customers.lifetimePoints', 'Lifetime Points')}</span>
                <span className="text-sm font-medium">{customer.lifetimeLoyaltyPoints.toLocaleString()}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-muted-foreground">{t('customers.tierLabel', 'Tier')}</span>
                <Badge variant="outline" className={getTierBadgeClass(customer.tier)}>
                  <Star className="h-3 w-3 mr-1" />
                  {t(`customers.tier.${customer.tier.toLowerCase()}`, customer.tier)}
                </Badge>
              </div>
              {canManage && (
                <>
                  <Separator />
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      className="flex-1 cursor-pointer"
                      onClick={() => setLoyaltyMode('add')}
                    >
                      <Plus className="h-4 w-4 mr-1" />
                      {t('customers.addPoints', 'Add')}
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      className="flex-1 cursor-pointer"
                      onClick={() => setLoyaltyMode('redeem')}
                      disabled={customer.loyaltyPoints <= 0}
                    >
                      <Minus className="h-4 w-4 mr-1" />
                      {t('customers.redeemPoints', 'Redeem')}
                    </Button>
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Edit Customer Dialog */}
      <CustomerFormDialog
        open={showEditDialog}
        onOpenChange={setShowEditDialog}
        customer={customer}
      />

      {/* Delete Customer Dialog */}
      <DeleteCustomerDialog
        open={showDeleteDialog}
        onOpenChange={setShowDeleteDialog}
        customer={customer}
        onSuccess={() => navigate('/portal/ecommerce/customers')}
      />

      {/* Address Form Dialog */}
      <AddressFormDialog
        open={showAddressDialog}
        onOpenChange={(open) => {
          setShowAddressDialog(open)
          if (!open) setAddressToEdit(null)
        }}
        customerId={customer.id}
        address={addressToEdit}
      />

      {/* Loyalty Points Dialog */}
      <LoyaltyPointsDialog
        open={loyaltyMode !== null}
        onOpenChange={(open) => !open && setLoyaltyMode(null)}
        customerId={customer.id}
        customerName={fullName}
        mode={loyaltyMode ?? 'add'}
        currentPoints={customer.loyaltyPoints}
      />
    </div>
  )
}

export default CustomerDetailPage
