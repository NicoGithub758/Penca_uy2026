using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposApiFootball : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiFootballCountry",
                table: "Pencas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiFootballLeagueName",
                table: "Pencas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Equipos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pais",
                table: "Equipos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$EpvR.PFPq1gI2Ab0//H5YeBLmGpWdS4NoUJ1/W7qw3e/.08q3uB1u");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiFootballCountry",
                table: "Pencas");

            migrationBuilder.DropColumn(
                name: "ApiFootballLeagueName",
                table: "Pencas");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "Pais",
                table: "Equipos");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$q64BgRQmJlSYgnPKozNcWOkGZN1oYtEDqPn.hkDhq29u1iVeVpebC");
        }
    }
}
