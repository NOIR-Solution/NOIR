namespace NOIR.Infrastructure.Persistence.SeedData.Data;

/// <summary>
/// Vietnamese address definition for seed data.
/// </summary>
public record AddressDef(
    string FullName,
    string Phone,
    string AddressLine1,
    string Ward,
    string District,
    string Province);

/// <summary>
/// Realistic Vietnamese addresses covering HCM, Hanoi, and Da Nang.
/// Used by CommerceSeedModule for customer addresses and order shipping addresses.
/// </summary>
public static class VietnameseAddresses
{
    public static AddressDef[] GetAddresses() =>
    [
        // Ho Chi Minh City (8 addresses)
        new("Nguyễn Văn An", "0901234567", "123 Nguyễn Huệ",
            "Phường Bến Nghé", "Quận 1", "TP. Hồ Chí Minh"),

        new("Trần Thị Bình", "0912345678", "456 Lê Lợi",
            "Phường Bến Thành", "Quận 1", "TP. Hồ Chí Minh"),

        new("Lê Minh Cường", "0923456789", "78 Võ Văn Tần",
            "Phường 6", "Quận 3", "TP. Hồ Chí Minh"),

        new("Phạm Thị Dung", "0934567890", "245 Điện Biên Phủ",
            "Phường 15", "Quận Bình Thạnh", "TP. Hồ Chí Minh"),

        new("Hoàng Đức Anh", "0945678901", "12 Võ Chí Công",
            "Phường Thảo Điền", "Thành phố Thủ Đức", "TP. Hồ Chí Minh"),

        new("Ngô Thị Hoa", "0956789012", "89 Phan Đình Phùng",
            "Phường 1", "Quận Phú Nhuận", "TP. Hồ Chí Minh"),

        new("Đặng Văn Khoa", "0967890123", "34 Quang Trung",
            "Phường 10", "Quận Gò Vấp", "TP. Hồ Chí Minh"),

        new("Vũ Thị Lan", "0978901234", "567 Nguyễn Thị Thập",
            "Phường Tân Phú", "Quận 7", "TP. Hồ Chí Minh"),

        // Hanoi (6 addresses)
        new("Bùi Quang Minh", "0389012345", "12 Phan Chu Trinh",
            "Phường Hoàn Kiếm", "Quận Hoàn Kiếm", "Hà Nội"),

        new("Võ Thị Ngọc", "0390123456", "34 Hai Bà Trưng",
            "Phường Tràng Tiền", "Quận Hoàn Kiếm", "Hà Nội"),

        new("Trịnh Văn Phúc", "0371234567", "56 Tôn Đức Thắng",
            "Phường Quốc Tử Giám", "Quận Đống Đa", "Hà Nội"),

        new("Lý Thị Quỳnh", "0362345678", "23 Nguyễn Thái Học",
            "Phường Điện Biên", "Quận Ba Đình", "Hà Nội"),

        new("Dương Văn Sơn", "0353456789", "78 Xuân Thủy",
            "Phường Dịch Vọng Hậu", "Quận Cầu Giấy", "Hà Nội"),

        new("Mai Thị Tâm", "0344567890", "45 Nguyễn Trãi",
            "Phường Khương Mai", "Quận Thanh Xuân", "Hà Nội"),

        // Da Nang (4 addresses)
        new("Phan Văn Uy", "0235678901", "56 Bạch Đằng",
            "Phường Hải Châu 1", "Quận Hải Châu", "Đà Nẵng"),

        new("Huỳnh Thị Vân", "0226789012", "123 Điện Biên Phủ",
            "Phường Chính Gián", "Quận Thanh Khê", "Đà Nẵng"),

        new("Nguyễn Xuân Yên", "0217890123", "89 Ngô Quyền",
            "Phường An Hải Bắc", "Quận Sơn Trà", "Đà Nẵng"),

        new("Trần Công Danh", "0208901234", "34 Lê Văn Hiến",
            "Phường Khuê Mỹ", "Quận Ngũ Hành Sơn", "Đà Nẵng")
    ];
}
