using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRolesSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Aggiungi le nuove colonne booleane per i ruoli
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsConsumer",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMerchant",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmployee",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Step 2: Migra i dati esistenti dal vecchio Role alle nuove colonne
            // Role enum values: User = 1, Merchant = 2, Admin = 3, Employee = 4

            // Admin (Role = 3) -> tutti i flag a true
            migrationBuilder.Sql(
                @"UPDATE ""Users""
                  SET ""IsAdmin"" = true,
                      ""IsConsumer"" = true,
                      ""IsMerchant"" = true,
                      ""IsEmployee"" = true
                  WHERE ""Role"" = 3");

            // Merchant (Role = 2) -> IsMerchant e IsConsumer a true
            migrationBuilder.Sql(
                @"UPDATE ""Users""
                  SET ""IsMerchant"" = true,
                      ""IsConsumer"" = true
                  WHERE ""Role"" = 2");

            // Employee (Role = 4) -> IsEmployee e IsConsumer a true
            migrationBuilder.Sql(
                @"UPDATE ""Users""
                  SET ""IsEmployee"" = true,
                      ""IsConsumer"" = true
                  WHERE ""Role"" = 4");

            // User/Consumer (Role = 1) -> IsConsumer a true
            migrationBuilder.Sql(
                @"UPDATE ""Users""
                  SET ""IsConsumer"" = true
                  WHERE ""Role"" = 1");

            // Step 3: Rimuovi la vecchia colonna Role
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Ripristina la vecchia colonna Role
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 1); // Default: User

            // Step 2: Migra i dati dalle nuove colonne al vecchio Role
            // Priorità: Admin > Merchant > Employee > User

            migrationBuilder.Sql(
                @"UPDATE ""Users""
                  SET ""Role"" = 3
                  WHERE ""IsAdmin"" = true");

            migrationBuilder.Sql(
                @"UPDATE ""Users""
                  SET ""Role"" = 2
                  WHERE ""IsMerchant"" = true AND ""IsAdmin"" = false");

            migrationBuilder.Sql(
                @"UPDATE ""Users""
                  SET ""Role"" = 4
                  WHERE ""IsEmployee"" = true AND ""IsMerchant"" = false AND ""IsAdmin"" = false");

            migrationBuilder.Sql(
                @"UPDATE ""Users""
                  SET ""Role"" = 1
                  WHERE ""IsConsumer"" = true AND ""IsEmployee"" = false AND ""IsMerchant"" = false AND ""IsAdmin"" = false");

            // Step 3: Rimuovi le nuove colonne booleane
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsConsumer",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsMerchant",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmployee",
                table: "Users");
        }
    }
}
