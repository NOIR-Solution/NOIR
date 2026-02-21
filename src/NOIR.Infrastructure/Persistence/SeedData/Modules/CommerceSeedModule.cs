namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Seeds commerce data: customers, orders (with lifecycle transitions), and inventory receipts.
/// Order: 300 (depends on CatalogSeedModule at 100 for product/variant lookups).
/// </summary>
public class CommerceSeedModule : ISeedDataModule
{
    public int Order => 300;
    public string ModuleName => "Commerce";

    public async Task SeedAsync(SeedDataContext context, CancellationToken ct = default)
    {
        var tenantId = context.CurrentTenant.Id;

        // Idempotency: skip if customers already exist for this tenant
        var hasData = await context.DbContext.Set<Customer>()
            .IgnoreQueryFilters()
            .TagWith("SeedData:CheckCommerce")
            .AnyAsync(c => c.TenantId == tenantId, ct);

        if (hasData)
        {
            context.Logger.LogInformation("[SeedData] Commerce already seeded for {Tenant}", tenantId);
            return;
        }

        var addresses = VietnameseAddresses.GetAddresses();
        var customerDefs = CommerceData.GetCustomers();
        var orderDefs = CommerceData.GetOrders();
        var receiptDefs = CommerceData.GetReceipts();

        // 1. Seed Customers + Addresses
        var customerEntities = SeedCustomers(context, customerDefs, addresses, tenantId);
        await context.DbContext.SaveChangesAsync(ct);

        // 2. Load product catalog for order item resolution
        var productLookup = await BuildProductLookupAsync(context, tenantId, ct);

        // 3. Seed Orders with lifecycle transitions
        SeedOrders(context, orderDefs, customerEntities, addresses, productLookup, tenantId);
        await context.DbContext.SaveChangesAsync(ct);

        // 4. Seed Inventory Receipts
        SeedInventoryReceipts(context, receiptDefs, productLookup, tenantId);
        await context.DbContext.SaveChangesAsync(ct);

        context.Logger.LogInformation(
            "[SeedData] Commerce: {Customers} customers, {Orders} orders, {Receipts} receipts",
            customerDefs.Length, orderDefs.Length, receiptDefs.Length);
    }

    private static List<(Guid Id, string Email, string FullName, string? Phone)> SeedCustomers(
        SeedDataContext context,
        CustomerDef[] customerDefs,
        AddressDef[] addresses,
        string tenantId)
    {
        var result = new List<(Guid Id, string Email, string FullName, string? Phone)>();

        foreach (var def in customerDefs)
        {
            var customerId = SeedDataConstants.TenantGuid(tenantId, $"customer:{def.Email}");
            var customer = Customer.Create(
                userId: null,
                email: def.Email,
                firstName: def.FirstName,
                lastName: def.LastName,
                phone: def.Phone,
                tenantId: tenantId);
            SeedDataConstants.SetEntityId(customer, customerId);

            context.DbContext.Set<Customer>().Add(customer);

            // Add a default shipping address from the address pool
            var addr = addresses[def.AddressIndex % addresses.Length];
            var customerAddress = CustomerAddress.Create(
                customerId: customer.Id,
                addressType: AddressType.Both,
                fullName: addr.FullName,
                phone: addr.Phone,
                addressLine1: addr.AddressLine1,
                province: addr.Province,
                ward: addr.Ward,
                district: addr.District,
                isDefault: true,
                tenantId: tenantId);

            context.DbContext.Set<CustomerAddress>().Add(customerAddress);

            result.Add((customer.Id, def.Email, $"{def.FirstName} {def.LastName}", def.Phone));
        }

        return result;
    }

    private static async Task<Dictionary<string, (Product Product, List<ProductVariant> Variants)>> BuildProductLookupAsync(
        SeedDataContext context,
        string tenantId,
        CancellationToken ct)
    {
        var products = await context.DbContext.Set<Product>()
            .IgnoreQueryFilters()
            .TagWith("SeedData:GetProductsForCommerce")
            .Where(p => p.TenantId == tenantId)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsSplitQuery()
            .ToListAsync(ct);

        return products.ToDictionary(
            p => p.Slug,
            p => (p, p.Variants.ToList()),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void SeedOrders(
        SeedDataContext context,
        OrderDef[] orderDefs,
        List<(Guid Id, string Email, string FullName, string? Phone)> customers,
        AddressDef[] addresses,
        Dictionary<string, (Product Product, List<ProductVariant> Variants)> productLookup,
        string tenantId)
    {
        foreach (var def in orderDefs)
        {
            var customer = customers[def.CustomerIndex % customers.Count];
            var addr = addresses[def.CustomerIndex % addresses.Length];

            // Calculate subtotal from items
            var subTotal = def.Items.Sum(i => i.UnitPrice * i.Quantity);

            var orderId = SeedDataConstants.TenantGuid(tenantId, $"order:{def.OrderNumber}");
            var order = NOIR.Domain.Entities.Order.Order.Create(
                orderNumber: def.OrderNumber,
                customerEmail: customer.Email,
                subTotal: subTotal,
                grandTotal: subTotal,
                currency: "VND",
                tenantId: tenantId);
            SeedDataConstants.SetEntityId(order, orderId);

            order.SetCustomerInfo(customer.Id, customer.FullName, customer.Phone);

            order.SetShippingAddress(new Address
            {
                FullName = addr.FullName,
                Phone = addr.Phone,
                AddressLine1 = addr.AddressLine1,
                Ward = addr.Ward,
                District = addr.District,
                Province = addr.Province,
                Country = "Vietnam",
                IsDefault = true
            });

            // Add order items, resolving product/variant by slug
            foreach (var item in def.Items)
            {
                if (!productLookup.TryGetValue(item.ProductSlug, out var entry))
                {
                    context.Logger.LogWarning(
                        "[SeedData] Product slug '{Slug}' not found for order {OrderNumber}. Skipping item.",
                        item.ProductSlug, def.OrderNumber);
                    continue;
                }

                var (product, variants) = entry;
                var variant = variants.FirstOrDefault(v => v.Name == item.VariantName)
                              ?? variants.FirstOrDefault();

                if (variant == null)
                {
                    context.Logger.LogWarning(
                        "[SeedData] No variants for product '{Slug}' in order {OrderNumber}. Skipping item.",
                        item.ProductSlug, def.OrderNumber);
                    continue;
                }

                var imageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url
                               ?? product.Images.FirstOrDefault()?.Url;

                order.AddItem(
                    productId: product.Id,
                    productVariantId: variant.Id,
                    productName: product.Name,
                    variantName: variant.Name,
                    unitPrice: item.UnitPrice,
                    quantity: item.Quantity,
                    sku: variant.Sku,
                    imageUrl: imageUrl);
            }

            // Apply lifecycle transitions based on target status
            ApplyOrderLifecycle(order, def);

            context.DbContext.Set<NOIR.Domain.Entities.Order.Order>().Add(order);
        }
    }

    /// <summary>
    /// Applies sequential state machine transitions to reach the target status.
    /// Must follow: Pending -> Confirmed -> Processing -> Shipped -> Delivered -> Completed.
    /// </summary>
    private static void ApplyOrderLifecycle(NOIR.Domain.Entities.Order.Order order, OrderDef def)
    {
        switch (def.TargetStatus)
        {
            case OrderStatus.Pending:
                // Default status, no action needed
                break;

            case OrderStatus.Confirmed:
                order.Confirm();
                break;

            case OrderStatus.Processing:
                order.Confirm();
                order.StartProcessing();
                break;

            case OrderStatus.Shipped:
                order.Confirm();
                order.StartProcessing();
                order.Ship($"TN{def.OrderNumber[4..]}", "GHTK");
                break;

            case OrderStatus.Delivered:
                order.Confirm();
                order.StartProcessing();
                order.Ship($"TN{def.OrderNumber[4..]}", "GHN");
                order.MarkAsDelivered();
                break;

            case OrderStatus.Completed:
                order.Confirm();
                order.StartProcessing();
                order.Ship($"TN{def.OrderNumber[4..]}", "GHTK");
                order.MarkAsDelivered();
                order.Complete();
                break;

            case OrderStatus.Cancelled:
                order.Cancel(def.CancellationReason);
                break;
        }
    }

    private static void SeedInventoryReceipts(
        SeedDataContext context,
        ReceiptDef[] receiptDefs,
        Dictionary<string, (Product Product, List<ProductVariant> Variants)> productLookup,
        string tenantId)
    {
        foreach (var def in receiptDefs)
        {
            var receiptId = SeedDataConstants.TenantGuid(tenantId, $"receipt:{def.ReceiptNumber}");
            var receipt = InventoryReceipt.Create(
                receiptNumber: def.ReceiptNumber,
                type: def.Type,
                notes: def.Notes,
                tenantId: tenantId);
            SeedDataConstants.SetEntityId(receipt, receiptId);

            foreach (var item in def.Items)
            {
                if (!productLookup.TryGetValue(item.ProductSlug, out var entry))
                {
                    context.Logger.LogWarning(
                        "[SeedData] Product slug '{Slug}' not found for receipt {ReceiptNumber}. Skipping item.",
                        item.ProductSlug, def.ReceiptNumber);
                    continue;
                }

                var (product, variants) = entry;
                var variant = variants.FirstOrDefault(v => v.Name == item.VariantName)
                              ?? variants.FirstOrDefault();

                if (variant == null)
                {
                    context.Logger.LogWarning(
                        "[SeedData] No variants for product '{Slug}' in receipt {ReceiptNumber}. Skipping item.",
                        item.ProductSlug, def.ReceiptNumber);
                    continue;
                }

                receipt.AddItem(
                    productVariantId: variant.Id,
                    productId: product.Id,
                    productName: product.Name,
                    variantName: variant.Name,
                    sku: variant.Sku,
                    quantity: item.Quantity,
                    unitCost: item.UnitCost);
            }

            if (def.TargetStatus == InventoryReceiptStatus.Confirmed)
            {
                receipt.Confirm(context.TenantAdminUserId);
            }

            context.DbContext.Set<InventoryReceipt>().Add(receipt);
        }
    }
}
