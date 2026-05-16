using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class ManualFixUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Agregamos manualmente las columnas que el Snapshot cree que existen pero la DB no
            // Nota: Se quitaron todas las demás porque ya existían.
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Sitios",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$XJYwFyQsoePf2PAkwRgXD.Ske1iMlVsHqodB8ikf3CWwXwI2pCIza");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Auth0Id",
                table: "UsuariosSitio");

            migrationBuilder.DropColumn(
                name: "FcmToken",
                table: "UsuariosSitio");

            migrationBuilder.DropColumn(
                name: "FechaRegistro",
                table: "UsuariosSitio");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "UsuariosSitio");

            migrationBuilder.DropColumn(
                name: "ColorPrincipal",
                table: "Sitios");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Sitios");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Sitios");

            migrationBuilder.RenameColumn(
                name: "Activo",
                table: "UsuariosSitio",
                newName: "EsAdminSitio");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$hBiA7SwPidKSK9cHH.SqIecpurfiygnakfrfxNRYgAE5kf4rdy.QW");
        }
    }
}
