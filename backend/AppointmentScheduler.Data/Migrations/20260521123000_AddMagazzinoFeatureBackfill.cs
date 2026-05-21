using AppointmentScheduler.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260521123000_AddMagazzinoFeatureBackfill")]
    public partial class AddMagazzinoFeatureBackfill : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""RoleFeatures"" (""RoleId"", ""Feature"", ""IsEnabled"")
                SELECT r.""Id"", 10, true
                FROM ""MerchantRoles"" r
                WHERE r.""IsDefault"" = true
                  AND NOT EXISTS (
                      SELECT 1 FROM ""RoleFeatures"" rf
                      WHERE rf.""RoleId"" = r.""Id"" AND rf.""Feature"" = 10
                  );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""RoleFeatures""
                WHERE ""Feature"" = 10
                  AND ""RoleId"" IN (
                      SELECT ""Id""
                      FROM ""MerchantRoles""
                      WHERE ""IsDefault"" = true
                  );
            ");
        }
    }
}