using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarOrigenYCupos : Migration
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
                name: "Origen",
                table: "UsuariosSitio",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "FuePorInvitacion",
                table: "SolicitudesIngreso",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UsosDisponibles",
                table: "Invitaciones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$Ob92BxENYujRuywio67lWOQDDOyfOF2P/90fbiqzX70XFTsZQbjt6");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Origen",
                table: "UsuariosSitio");

            migrationBuilder.DropColumn(
                name: "FuePorInvitacion",
                table: "SolicitudesIngreso");

            migrationBuilder.DropColumn(
                name: "UsosDisponibles",
                table: "Invitaciones");

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
