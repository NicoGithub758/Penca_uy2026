using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class ActualizarSitioConSlugYTipoRegistro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsAdminSitio",
                table: "UsuariosSitio");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "UsuariosSitio");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Sitios");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "Sitios");

            migrationBuilder.DropColumn(
                name: "SitioId",
                table: "Predicciones");

            migrationBuilder.DropColumn(
                name: "SitioId",
                table: "Participaciones");

            migrationBuilder.DropColumn(
                name: "SitioId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "SitioId",
                table: "Notificaciones");

            migrationBuilder.DropColumn(
                name: "SitioId",
                table: "MensajesChat");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$7fBNmRk6wlJafWu7wSnxiOQX6emTcWJUAHBnZqf/OSdO0IQoigaUC");
        }
    }
}
