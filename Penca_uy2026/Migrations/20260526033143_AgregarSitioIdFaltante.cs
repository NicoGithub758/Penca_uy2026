using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    public partial class AgregarSitioIdFaltante : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SitioId", table: "Participaciones",
                nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SitioId", table: "Pagos",
                nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SitioId", table: "Predicciones",
                nullable: false, defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SitioId", table: "MensajesChat",
                nullable: false, defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE p SET p.SitioId = pi.SitioId
                FROM Participaciones p
                INNER JOIN PencaInstancias pi ON p.PencaInstanciaId = pi.Id;
            ");

            migrationBuilder.Sql(@"
                UPDATE pg SET pg.SitioId = pa.SitioId
                FROM Pagos pg
                INNER JOIN Participaciones pa ON pg.ParticipacionId = pa.Id;
            ");

            migrationBuilder.Sql(@"
                UPDATE pr SET pr.SitioId = pa.SitioId
                FROM Predicciones pr
                INNER JOIN Participaciones pa ON pr.ParticipacionId = pa.Id;
            ");

            migrationBuilder.Sql(@"
                UPDATE mc SET mc.SitioId = pa.SitioId
                FROM MensajesChat mc
                INNER JOIN Participaciones pa ON mc.ParticipacionId = pa.Id;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SitioId", table: "Participaciones");
            migrationBuilder.DropColumn(name: "SitioId", table: "Pagos");
            migrationBuilder.DropColumn(name: "SitioId", table: "Predicciones");
            migrationBuilder.DropColumn(name: "SitioId", table: "MensajesChat");
        }
    }
}
