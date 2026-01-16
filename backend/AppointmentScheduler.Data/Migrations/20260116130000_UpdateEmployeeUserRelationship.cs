using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmployeeUserRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rimuovi il constraint unique da Employee.UserId per permettere multipli Employee record per stesso User
            // Questo permette a un employee di lavorare per multipli merchant
            migrationBuilder.DropIndex(
                name: "IX_Employees_UserId",
                table: "Employees");

            // Ricrea l'indice senza unique constraint
            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ripristina il constraint unique (richiede che non ci siano duplicati!)
            migrationBuilder.DropIndex(
                name: "IX_Employees_UserId",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId",
                unique: true);
        }
    }
}
