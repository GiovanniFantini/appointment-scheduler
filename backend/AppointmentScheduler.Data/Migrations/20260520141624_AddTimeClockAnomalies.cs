using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeClockAnomalies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimeClockAnomalies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: true),
                    EventParticipantId = table.Column<int>(type: "integer", nullable: true),
                    TimeEntryId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeviationMinutes = table.Column<int>(type: "integer", nullable: true),
                    OvertimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    EmployeeReason = table.Column<int>(type: "integer", nullable: true),
                    EmployeeNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    JustifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeClockAnomalies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeClockAnomalies_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeClockAnomalies_EventParticipants_EventParticipantId",
                        column: x => x.EventParticipantId,
                        principalTable: "EventParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimeClockAnomalies_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimeClockAnomalies_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeClockAnomalies_TimeEntries_TimeEntryId",
                        column: x => x.TimeEntryId,
                        principalTable: "TimeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimeClockAnomalies_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeClockAnomalies_EmployeeId_WorkDate",
                table: "TimeClockAnomalies",
                columns: new[] { "EmployeeId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeClockAnomalies_EventId",
                table: "TimeClockAnomalies",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeClockAnomalies_EventParticipantId",
                table: "TimeClockAnomalies",
                column: "EventParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeClockAnomalies_MerchantId_Status",
                table: "TimeClockAnomalies",
                columns: new[] { "MerchantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeClockAnomalies_ReviewedByUserId",
                table: "TimeClockAnomalies",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeClockAnomalies_TimeEntryId",
                table: "TimeClockAnomalies",
                column: "TimeEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeClockAnomalies_Type",
                table: "TimeClockAnomalies",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimeClockAnomalies");
        }
    }
}
