using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSitiosSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$Cjuh0DRxvu5goj1aP0/kj.PuF1lK7oF7I/DzDlR2S4m0ctUH9HUqK");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_LocalEquipoId",
                table: "Partidos",
                column: "LocalEquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_VisitanteEquipoId",
                table: "Partidos",
                column: "VisitanteEquipoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Partidos_Equipos_LocalEquipoId",
                table: "Partidos",
                column: "LocalEquipoId",
                principalTable: "Equipos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Partidos_Equipos_VisitanteEquipoId",
                table: "Partidos",
                column: "VisitanteEquipoId",
                principalTable: "Equipos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Partidos_Equipos_LocalEquipoId",
                table: "Partidos");

            migrationBuilder.DropForeignKey(
                name: "FK_Partidos_Equipos_VisitanteEquipoId",
                table: "Partidos");

            migrationBuilder.DropIndex(
                name: "IX_Partidos_LocalEquipoId",
                table: "Partidos");

            migrationBuilder.DropIndex(
                name: "IX_Partidos_VisitanteEquipoId",
                table: "Partidos");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$60WVU3r69ubtA9HkKEAAMOuKlprpZCM29EossYBRPgZ31KSn/GJve");
        }
    }
}
