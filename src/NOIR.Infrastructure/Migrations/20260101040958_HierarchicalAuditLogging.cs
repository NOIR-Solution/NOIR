using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HierarchicalAuditLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

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
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HttpRequestAuditLogs", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_CorrelationId",
                table: "EntityAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_EntityType_EntityId",
                table: "EntityAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_HandlerAuditLogId",
                table: "EntityAuditLogs",
                column: "HandlerAuditLogId");

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
                name: "IX_HandlerAuditLogs_TargetDtoType_TargetDtoId",
                table: "HandlerAuditLogs",
                columns: new[] { "TargetDtoType", "TargetDtoId" });

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_CorrelationId",
                table: "HttpRequestAuditLogs",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_StartTime",
                table: "HttpRequestAuditLogs",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_UserId",
                table: "HttpRequestAuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntityAuditLogs");

            migrationBuilder.DropTable(
                name: "HandlerAuditLogs");

            migrationBuilder.DropTable(
                name: "HttpRequestAuditLogs");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedProperties = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    HandlerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    InputParameters = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OutputResult = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");
        }
    }
}
