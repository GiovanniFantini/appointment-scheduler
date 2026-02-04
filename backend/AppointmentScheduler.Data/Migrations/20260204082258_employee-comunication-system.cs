using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class employeecomunicationsystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    AuthorUserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardMessages_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardMessages_Users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BoardMessageReads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BoardMessageId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardMessageReads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardMessageReads_BoardMessages_BoardMessageId",
                        column: x => x.BoardMessageId,
                        principalTable: "BoardMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardMessageReads_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardMessageReads_BoardMessageId_EmployeeId",
                table: "BoardMessageReads",
                columns: new[] { "BoardMessageId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardMessageReads_EmployeeId",
                table: "BoardMessageReads",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMessages_AuthorUserId",
                table: "BoardMessages",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMessages_ExpiresAt",
                table: "BoardMessages",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMessages_IsActive",
                table: "BoardMessages",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMessages_MerchantId",
                table: "BoardMessages",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardMessages_MerchantId_IsActive_IsPinned",
                table: "BoardMessages",
                columns: new[] { "MerchantId", "IsActive", "IsPinned" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardMessageReads");

            migrationBuilder.DropTable(
                name: "BoardMessages");
        }
    }
}
