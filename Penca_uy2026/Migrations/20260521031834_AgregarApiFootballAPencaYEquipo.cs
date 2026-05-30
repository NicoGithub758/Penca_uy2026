using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarApiFootballAPencaYEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "UsuariosSitio",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "ApiFootballLeagueId",
                table: "Pencas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApiFootballSeason",
                table: "Pencas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApiFootballTeamId",
                table: "Equipos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Equipos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$R4fs18fTyRNaHwTgvSDn6ueq8kM/SPGPQYAb4QtZXdO99IZIWhLwa");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiFootballLeagueId",
                table: "Pencas");

            migrationBuilder.DropColumn(
                name: "ApiFootballSeason",
                table: "Pencas");

            migrationBuilder.DropColumn(
                name: "ApiFootballTeamId",
                table: "Equipos");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Equipos");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "UsuariosSitio",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$XJYwFyQsoePf2PAkwRgXD.Ske1iMlVsHqodB8ikf3CWwXwI2pCIza");
        }
    }
}
