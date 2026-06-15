using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarParametrosSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParametrosSistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActualizacionAutomaticaResultadosActiva = table.Column<bool>(type: "bit", nullable: false),
                    MinutosDespuesInicioParaConsultarResultado = table.Column<int>(type: "int", nullable: false),
                    IntervaloMinutosConsultaResultados = table.Column<int>(type: "int", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosSistema", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ParametrosSistema",
                columns: new[] { "Id", "ActualizacionAutomaticaResultadosActiva", "FechaActualizacion", "IntervaloMinutosConsultaResultados", "MinutosDespuesInicioParaConsultarResultado", "TimeZoneId" },
                values: new object[] { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10, 110, "America/Montevideo" });

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$MU3hK0NZ6ekAQlqu796npOT4GatuDi0ut9Ez3/p9eaMJJiD8L.0Ay");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParametrosSistema");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$HV6DI8NXAZTnuj0Ex0Vc8.0AkXg28RZBJqFQHLXBNhx54QxSmvgNi");
        }
    }
}
