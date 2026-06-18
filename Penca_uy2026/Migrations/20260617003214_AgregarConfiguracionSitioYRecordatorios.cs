using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConfiguracionSitioYRecordatorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesSitio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SitioId = table.Column<int>(type: "int", nullable: false),
                    RecordatoriosAutomaticosActivos = table.Column<bool>(type: "bit", nullable: false),
                    HorasAntes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesSitio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesSitio_Sitios_SitioId",
                        column: x => x.SitioId,
                        principalTable: "Sitios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecordatoriosPartidoSitio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartidoId = table.Column<int>(type: "int", nullable: false),
                    SitioId = table.Column<int>(type: "int", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CantidadEnviados = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordatoriosPartidoSitio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordatoriosPartidoSitio_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecordatoriosPartidoSitio_Sitios_SitioId",
                        column: x => x.SitioId,
                        principalTable: "Sitios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$f/h5mYKfgAnywDOetTJp7uxjQIO1xrZJYn31Kcz/ZHM9fC/4/61wa");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_LocalEquipoId",
                table: "Partidos",
                column: "LocalEquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_VisitanteEquipoId",
                table: "Partidos",
                column: "VisitanteEquipoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesSitio_SitioId",
                table: "ConfiguracionesSitio",
                column: "SitioId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordatoriosPartidoSitio_PartidoId",
                table: "RecordatoriosPartidoSitio",
                column: "PartidoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordatoriosPartidoSitio_SitioId",
                table: "RecordatoriosPartidoSitio",
                column: "SitioId");

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

            migrationBuilder.DropTable(
                name: "ConfiguracionesSitio");

            migrationBuilder.DropTable(
                name: "RecordatoriosPartidoSitio");

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
