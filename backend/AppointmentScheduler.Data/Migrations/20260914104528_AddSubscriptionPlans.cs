using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubscriptionPlanId",
                table: "Merchants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "Merchants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Features = table.Column<int[]>(type: "integer[]", nullable: false),
                    MaxEmployees = table.Column<int>(type: "integer", nullable: true),
                    MaxBranches = table.Column<int>(type: "integer", nullable: true),
                    MaxStorageBytes = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Merchants_SubscriptionPlanId",
                table: "Merchants",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Merchants_SubscriptionPlans_SubscriptionPlanId",
                table: "Merchants",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql("""
                INSERT INTO "SubscriptionPlans" ("Name", "Description", "Features", "UpdatedAt")
                VALUES ('Completo', 'Tutte le funzioni, senza limiti quantitativi.', ARRAY[1,2,3,4,5,6,7,8,9,10], NOW());
                UPDATE "Merchants" SET "SubscriptionPlanId" = (SELECT "Id" FROM "SubscriptionPlans" WHERE "Name" = 'Completo');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Merchants_SubscriptionPlans_SubscriptionPlanId",
                table: "Merchants");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_Merchants_SubscriptionPlanId",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Merchants");
        }
    }
}
