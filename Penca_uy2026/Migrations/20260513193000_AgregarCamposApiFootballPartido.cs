using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Penca_uy2026.Data;

#nullable disable

namespace Penca_uy2026.Migrations
{
    [DbContext(typeof(MyDbContext))]
    [Migration("20260513193000_AgregarCamposApiFootballPartido")]
    public partial class AgregarCamposApiFootballPartido : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Sitios",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ApiFootballFixtureId",
                table: "Partidos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstadoApi",
                table: "Partidos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Minuto",
                table: "Partidos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaSyncApi",
                table: "Partidos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_ApiFootballFixtureId",
                table: "Partidos",
                column: "ApiFootballFixtureId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Partidos_ApiFootballFixtureId",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "ApiFootballFixtureId",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "EstadoApi",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "Minuto",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "UltimaSyncApi",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Sitios");
        }
    }
}
