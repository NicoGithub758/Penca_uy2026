using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPreferenciasNotificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreferenciasNotificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioSitioId = table.Column<int>(type: "int", nullable: false),
                    SitioId = table.Column<int>(type: "int", nullable: false),
                    RecibirResultados = table.Column<bool>(type: "bit", nullable: false),
                    RecibirPartidos = table.Column<bool>(type: "bit", nullable: false),
                    RecibirGenerales = table.Column<bool>(type: "bit", nullable: false),
                    RecibirRanking = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenciasNotificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreferenciasNotificacion_UsuariosSitio_UsuarioSitioId",
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
                value: "$2a$11$NkyAcoQRYnR/7HpOtd4cCuuczx2RbaC13LbVGfMvWKb5CGpOsomNq");

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasNotificacion_UsuarioSitioId",
                table: "PreferenciasNotificacion",
                column: "UsuarioSitioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreferenciasNotificacion");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$HV6DI8NXAZTnuj0Ex0Vc8.0AkXg28RZBJqFQHLXBNhx54QxSmvgNi");
        }
    }
}
