using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleFeatureAccessLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "RoleFeatures",
                type: "integer",
                nullable: true);

            // Backfill del livello di accesso per la sola feature Magazzino (Feature = 10).
            // Le altre feature non usano i livelli: AccessLevel resta NULL.
            //  - Ruoli di default ("Responsabile App"): livello Manager (3), pieni poteri.
            //  - Altri ruoli con Magazzino abilitato: livello ReadOnly (1), default sicuro;
            //    il merchant potrà poi alzarlo a Operator/Manager dalla gestione ruoli.
            migrationBuilder.Sql(@"
                UPDATE ""RoleFeatures"" rf
                SET ""AccessLevel"" = CASE WHEN r.""IsDefault"" THEN 3 ELSE 1 END
                FROM ""MerchantRoles"" r
                WHERE rf.""RoleId"" = r.""Id""
                  AND rf.""Feature"" = 10
                  AND rf.""IsEnabled"" = TRUE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "RoleFeatures");
        }
    }
}
