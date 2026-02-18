using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Mappers;
using NOIR.Application.Features.Inventory.Specifications;

namespace NOIR.Application.Features.Inventory.Commands.CreateInventoryReceipt;

/// <summary>
/// Wolverine handler for creating an inventory receipt.
/// </summary>
public class CreateInventoryReceiptCommandHandler
{
    private readonly IRepository<InventoryReceipt, Guid> _receiptRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateInventoryReceiptCommandHandler(
        IRepository<InventoryReceipt, Guid> receiptRepository,
        IRepository<Product, Guid> productRepository,
        IUnitOfWork unitOfWork)
    {
        _receiptRepository = receiptRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InventoryReceiptDto>> Handle(
        CreateInventoryReceiptCommand command,
        CancellationToken cancellationToken)
    {
        // Validate product and variant existence before creating receipt items.
        // Without this, invalid GUIDs create dangling references that get silently
        // skipped during confirmation (ConfirmInventoryReceiptCommandHandler).
        var distinctProductIds = command.Items.Select(i => i.ProductId).Distinct().ToList();
        var productVariantMap = new Dictionary<Guid, Product>();

        foreach (var productId in distinctProductIds)
        {
            var productSpec = new ProductByIdSpec(productId);
            var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

            if (product is null)
            {
                return Result.Failure<InventoryReceiptDto>(
                    Error.NotFound($"Product with ID '{productId}' not found.", "NOIR-INVENTORY-005"));
            }

            productVariantMap[productId] = product;
        }

        foreach (var item in command.Items)
        {
            var product = productVariantMap[item.ProductId];
            var variant = product.Variants.FirstOrDefault(v => v.Id == item.ProductVariantId);

            if (variant is null)
            {
                return Result.Failure<InventoryReceiptDto>(
                    Error.NotFound(
                        $"Product variant with ID '{item.ProductVariantId}' not found on product '{product.Name}'.",
                        "NOIR-INVENTORY-006"));
            }
        }

        // Generate receipt number
        var prefix = command.Type == InventoryReceiptType.StockIn ? "RCV" : "SHP";
        var dateStr = DateTimeOffset.UtcNow.ToString("yyyyMMdd");
        var receiptPrefix = $"{prefix}-{dateStr}-";

        var latestSpec = new LatestReceiptNumberTodaySpec(receiptPrefix);
        var latestReceipt = await _receiptRepository.FirstOrDefaultAsync(latestSpec, cancellationToken);

        int sequence = 1;
        if (latestReceipt is not null)
        {
            var lastNumber = latestReceipt.ReceiptNumber.Split('-').LastOrDefault();
            if (int.TryParse(lastNumber, out var lastSeq))
            {
                sequence = lastSeq + 1;
            }
        }

        var receiptNumber = $"{receiptPrefix}{sequence:D4}";

        var receipt = InventoryReceipt.Create(
            receiptNumber,
            command.Type,
            command.Notes);

        // Add items
        foreach (var item in command.Items)
        {
            receipt.AddItem(
                item.ProductVariantId,
                item.ProductId,
                item.ProductName,
                item.VariantName,
                item.Sku,
                item.Quantity,
                item.UnitCost);
        }

        await _receiptRepository.AddAsync(receipt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(InventoryReceiptMapper.ToDto(receipt));
    }
}
