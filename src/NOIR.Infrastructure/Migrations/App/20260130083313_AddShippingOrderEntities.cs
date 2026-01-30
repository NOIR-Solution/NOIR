using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.App
{
    /// <inheritdoc />
    public partial class AddShippingOrderEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShippingOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderOrderId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceTypeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CodFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    InsuranceFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    TotalShippingFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CodAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DeclaredValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WeightGrams = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PickupAddressJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    DeliveryAddressJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    SenderJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    RecipientJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    ItemsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    LabelUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrackingUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedDeliveryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualDeliveryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PickedUpAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProviderRawResponse = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    IsFreeship = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_ShippingOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingOrders_ShippingProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "ShippingProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShippingWebhookLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "POST"),
                    Endpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HeadersJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    Signature = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessedSuccessfully = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    ProcessingAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingWebhookLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingTrackingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShippingOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EventDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingTrackingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingTrackingEvents_ShippingOrders_ShippingOrderId",
                        column: x => x.ShippingOrderId,
                        principalTable: "ShippingOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOrders_OrderId",
                table: "ShippingOrders",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOrders_ProviderId",
                table: "ShippingOrders",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOrders_Status",
                table: "ShippingOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOrders_Tenant_OrderId",
                table: "ShippingOrders",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOrders_Tenant_Status_Created",
                table: "ShippingOrders",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingOrders_TenantId",
                table: "ShippingOrders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_ShippingOrders_TrackingNumber",
                table: "ShippingOrders",
                column: "TrackingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingTrackingEvents_Order_Date",
                table: "ShippingTrackingEvents",
                columns: new[] { "ShippingOrderId", "EventDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingWebhookLogs_ProcessingStatus",
                table: "ShippingWebhookLogs",
                columns: new[] { "ProcessedSuccessfully", "ProcessingAttempts" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingWebhookLogs_Provider_Tracking",
                table: "ShippingWebhookLogs",
                columns: new[] { "ProviderCode", "TrackingNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ShippingWebhookLogs_ReceivedAt",
                table: "ShippingWebhookLogs",
                column: "ReceivedAt",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingTrackingEvents");

            migrationBuilder.DropTable(
                name: "ShippingWebhookLogs");

            migrationBuilder.DropTable(
                name: "ShippingOrders");
        }
    }
}
