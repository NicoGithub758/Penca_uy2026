using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Penca_uy2026.Data;

#nullable disable

namespace Penca_uy2026.Migrations
{
    [DbContext(typeof(MyDbContext))]
    [Migration("20260618120000_AgregarPorcentajeComisionSistema")]
    public partial class AgregarPorcentajeComisionSistema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeComisionPenca",
                table: "ParametrosSistema",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 5m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PorcentajeComisionPenca",
                table: "ParametrosSistema");
        }
    }
}
