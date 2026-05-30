using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class ActualizarPartidoConEquiposApi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApiFootballFixtureId",
                table: "Partidos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocalEquipoId",
                table: "Partidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VisitanteEquipoId",
                table: "Partidos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE p
                SET LocalEquipoId = e.Id
                FROM Partidos p
                INNER JOIN Equipos e ON e.PencaId = p.PencaId AND e.Nombre = p.Local;
                """);

            migrationBuilder.Sql("""
                UPDATE p
                SET VisitanteEquipoId = e.Id
                FROM Partidos p
                INNER JOIN Equipos e ON e.PencaId = p.PencaId AND e.Nombre = p.Visitante;
                """);

            migrationBuilder.DropColumn(
                name: "Local",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "Visitante",
                table: "Partidos");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$q64BgRQmJlSYgnPKozNcWOkGZN1oYtEDqPn.hkDhq29u1iVeVpebC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Local",
                table: "Partidos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Visitante",
                table: "Partidos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE p
                SET Local = e.Nombre
                FROM Partidos p
                INNER JOIN Equipos e ON e.Id = p.LocalEquipoId;
                """);

            migrationBuilder.Sql("""
                UPDATE p
                SET Visitante = e.Nombre
                FROM Partidos p
                INNER JOIN Equipos e ON e.Id = p.VisitanteEquipoId;
                """);

            migrationBuilder.DropColumn(
                name: "ApiFootballFixtureId",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "LocalEquipoId",
                table: "Partidos");

            migrationBuilder.DropColumn(
                name: "VisitanteEquipoId",
                table: "Partidos");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$R4fs18fTyRNaHwTgvSDn6ueq8kM/SPGPQYAb4QtZXdO99IZIWhLwa");
        }
    }
}
