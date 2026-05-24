using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalExternalBaseRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Crea i ruoli base per merchant esistenti se mancanti.
            migrationBuilder.Sql(@"
                INSERT INTO ""MerchantRoles"" (""MerchantId"", ""Name"", ""IsDefault"", ""CreatedAt"")
                SELECT m.""Id"", 'Interno Base', FALSE, NOW() AT TIME ZONE 'UTC'
                FROM ""Merchants"" m
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""MerchantRoles"" r
                    WHERE r.""MerchantId"" = m.""Id""
                      AND r.""Name"" = 'Interno Base'
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""MerchantRoles"" (""MerchantId"", ""Name"", ""IsDefault"", ""CreatedAt"")
                SELECT m.""Id"", 'Esterno Base', FALSE, NOW() AT TIME ZONE 'UTC'
                FROM ""Merchants"" m
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""MerchantRoles"" r
                    WHERE r.""MerchantId"" = m.""Id""
                      AND r.""Name"" = 'Esterno Base'
                );
            ");

            // Popola le feature del ruolo "Interno Base".
            migrationBuilder.Sql(@"
                INSERT INTO ""RoleFeatures"" (""RoleId"", ""Feature"", ""IsEnabled"", ""AccessLevel"")
                SELECT r.""Id"",
                       f.""Feature"",
                       CASE WHEN f.""Feature"" IN (1, 2, 9) THEN TRUE ELSE FALSE END,
                       CASE WHEN f.""Feature"" IN (1, 2, 9) THEN 1 ELSE NULL END
                FROM ""MerchantRoles"" r
                CROSS JOIN (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) AS f(""Feature"")
                WHERE r.""Name"" = 'Interno Base'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ""RoleFeatures"" rf
                      WHERE rf.""RoleId"" = r.""Id""
                        AND rf.""Feature"" = f.""Feature""
                  );
            ");

            // Popola le feature del ruolo "Esterno Base".
            migrationBuilder.Sql(@"
                INSERT INTO ""RoleFeatures"" (""RoleId"", ""Feature"", ""IsEnabled"", ""AccessLevel"")
                SELECT r.""Id"",
                       f.""Feature"",
                       CASE WHEN f.""Feature"" = 1 THEN TRUE ELSE FALSE END,
                       CASE WHEN f.""Feature"" = 1 THEN 1 ELSE NULL END
                FROM ""MerchantRoles"" r
                CROSS JOIN (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) AS f(""Feature"")
                WHERE r.""Name"" = 'Esterno Base'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ""RoleFeatures"" rf
                      WHERE rf.""RoleId"" = r.""Id""
                        AND rf.""Feature"" = f.""Feature""
                  );
            ");

            // Riassegna gli esterni attivi dal ruolo "Responsabile App" a "Esterno Base".
            migrationBuilder.Sql(@"
                UPDATE ""EmployeeMemberships"" em
                SET ""RoleId"" = rb.""Id""
                FROM ""Employees"" e,
                     ""MerchantRoles"" currentRole,
                     ""MerchantRoles"" rb
                WHERE e.""Id"" = em.""EmployeeId""
                  AND currentRole.""Id"" = em.""RoleId""
                  AND rb.""MerchantId"" = em.""MerchantId""
                  AND rb.""Name"" = 'Esterno Base'
                  AND e.""Kind"" = 1
                  AND (currentRole.""IsDefault"" = TRUE OR currentRole.""Name"" = 'Responsabile App');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Riporta gli esterni su "Responsabile App" se presente.
            migrationBuilder.Sql(@"
                UPDATE ""EmployeeMemberships"" em
                SET ""RoleId"" = app.""Id""
                FROM ""Employees"" e,
                     ""MerchantRoles"" currentRole,
                     ""MerchantRoles"" app
                WHERE e.""Id"" = em.""EmployeeId""
                  AND currentRole.""Id"" = em.""RoleId""
                  AND app.""MerchantId"" = em.""MerchantId""
                  AND app.""Name"" = 'Responsabile App'
                  AND e.""Kind"" = 1
                  AND currentRole.""Name"" = 'Esterno Base';
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""RoleFeatures"" rf
                USING ""MerchantRoles"" r
                WHERE rf.""RoleId"" = r.""Id""
                  AND r.""Name"" IN ('Interno Base', 'Esterno Base');
            ");

            migrationBuilder.Sql(@"
                DELETE FROM ""MerchantRoles"" r
                WHERE r.""Name"" IN ('Interno Base', 'Esterno Base')
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ""EmployeeMemberships"" em
                      WHERE em.""RoleId"" = r.""Id""
                  );
            ");
        }
    }
}
