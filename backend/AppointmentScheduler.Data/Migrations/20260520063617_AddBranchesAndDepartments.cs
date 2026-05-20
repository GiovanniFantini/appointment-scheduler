using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchesAndDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AppliesToAllBranches",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "EventParticipants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeBranchId",
                table: "EmployeeMemberships",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HomeDepartmentId",
                table: "EmployeeMemberships",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MerchantBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    IsHeadquarters = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchantBranches_Merchants_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "Merchants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    MerchantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_MerchantBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "MerchantBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeBranchAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MembershipId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeBranchAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeBranchAccess_EmployeeMemberships_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "EmployeeMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeBranchAccess_MerchantBranches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "MerchantBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_BranchId_StartDate",
                table: "Events",
                columns: new[] { "BranchId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_DepartmentId",
                table: "Events",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_DepartmentId",
                table: "EventParticipants",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMemberships_HomeBranchId",
                table: "EmployeeMemberships",
                column: "HomeBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMemberships_HomeDepartmentId",
                table: "EmployeeMemberships",
                column: "HomeDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_BranchId",
                table: "Departments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_BranchId_Name",
                table: "Departments",
                columns: new[] { "BranchId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_MerchantId",
                table: "Departments",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBranchAccess_BranchId",
                table: "EmployeeBranchAccess",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBranchAccess_MembershipId_BranchId",
                table: "EmployeeBranchAccess",
                columns: new[] { "MembershipId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MerchantBranches_MerchantId",
                table: "MerchantBranches",
                column: "MerchantId");

            // ── Data backfill (non-breaking) ──────────────────────────────────
            // Le colonne Events.BranchId ed EmployeeMemberships.HomeBranchId sono state
            // create con default 0. Qui, PRIMA di aggiungere le FK, creiamo per ogni
            // merchant una filiale HQ e ricolleghiamo eventi e membership esistenti.

            // 1. Una filiale HQ per ogni merchant, con anagrafica copiata dal merchant.
            migrationBuilder.Sql(@"
                INSERT INTO ""MerchantBranches""
                    (""MerchantId"", ""Name"", ""Address"", ""City"", ""PostalCode"",
                     ""Country"", ""Phone"", ""IsHeadquarters"", ""IsActive"", ""CreatedAt"")
                SELECT
                    m.""Id"",
                    COALESCE(NULLIF(TRIM(m.""CompanyName""), ''), 'Sede principale'),
                    m.""Address"", m.""City"", m.""PostalCode"", m.""Country"", m.""Phone"",
                    TRUE, TRUE, NOW() AT TIME ZONE 'UTC'
                FROM ""Merchants"" m;
            ");

            // 2. Tutti gli eventi del merchant puntano alla sua HQ.
            migrationBuilder.Sql(@"
                UPDATE ""Events"" e
                SET ""BranchId"" = b.""Id""
                FROM ""MerchantBranches"" b
                WHERE b.""MerchantId"" = e.""MerchantId"" AND b.""IsHeadquarters"" = TRUE;
            ");

            // 3. Tutte le membership del merchant hanno la HQ come sede primaria.
            migrationBuilder.Sql(@"
                UPDATE ""EmployeeMemberships"" em
                SET ""HomeBranchId"" = b.""Id""
                FROM ""MerchantBranches"" b
                WHERE b.""MerchantId"" = em.""MerchantId"" AND b.""IsHeadquarters"" = TRUE;
            ");

            // 4. RBAC: abilita la feature Filiali (= 8) ai ruoli predefiniti esistenti
            //    (e a quelli che hanno già tutte le altre 7 feature attive).
            migrationBuilder.Sql(@"
                INSERT INTO ""RoleFeatures"" (""RoleId"", ""Feature"", ""IsEnabled"")
                SELECT r.""Id"", 8, TRUE
                FROM ""MerchantRoles"" r
                WHERE NOT EXISTS (
                          SELECT 1 FROM ""RoleFeatures"" rf
                          WHERE rf.""RoleId"" = r.""Id"" AND rf.""Feature"" = 8)
                  AND (
                        r.""IsDefault"" = TRUE
                        OR (SELECT COUNT(*) FROM ""RoleFeatures"" rf2
                            WHERE rf2.""RoleId"" = r.""Id"" AND rf2.""IsEnabled"" = TRUE) >= 7
                      );
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeMemberships_Departments_HomeDepartmentId",
                table: "EmployeeMemberships",
                column: "HomeDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeMemberships_MerchantBranches_HomeBranchId",
                table: "EmployeeMemberships",
                column: "HomeBranchId",
                principalTable: "MerchantBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EventParticipants_Departments_DepartmentId",
                table: "EventParticipants",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Departments_DepartmentId",
                table: "Events",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_MerchantBranches_BranchId",
                table: "Events",
                column: "BranchId",
                principalTable: "MerchantBranches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeMemberships_Departments_HomeDepartmentId",
                table: "EmployeeMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeMemberships_MerchantBranches_HomeBranchId",
                table: "EmployeeMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_EventParticipants_Departments_DepartmentId",
                table: "EventParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Departments_DepartmentId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_MerchantBranches_BranchId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "EmployeeBranchAccess");

            migrationBuilder.DropTable(
                name: "MerchantBranches");

            migrationBuilder.DropIndex(
                name: "IX_Events_BranchId_StartDate",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_DepartmentId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_EventParticipants_DepartmentId",
                table: "EventParticipants");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeMemberships_HomeBranchId",
                table: "EmployeeMemberships");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeMemberships_HomeDepartmentId",
                table: "EmployeeMemberships");

            migrationBuilder.DropColumn(
                name: "AppliesToAllBranches",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "EventParticipants");

            migrationBuilder.DropColumn(
                name: "HomeBranchId",
                table: "EmployeeMemberships");

            migrationBuilder.DropColumn(
                name: "HomeDepartmentId",
                table: "EmployeeMemberships");
        }
    }
}
