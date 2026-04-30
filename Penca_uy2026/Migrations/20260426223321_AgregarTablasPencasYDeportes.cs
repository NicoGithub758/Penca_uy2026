using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTablasPencasYDeportes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Deportes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deportes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pencas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    DeporteId = table.Column<int>(type: "integer", nullable: false),
                    CantidadEquipos = table.Column<int>(type: "integer", nullable: false),
                    Modo = table.Column<int>(type: "integer", nullable: false),
                    Finalizada = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pencas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pencas_Deportes_DeporteId",
                        column: x => x.DeporteId,
                        principalTable: "Deportes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    PencaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipos_Pencas_PencaId",
                        column: x => x.PencaId,
                        principalTable: "Pencas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Partidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PencaId = table.Column<int>(type: "integer", nullable: false),
                    Local = table.Column<string>(type: "text", nullable: false),
                    Visitante = table.Column<string>(type: "text", nullable: false),
                    GolesLocal = table.Column<int>(type: "integer", nullable: true),
                    GolesVisitante = table.Column<int>(type: "integer", nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fase = table.Column<string>(type: "text", nullable: false),
                    Jugado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Partidos_Pencas_PencaId",
                        column: x => x.PencaId,
                        principalTable: "Pencas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Deportes",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Fútbol" },
                    { 2, "Básquetbol" },
                    { 3, "Tenis" },
                    { 4, "Vóleibol" }
                });

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$kMhqSS6jw1fkuJrGnb3pxe6FaYXFoPpv5UU0uekkWgrr/HvifCS.q");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_PencaId",
                table: "Equipos",
                column: "PencaId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_PencaId",
                table: "Partidos",
                column: "PencaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pencas_DeporteId",
                table: "Pencas",
                column: "DeporteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Equipos");

            migrationBuilder.DropTable(
                name: "Partidos");

            migrationBuilder.DropTable(
                name: "Pencas");

            migrationBuilder.DropTable(
                name: "Deportes");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$O98kmr4BifIo.yO3kIguQeg3Z3twpuy7qG39RNbXJv4Qc3KyDcJhS");
        }
    }
}
