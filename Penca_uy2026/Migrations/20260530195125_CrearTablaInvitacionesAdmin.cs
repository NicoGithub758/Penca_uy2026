using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaInvitacionesAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvitacionesAdmin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioSitioId = table.Column<int>(type: "int", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Usado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitacionesAdmin", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvitacionesAdmin_UsuariosSitio_UsuarioSitioId",
                        column: x => x.UsuarioSitioId,
                        principalTable: "UsuariosSitio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$JF6yA0BTWFRcxl02r1wQOOeX6y7MvUDgEdkJqgLSBxYRKH2HeLIdC");

            migrationBuilder.CreateIndex(
                name: "IX_InvitacionesAdmin_UsuarioSitioId",
                table: "InvitacionesAdmin",
                column: "UsuarioSitioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvitacionesAdmin");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$HV6DI8NXAZTnuj0Ex0Vc8.0AkXg28RZBJqFQHLXBNhx54QxSmvgNi");
        }
    }
}
