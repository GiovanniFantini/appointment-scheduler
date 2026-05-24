using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    [Migration("20260523173000_BackfillCalendarioDocumentiAccessLevel")]
    public partial class BackfillCalendarioDocumentiAccessLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nuovo backfill separato: estende i livelli anche a Calendario (1)
            // e Documenti (5) senza modificare la migration storica.
            // Mantiene eventuali valori già impostati manualmente (solo AccessLevel NULL).
            migrationBuilder.Sql(@"
                UPDATE ""RoleFeatures"" rf
                SET ""AccessLevel"" = CASE WHEN r.""IsDefault"" THEN 3 ELSE 1 END
                FROM ""MerchantRoles"" r
                WHERE rf.""RoleId"" = r.""Id""
                  AND rf.""Feature"" IN (1, 5)
                  AND rf.""IsEnabled"" = TRUE
                  AND rf.""AccessLevel"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""RoleFeatures""
                SET ""AccessLevel"" = NULL
                WHERE ""Feature"" IN (1, 5);
            ");
        }
    }
}
