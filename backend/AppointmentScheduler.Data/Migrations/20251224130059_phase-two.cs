using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class phasetwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to Services table
            migrationBuilder.AddColumn<int>(
                name: "BookingMode",
                table: "Services",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "SlotDurationMinutes",
                table: "Services",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxCapacityPerSlot",
                table: "Services",
                type: "integer",
                nullable: true);

            // Create Availabilities table
            migrationBuilder.CreateTable(
                name: "Availabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    SpecificDate = table.Column<DateTime>(type: "date", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Availabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Availabilities_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create AvailabilitySlots table
            migrationBuilder.CreateTable(
                name: "AvailabilitySlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AvailabilityId = table.Column<int>(type: "integer", nullable: false),
                    SlotTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilitySlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AvailabilitySlots_Availabilities_AvailabilityId",
                        column: x => x.AvailabilityId,
                        principalTable: "Availabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_ServiceId",
                table: "Availabilities",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_DayOfWeek",
                table: "Availabilities",
                column: "DayOfWeek");

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_SpecificDate",
                table: "Availabilities",
                column: "SpecificDate");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilitySlots_AvailabilityId",
                table: "AvailabilitySlots",
                column: "AvailabilityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AvailabilitySlots");

            migrationBuilder.DropTable(
                name: "Availabilities");

            migrationBuilder.DropColumn(
                name: "BookingMode",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "SlotDurationMinutes",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "MaxCapacityPerSlot",
                table: "Services");
        }
    }
}
