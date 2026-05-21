using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStockBalanceRowVersionWithXmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The optimistic concurrency token for InventoryStockBalances now maps to
            // PostgreSQL's system "xmin" column instead of a stored "bytea" column.
            // "xmin" is a system column present on every table and is NOT created here;
            // only the obsolete physical "RowVersion" column is dropped.
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InventoryStockBalances");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InventoryStockBalances",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
