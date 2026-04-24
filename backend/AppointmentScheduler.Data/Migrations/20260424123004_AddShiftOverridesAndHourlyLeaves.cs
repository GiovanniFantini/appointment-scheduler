using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftOverridesAndHourlyLeaves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTimeOverride",
                table: "EventParticipants",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParticipantNotes",
                table: "EventParticipants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTimeOverride",
                table: "EventParticipants",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "EmployeeRequests",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "EmployeeRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "EmployeeRequests",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRequests_EventId",
                table: "EmployeeRequests",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeRequests_Events_EventId",
                table: "EmployeeRequests",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeRequests_Events_EventId",
                table: "EmployeeRequests");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeRequests_EventId",
                table: "EmployeeRequests");

            migrationBuilder.DropColumn(
                name: "EndTimeOverride",
                table: "EventParticipants");

            migrationBuilder.DropColumn(
                name: "ParticipantNotes",
                table: "EventParticipants");

            migrationBuilder.DropColumn(
                name: "StartTimeOverride",
                table: "EventParticipants");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "EmployeeRequests");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "EmployeeRequests");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "EmployeeRequests");
        }
    }
}
