using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregaCamposMobile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "UsuariosSitio",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Auth0Id",
                table: "UsuariosSitio",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FcmToken",
                table: "UsuariosSitio",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistro",
                table: "UsuariosSitio",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "UsuariosSitio",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rol",
                table: "UsuariosSitio",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Sitios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ColorPrincipal",
                table: "Sitios",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Sitios",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Sitios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoRegistro",
                table: "Sitios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$9MmgdIv7kW8MVd4k3Kk4Ee9cyOvKuaRTMAOMJerW4iBqpZVhGdkzK");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "UsuariosSitio");

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
                name: "PasswordHash",
                table: "UsuariosSitio");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "UsuariosSitio");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Sitios");

            migrationBuilder.DropColumn(
                name: "ColorPrincipal",
                table: "Sitios");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Sitios");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Sitios");

            migrationBuilder.DropColumn(
                name: "TipoRegistro",
                table: "Sitios");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$7fBNmRk6wlJafWu7wSnxiOQX6emTcWJUAHBnZqf/OSdO0IQoigaUC");
        }
    }
}
