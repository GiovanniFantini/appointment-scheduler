using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHRDocumentAccessTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HRDocumentAcknowledgements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HRDocumentVersionId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRDocumentAcknowledgements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HRDocumentAcknowledgements_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HRDocumentAcknowledgements_HRDocumentVersions_HRDocumentVer~",
                        column: x => x.HRDocumentVersionId,
                        principalTable: "HRDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HRDocumentDownloads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HRDocumentVersionId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRDocumentDownloads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HRDocumentDownloads_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HRDocumentDownloads_HRDocumentVersions_HRDocumentVersionId",
                        column: x => x.HRDocumentVersionId,
                        principalTable: "HRDocumentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HRDocumentAcknowledgements_EmployeeId",
                table: "HRDocumentAcknowledgements",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_HRDocumentAcknowledgements_HRDocumentVersionId_EmployeeId",
                table: "HRDocumentAcknowledgements",
                columns: new[] { "HRDocumentVersionId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HRDocumentDownloads_EmployeeId",
                table: "HRDocumentDownloads",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_HRDocumentDownloads_HRDocumentVersionId_EmployeeId",
                table: "HRDocumentDownloads",
                columns: new[] { "HRDocumentVersionId", "EmployeeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HRDocumentAcknowledgements");

            migrationBuilder.DropTable(
                name: "HRDocumentDownloads");
        }
    }
}
