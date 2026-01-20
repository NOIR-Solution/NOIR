using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.ApplicationDbContext
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentRoleId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    IsPlatformRole = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IconName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoles_AspNetRoles_ParentRoleId",
                        column: x => x.ParentRoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefreshToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PasswordLastChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsSystemUser = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailChangeOtps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CurrentEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NewEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OtpHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ResendCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastResendAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailChangeOtps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    PlainTextBody = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AvailableVariables = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HttpRequestAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    QueryString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestHeaders = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    RequestBody = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HttpRequestAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShortId = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Folder = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ThumbHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DominantColor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    HasTransparency = table.Column<bool>(type: "bit", nullable: false),
                    VariantsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    SrcsetsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EmailFrequency = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActionUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IncludedInDigest = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Actions = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetOtps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OtpHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ResetToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ResetTokenExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResendCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastResendAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetOtps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IconName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MetaTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PostCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostCategories_PostCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "PostCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PostCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReasonRevoked = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TokenFamily = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SharedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ParentResourceType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceShares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Identifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "string"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HandlerAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HttpRequestAuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    HandlerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PageContext = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ActionDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TargetDtoType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TargetDtoId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DtoDiff = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    InputParameters = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    OutputResult = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandlerAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandlerAuditLogs_HttpRequestAuditLogs_HttpRequestAuditLogId",
                        column: x => x.HttpRequestAuditLogId,
                        principalTable: "HttpRequestAuditLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermissionTemplateItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionTemplateItems_PermissionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "PermissionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermissionTemplateItems_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Excerpt = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    FeaturedImageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FeaturedImageAlt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FeaturedImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ScheduledPublishAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MetaTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AllowIndexing = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    ReadingTimeMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_MediaFiles_FeaturedImageId",
                        column: x => x.FeaturedImageId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Posts_PostCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "PostCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EntityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandlerAuditLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntityDiff = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityAuditLogs_HandlerAuditLogs_HandlerAuditLogId",
                        column: x => x.HandlerAuditLogId,
                        principalTable: "HandlerAuditLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostTagAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostTagAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostTagAssignments_PostTags_TagId",
                        column: x => x.TagId,
                        principalTable: "PostTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostTagAssignments_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_IsDeleted",
                table: "AspNetRoles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_NormalizedName_TenantId",
                table: "AspNetRoles",
                columns: new[] { "NormalizedName", "TenantId" },
                unique: true,
                filter: "[NormalizedName] IS NOT NULL AND [TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_ParentRoleId",
                table: "AspNetRoles",
                column: "ParentRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_TenantId",
                table: "AspNetRoles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_NewEmail",
                table: "EmailChangeOtps",
                column: "NewEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_SessionToken",
                table: "EmailChangeOtps",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_SessionToken_IsUsed",
                table: "EmailChangeOtps",
                columns: new[] { "SessionToken", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_TenantId",
                table: "EmailChangeOtps",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_UserId",
                table: "EmailChangeOtps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_UserId_IsUsed_IsDeleted",
                table: "EmailChangeOtps",
                columns: new[] { "UserId", "IsUsed", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name_IsActive_IsDeleted",
                table: "EmailTemplates",
                columns: new[] { "Name", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name_TenantId",
                table: "EmailTemplates",
                columns: new[] { "Name", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_TenantId",
                table: "EmailTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_CorrelationId",
                table: "EntityAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_EntityHistory",
                table: "EntityAuditLogs",
                columns: new[] { "EntityType", "EntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_EntityType_EntityId",
                table: "EntityAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_HandlerAuditLogId",
                table: "EntityAuditLogs",
                column: "HandlerAuditLogId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_IsArchived",
                table: "EntityAuditLogs",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_IsArchived_ArchivedAt",
                table: "EntityAuditLogs",
                columns: new[] { "IsArchived", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_TenantId",
                table: "EntityAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_Timestamp",
                table: "EntityAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_CorrelationId",
                table: "HandlerAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_HandlerName",
                table: "HandlerAuditLogs",
                column: "HandlerName");

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_HttpRequestAuditLogId",
                table: "HandlerAuditLogs",
                column: "HttpRequestAuditLogId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_IsArchived",
                table: "HandlerAuditLogs",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_IsArchived_ArchivedAt",
                table: "HandlerAuditLogs",
                columns: new[] { "IsArchived", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_PageContext",
                table: "HandlerAuditLogs",
                column: "PageContext");

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_TargetDtoType_TargetDtoId",
                table: "HandlerAuditLogs",
                columns: new[] { "TargetDtoType", "TargetDtoId" });

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_TenantId",
                table: "HandlerAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_CorrelationId",
                table: "HttpRequestAuditLogs",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_IsArchived",
                table: "HttpRequestAuditLogs",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_IsArchived_ArchivedAt",
                table: "HttpRequestAuditLogs",
                columns: new[] { "IsArchived", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_StartTime",
                table: "HttpRequestAuditLogs",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_Tenant_StartTime",
                table: "HttpRequestAuditLogs",
                columns: new[] { "TenantId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_TenantId",
                table: "HttpRequestAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_UserId",
                table: "HttpRequestAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_Folder_TenantId",
                table: "MediaFiles",
                columns: new[] { "Folder", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_ShortId",
                table: "MediaFiles",
                column: "ShortId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_Slug_TenantId",
                table: "MediaFiles",
                columns: new[] { "Slug", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_TenantId",
                table: "MediaFiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_UploadedBy",
                table: "MediaFiles",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_TenantId",
                table: "NotificationPreferences",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId_Category",
                table: "NotificationPreferences",
                columns: new[] { "UserId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PendingDigest",
                table: "Notifications",
                columns: new[] { "UserId", "IncludedInDigest", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_Category",
                table: "Notifications",
                columns: new[] { "UserId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsDeleted",
                table: "Notifications",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_Email",
                table: "PasswordResetOtps",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_Email_IsUsed_IsDeleted",
                table: "PasswordResetOtps",
                columns: new[] { "Email", "IsUsed", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_ResetToken",
                table: "PasswordResetOtps",
                column: "ResetToken");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_SessionToken",
                table: "PasswordResetOtps",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_SessionToken_IsUsed",
                table: "PasswordResetOtps",
                columns: new[] { "SessionToken", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_TenantId",
                table: "PasswordResetOtps",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_UserId",
                table: "PasswordResetOtps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Resource_Action_Scope",
                table: "Permissions",
                columns: new[] { "Resource", "Action", "Scope" },
                unique: true,
                filter: "[Scope] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplateItems_PermissionId",
                table: "PermissionTemplateItems",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplateItems_TemplateId_PermissionId",
                table: "PermissionTemplateItems",
                columns: new[] { "TemplateId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_IsDeleted",
                table: "PermissionTemplates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_Name_TenantId",
                table: "PermissionTemplates",
                columns: new[] { "Name", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_TenantId",
                table: "PermissionTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_Parent_Sort",
                table: "PostCategories",
                columns: new[] { "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_Slug_TenantId",
                table: "PostCategories",
                columns: new[] { "Slug", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_TenantId",
                table: "PostCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AuthorId",
                table: "Posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Category_Status",
                table: "Posts",
                columns: new[] { "CategoryId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_FeaturedImageId",
                table: "Posts",
                column: "FeaturedImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ScheduledPublish",
                table: "Posts",
                columns: new[] { "ScheduledPublishAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Slug_TenantId",
                table: "Posts",
                columns: new[] { "Slug", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Status_PublishedAt",
                table: "Posts",
                columns: new[] { "Status", "PublishedAt", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TenantId",
                table: "Posts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PostTagAssignments_PostId_TagId",
                table: "PostTagAssignments",
                columns: new[] { "PostId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostTagAssignments_TagId",
                table: "PostTagAssignments",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_PostTagAssignments_TenantId",
                table: "PostTagAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PostTags_Slug_TenantId",
                table: "PostTags",
                columns: new[] { "Slug", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostTags_TenantId",
                table: "PostTags",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TenantId",
                table: "RefreshTokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenFamily",
                table: "RefreshTokens",
                column: "TokenFamily");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsDeleted",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_ExpiresAt",
                table: "ResourceShares",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_IsDeleted",
                table: "ResourceShares",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_Resource",
                table: "ResourceShares",
                columns: new[] { "ResourceType", "ResourceId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_TenantId",
                table: "ResourceShares",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_Unique",
                table: "ResourceShares",
                columns: new[] { "ResourceType", "ResourceId", "SharedWithUserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_User",
                table: "ResourceShares",
                columns: new[] { "SharedWithUserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_IsDeleted",
                table: "RolePermissions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_IsDeleted",
                table: "TenantSettings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_Key",
                table: "TenantSettings",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId_Category",
                table: "TenantSettings",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId_Key",
                table: "TenantSettings",
                columns: new[] { "TenantId", "Key" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "EmailChangeOtps");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "EntityAuditLogs");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PasswordResetOtps");

            migrationBuilder.DropTable(
                name: "PermissionTemplateItems");

            migrationBuilder.DropTable(
                name: "PostTagAssignments");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ResourceShares");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "TenantSettings");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "HandlerAuditLogs");

            migrationBuilder.DropTable(
                name: "PermissionTemplates");

            migrationBuilder.DropTable(
                name: "PostTags");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "HttpRequestAuditLogs");

            migrationBuilder.DropTable(
                name: "MediaFiles");

            migrationBuilder.DropTable(
                name: "PostCategories");
        }
    }
}
