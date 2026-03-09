using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.App
{
    /// <inheritdoc />
    public partial class NormalizeTaskSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Assign sequential sort orders (1, 2, 3...) to all active tasks within each column,
            // ordered by existing SortOrder then CreatedAt to preserve any manually set order.
            // This fixes tasks that were all created with SortOrder = 0 (the default).
            migrationBuilder.Sql(@"
                WITH RankedTasks AS (
                    SELECT
                        Id,
                        ROW_NUMBER() OVER (
                            PARTITION BY ColumnId
                            ORDER BY SortOrder, CreatedAt
                        ) AS NewSortOrder
                    FROM ProjectTasks
                    WHERE IsDeleted = 0
                      AND IsArchived = 0
                      AND ColumnId IS NOT NULL
                )
                UPDATE pt
                SET pt.SortOrder = r.NewSortOrder
                FROM ProjectTasks pt
                INNER JOIN RankedTasks r ON pt.Id = r.Id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot reverse: original values are lost
        }
    }
}
