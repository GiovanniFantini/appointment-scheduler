using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentScheduler.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClockingRequiredSince : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ClockingRequiredSince",
                table: "BranchTimeClockSettings",
                type: "date",
                nullable: true);

            // Le filiali che hanno già la timbratura obbligatoria non hanno una data
            // di attivazione registrata: senza backfill l'obbligo risulterebbe non in
            // vigore e smetterebbero di generare anomalie. Si usa la data dell'ultima
            // modifica della configurazione, l'approssimazione più vicina al momento
            // in cui l'obbligo è stato acceso.
            migrationBuilder.Sql(@"
                UPDATE ""BranchTimeClockSettings""
                SET ""ClockingRequiredSince"" = COALESCE(""UpdatedAt"", ""CreatedAt"")::date
                WHERE ""IsEnabled"" = TRUE AND ""ClockingRequired"" = TRUE;
            ");

            // Bonifica delle anomalie già generate retroattivamente all'attivazione:
            // sono mancate timbrature su turni in cui l'obbligo non era ancora in
            // vigore, quindi non c'è nulla da giustificare. Si toccano solo quelle
            // ancora aperte: giustificate o revisionate restano storico.
            migrationBuilder.Sql(@"
                DELETE FROM ""TimeClockAnomalies"" a
                USING ""Events"" e, ""BranchTimeClockSettings"" s
                WHERE a.""EventId"" = e.""Id""
                  AND s.""BranchId"" = e.""BranchId""
                  AND a.""Type"" IN (5, 6)
                  AND a.""Status"" = 1
                  AND (s.""ClockingRequiredSince"" IS NULL
                       OR a.""WorkDate"" < s.""ClockingRequiredSince"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClockingRequiredSince",
                table: "BranchTimeClockSettings");
        }
    }
}
