using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Penca_uy2026.Data;

#nullable disable

namespace Penca_uy2026.Migrations
{
    [DbContext(typeof(MyDbContext))]
    [Migration("20260617120000_AgregarParametrosPuntajePredicciones")]
    public partial class AgregarParametrosPuntajePredicciones : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PuntosResultadoExacto",
                table: "ParametrosSistema",
                type: "int",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.AddColumn<int>(
                name: "PuntosGanadorDiferenciaGoles",
                table: "ParametrosSistema",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<int>(
                name: "PuntosGanadorEmpate",
                table: "ParametrosSistema",
                type: "int",
                nullable: false,
                defaultValue: 3);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PuntosResultadoExacto",
                table: "ParametrosSistema");

            migrationBuilder.DropColumn(
                name: "PuntosGanadorDiferenciaGoles",
                table: "ParametrosSistema");

            migrationBuilder.DropColumn(
                name: "PuntosGanadorEmpate",
                table: "ParametrosSistema");
        }
    }
}
