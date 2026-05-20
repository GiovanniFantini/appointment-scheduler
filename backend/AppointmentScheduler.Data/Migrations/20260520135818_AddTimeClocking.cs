using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeClocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BranchTimeClockSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ClockingRequired = table.Column<bool>(type: "boolean", nullable: false),
                    GraceInMinutes = table.Column<int>(type: "integer", nullable: false),
                    GraceOutMinutes = table.Column<int>(type: "integer", nullable: false),
                    EarlyClockInToleranceMinutes = table.Column<int>(type: "integer", nullable: false),
                    LateClockOutToleranceMinutes = table.Column<int>(type: "integer", nullable: false),
                    GeofencingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    GeofenceRadiusMeters = table.Column<int>(type: "integer", nullable: false),
                    BreakTrackingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MaxBreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    RoundingMinutes = table.Column<int>(type: "integer", nullable: false),
                    RequirePhoto = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchTimeClockSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchTimeClockSettings_MerchantBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "MerchantBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BranchTimeClockSettings_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    EventParticipantId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    DeviationMinutes = table.Column<int>(type: "integer", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    GpsAccuracyMeters = table.Column<double>(type: "double precision", nullable: true),
                    DistanceFromBranchMeters = table.Column<double>(type: "double precision", nullable: true),
                    GeofenceOk = table.Column<bool>(type: "boolean", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsManualCorrection = table.Column<bool>(type: "boolean", nullable: false),
                    CorrectedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CorrectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeEntries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeEntries_EventParticipants_EventParticipantId",
                        column: x => x.EventParticipantId,
                        principalTable: "EventParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeEntries_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeEntries_MerchantBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "MerchantBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeEntries_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeEntries_Users_CorrectedByUserId",
                        column: x => x.CorrectedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchTimeClockSettings_BranchId",
                table: "BranchTimeClockSettings",
                column: "BranchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchTimeClockSettings_MerchantId",
                table: "BranchTimeClockSettings",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_BranchId_WorkDate",
                table: "TimeEntries",
                columns: new[] { "BranchId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_CorrectedByUserId",
                table: "TimeEntries",
                column: "CorrectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_EmployeeId_WorkDate",
                table: "TimeEntries",
                columns: new[] { "EmployeeId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_EventId",
                table: "TimeEntries",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_EventParticipantId",
                table: "TimeEntries",
                column: "EventParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_MerchantId_WorkDate",
                table: "TimeEntries",
                columns: new[] { "MerchantId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_Status",
                table: "TimeEntries",
                column: "Status");

            // RBAC: rende la feature Timbratura (= 9) visibile nella UI Ruoli per
            // i ruoli già esistenti. La feature parte disabilitata; viene attivata
            // ai ruoli predefiniti (e a quelli che hanno già tutte le altre 8 feature).
            migrationBuilder.Sql(@"
                INSERT INTO ""RoleFeatures"" (""RoleId"", ""Feature"", ""IsEnabled"")
                SELECT r.""Id"", 9,
                       CASE
                         WHEN r.""IsDefault"" = TRUE THEN TRUE
                         WHEN (SELECT COUNT(*) FROM ""RoleFeatures"" rf2
                               WHERE rf2.""RoleId"" = r.""Id"" AND rf2.""IsEnabled"" = TRUE) >= 8 THEN TRUE
                         ELSE FALSE
                       END
                FROM ""MerchantRoles"" r
                WHERE NOT EXISTS (
                          SELECT 1 FROM ""RoleFeatures"" rf
                          WHERE rf.""RoleId"" = r.""Id"" AND rf.""Feature"" = 9);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM ""RoleFeatures"" WHERE ""Feature"" = 9;");

            migrationBuilder.DropTable(
                name: "BranchTimeClockSettings");

            migrationBuilder.DropTable(
                name: "TimeEntries");
        }
    }
}
