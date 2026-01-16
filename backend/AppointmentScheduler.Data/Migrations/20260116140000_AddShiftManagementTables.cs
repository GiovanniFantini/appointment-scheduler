using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftManagementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create ShiftTemplates table
            migrationBuilder.CreateTable(
                name: "ShiftTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShiftType = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BreakDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RecurrencePattern = table.Column<int>(type: "integer", nullable: false),
                    RecurrenceDays = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftTemplates_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create Shifts table
            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    ShiftTemplateId = table.Column<int>(type: "integer", nullable: true),
                    EmployeeId = table.Column<int>(type: "integer", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BreakDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    ShiftType = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    IsCheckedIn = table.Column<bool>(type: "boolean", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCheckedOut = table.Column<bool>(type: "boolean", nullable: false),
                    CheckOutTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shifts_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Shifts_ShiftTemplates_ShiftTemplateId",
                        column: x => x.ShiftTemplateId,
                        principalTable: "ShiftTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Shifts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create ShiftSwapRequests table
            migrationBuilder.CreateTable(
                name: "ShiftSwapRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    RequestingEmployeeId = table.Column<int>(type: "integer", nullable: false),
                    TargetEmployeeId = table.Column<int>(type: "integer", nullable: true),
                    OfferedShiftId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResponseMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequiresMerchantApproval = table.Column<bool>(type: "boolean", nullable: false),
                    MerchantResponseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByMerchantId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Employees_RequestingEmployeeId",
                        column: x => x.RequestingEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Employees_TargetEmployeeId",
                        column: x => x.TargetEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Shifts_OfferedShiftId",
                        column: x => x.OfferedShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create EmployeeWorkingHoursLimits table
            migrationBuilder.CreateTable(
                name: "EmployeeWorkingHoursLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    MaxHoursPerDay = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    MaxHoursPerWeek = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    MaxHoursPerMonth = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    MinHoursPerWeek = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    MinHoursPerMonth = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    AllowOvertime = table.Column<bool>(type: "boolean", nullable: false),
                    MaxOvertimeHoursPerWeek = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    MaxOvertimeHoursPerMonth = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWorkingHoursLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkingHoursLimits_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkingHoursLimits_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_ShiftTemplates_MerchantId",
                table: "ShiftTemplates",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_MerchantId",
                table: "Shifts",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_EmployeeId",
                table: "Shifts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_Date",
                table: "Shifts",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_EmployeeId_Date",
                table: "Shifts",
                columns: new[] { "EmployeeId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_ShiftId",
                table: "ShiftSwapRequests",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_RequestingEmployeeId",
                table: "ShiftSwapRequests",
                column: "RequestingEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_Status",
                table: "ShiftSwapRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkingHoursLimits_EmployeeId",
                table: "EmployeeWorkingHoursLimits",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkingHoursLimits_EmployeeId_ValidFrom_ValidTo",
                table: "EmployeeWorkingHoursLimits",
                columns: new[] { "EmployeeId", "ValidFrom", "ValidTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ShiftSwapRequests");
            migrationBuilder.DropTable(name: "EmployeeWorkingHoursLimits");
            migrationBuilder.DropTable(name: "Shifts");
            migrationBuilder.DropTable(name: "ShiftTemplates");
        }
    }
}
