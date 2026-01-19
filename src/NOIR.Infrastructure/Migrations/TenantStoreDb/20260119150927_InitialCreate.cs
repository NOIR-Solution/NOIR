using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.TenantStoreDb
{
    /// <inheritdoc />
    /// <remarks>
    /// This migration is intentionally empty because the Tenants table is created by ApplicationDbContext.
    /// TenantStoreDbContext shares the same database and uses the same Tenants table for Finbuckle tenant resolution.
    /// The model snapshot is still needed for EF Core to track the schema state for this context.
    /// </remarks>
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Empty - Tenants table is created by ApplicationDbContext migration
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Empty - Tenants table is managed by ApplicationDbContext
        }
    }
}
