namespace NOIR.Infrastructure.Persistence.SeedData.Data;

/// <summary>
/// Customer definition for seed data.
/// </summary>
public record CustomerDef(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    int AddressIndex);

/// <summary>
/// Order definition for seed data with lifecycle target status.
/// </summary>
public record OrderDef(
    string OrderNumber,
    int CustomerIndex,
    int DayOffset,
    OrderStatus TargetStatus,
    string? CancellationReason,
    OrderItemDef[] Items);

/// <summary>
/// Order item referencing a product by slug and variant name.
/// Resolved at seed time against CatalogSeedModule products.
/// </summary>
public record OrderItemDef(
    string ProductSlug,
    string VariantName,
    int Quantity,
    decimal UnitPrice);

/// <summary>
/// Inventory receipt definition for seed data.
/// </summary>
public record ReceiptDef(
    string ReceiptNumber,
    InventoryReceiptType Type,
    InventoryReceiptStatus TargetStatus,
    string? Notes,
    int DayOffset,
    ReceiptItemDef[] Items);

/// <summary>
/// Inventory receipt item referencing a product by slug and variant name.
/// </summary>
public record ReceiptItemDef(
    string ProductSlug,
    string VariantName,
    int Quantity,
    decimal UnitCost);

/// <summary>
/// Vietnamese commerce seed data: customers, orders, and inventory receipts.
/// Product slugs reference products seeded by CatalogSeedModule.
/// Prices in VND.
/// </summary>
public static class CommerceData
{
    public static CustomerDef[] GetCustomers() =>
    [
        new("An", "Nguyễn Văn", "nguyen.van.an@email.com", "0901234567", 0),
        new("Bình", "Trần Thị", "tran.thi.binh@email.com", "0912345678", 1),
        new("Cường", "Lê Minh", "le.minh.cuong@email.com", "0923456789", 2),
        new("Dung", "Phạm Thị", "pham.thi.dung@email.com", "0934567890", 3),
        new("Đức", "Hoàng", "hoang.duc@email.com", "0945678901", 4),
        new("Hoa", "Ngô Thị", "ngo.thi.hoa@email.com", "0956789012", 5),
        new("Khoa", "Đặng Văn", "dang.van.khoa@email.com", "0967890123", 6),
        new("Lan", "Vũ Thị", "vu.thi.lan@email.com", "0978901234", 7),
        new("Minh", "Bùi Quang", "bui.quang.minh@email.com", "0389012345", 8),
        new("Ngọc", "Võ Thị", "vo.thi.ngoc@email.com", "0390123456", 9),
        new("Phúc", "Trịnh Văn", "trinh.van.phuc@email.com", "0371234567", 10),
        new("Quỳnh", "Lý Thị", "ly.thi.quynh@email.com", "0362345678", 11)
    ];

    /// <summary>
    /// 9 orders spread over ~3 months with various lifecycle statuses.
    /// Product slugs and variant names MUST match CatalogData.GetProducts() exactly.
    /// Variant format: "{Size} - {Color}" for sized products, "{VariantName}" for accessories.
    /// </summary>
    public static OrderDef[] GetOrders() =>
    [
        // 3 Completed orders (oldest)
        new("ORD-20260101-0001", 0, -85, OrderStatus.Completed, null,
        [
            new("ao-thun-tron-co-tron", "M - Trắng", 2, 199_000m),
            new("quan-jeans-slim-fit", "32 - Xanh nhạt", 1, 499_000m)
        ]),

        new("ORD-20260115-0002", 1, -60, OrderStatus.Completed, null,
        [
            new("quan-kaki-chat", "32 - Be", 1, 550_000m),
            new("tui-xach-da-thoi-trang", "Nâu bò", 1, 1_290_000m),
            new("that-lung-da-nam", "Đen - 110cm", 1, 450_000m)
        ]),

        new("ORD-20260201-0003", 4, -30, OrderStatus.Completed, null,
        [
            new("ao-polo-classic", "L - Xanh navy", 1, 450_000m)
        ]),

        // 1 Cancelled order
        new("ORD-20260210-0004", 2, -45, OrderStatus.Cancelled,
            "Khách hàng đổi ý, muốn chọn size khác",
        [
            new("ao-khoac-gio-nhe", "L - Đen", 1, 750_000m),
            new("mu-luoi-trai-unisex", "Đen", 1, 199_000m)
        ]),

        // 2 Confirmed orders
        new("ORD-20260218-0005", 3, -10, OrderStatus.Confirmed, null,
        [
            new("ao-kieu-tay-phong", "S - Trắng", 1, 499_000m),
            new("kinh-mat-phan-cuc", "Gọng đen - Tròng xám", 1, 699_000m)
        ]),

        new("ORD-20260220-0006", 5, -5, OrderStatus.Confirmed, null,
        [
            new("ao-thun-tron-co-tron", "L - Đen", 3, 199_000m)
        ]),

        // 1 Processing order
        new("ORD-20260222-0007", 6, -3, OrderStatus.Processing, null,
        [
            new("dong-ho-nam-thoi-trang", "Dây da đen", 1, 1_890_000m),
            new("that-lung-da-nam", "Nâu - 110cm", 1, 450_000m)
        ]),

        // 1 Shipped order
        new("ORD-20260223-0008", 8, -2, OrderStatus.Shipped, null,
        [
            new("quan-short-the-thao", "M - Đen", 1, 299_000m),
            new("ao-polo-classic", "L - Trắng", 2, 450_000m),
            new("quan-jeans-slim-fit", "33 - Đen", 1, 499_000m)
        ]),

        // 1 Pending order (most recent)
        new("ORD-20260224-0009", 9, -1, OrderStatus.Pending, null,
        [
            new("ao-khoac-gio-nhe", "L - Xanh rêu", 1, 750_000m),
            new("mu-luoi-trai-unisex", "Trắng", 1, 199_000m),
            new("kinh-mat-phan-cuc", "Gọng vàng - Tròng nâu", 1, 699_000m),
            new("ao-thun-tron-co-tron", "XL - Đen", 1, 199_000m)
        ])
    ];

    /// <summary>
    /// 3 inventory receipts: 2 StockIn (Confirmed), 1 StockOut (Draft).
    /// Product slugs and variant names MUST match CatalogData.GetProducts() exactly.
    /// </summary>
    public static ReceiptDef[] GetReceipts() =>
    [
        new("RCV-20261001-0001", InventoryReceiptType.StockIn, InventoryReceiptStatus.Confirmed,
            "Nhập hàng đợt 1 - Quý 4/2025", -100,
        [
            new("ao-thun-tron-co-tron", "M - Trắng", 50, 95_000m),
            new("ao-thun-tron-co-tron", "L - Đen", 50, 95_000m),
            new("ao-thun-tron-co-tron", "XL - Đen", 30, 95_000m),
            new("quan-jeans-slim-fit", "32 - Xanh nhạt", 30, 220_000m),
            new("quan-jeans-slim-fit", "33 - Đen", 25, 220_000m),
            new("quan-short-the-thao", "M - Đen", 20, 140_000m),
            new("quan-jogger-day-thun", "L - Đen", 20, 170_000m)
        ]),

        new("RCV-20261120-0002", InventoryReceiptType.StockIn, InventoryReceiptStatus.Confirmed,
            "Nhập bổ sung - chuẩn bị Tết 2026", -50,
        [
            new("ao-polo-classic", "L - Xanh navy", 40, 210_000m),
            new("ao-polo-classic", "L - Trắng", 40, 210_000m),
            new("ao-khoac-gio-nhe", "M - Đen", 15, 380_000m),
            new("ao-khoac-gio-nhe", "L - Xanh rêu", 15, 380_000m),
            new("ao-thun-nu-crop-top", "S - Hồng", 20, 85_000m),
            new("dong-ho-nam-thoi-trang", "Dây da đen", 10, 900_000m)
        ]),

        new("SHP-20260224-0001", InventoryReceiptType.StockOut, InventoryReceiptStatus.Draft,
            "Xuất kho cho đơn hàng tháng 2", -1,
        [
            new("ao-thun-tron-co-tron", "L - Đen", 3, 95_000m),
            new("quan-jogger-day-thun", "M - Xám", 1, 170_000m),
            new("ao-polo-classic", "L - Trắng", 2, 210_000m)
        ])
    ];
}
