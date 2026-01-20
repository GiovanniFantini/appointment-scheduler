using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class hrpayrollsystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HRDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Month = table.Column<int>(type: "integer", nullable: true),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HRDocuments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HRDocuments_Merchants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HRDocuments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HRDocuments_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HRDocumentVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HRDocumentId = table.Column<int>(type: "integer", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    BlobPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ChangeNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    UploadStatus = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRDocumentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HRDocumentVersions_HRDocuments_HRDocumentId",
                        column: x => x.HRDocumentId,
                        principalTable: "HRDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HRDocumentVersions_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HRDocuments_CreatedByUserId",
                table: "HRDocuments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HRDocuments_EmployeeId",
                table: "HRDocuments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_HRDocuments_IsDeleted",
                table: "HRDocuments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_HRDocuments_Status",
                table: "HRDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HRDocuments_TenantId",
                table: "HRDocuments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HRDocuments_TenantId_DocumentType_Year_Month",
                table: "HRDocuments",
                columns: new[] { "TenantId", "DocumentType", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_HRDocuments_TenantId_EmployeeId",
                table: "HRDocuments",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_HRDocuments_UpdatedByUserId",
                table: "HRDocuments",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HRDocumentVersions_HRDocumentId_VersionNumber",
                table: "HRDocumentVersions",
                columns: new[] { "HRDocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HRDocumentVersions_UploadedByUserId",
                table: "HRDocumentVersions",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HRDocumentVersions_UploadStatus",
                table: "HRDocumentVersions",
                column: "UploadStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HRDocumentVersions");

            migrationBuilder.DropTable(
                name: "HRDocuments");
        }
    }
}
