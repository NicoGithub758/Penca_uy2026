using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTrackingConsultaApiPartidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiFootballStatus",
                table: "Partidos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntentosConsultaApi",
                table: "Partidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaConsultaApi",
                table: "Partidos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$akRR6DJAiMMD78At0PA.suwe0PytqK7T4mZ2SMXMcy8rhJ79t8ota");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiFootballStatus",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "IntentosConsultaApi",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "UltimaConsultaApi",
                table: "Partidos");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$EpvR.PFPq1gI2Ab0//H5YeBLmGpWdS4NoUJ1/W7qw3e/.08q3uB1u");
        }
    }
}
