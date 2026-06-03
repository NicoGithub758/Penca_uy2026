using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaReglasPremios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReglasPremios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PencaInstanciaId = table.Column<int>(type: "int", nullable: false),
                    Posicion = table.Column<int>(type: "int", nullable: false),
                    PorcentajeDelPozo = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SitioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglasPremios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReglasPremios_PencaInstancias_PencaInstanciaId",
                        column: x => x.PencaInstanciaId,
                        principalTable: "PencaInstancias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$zQFg7SdiFVp.62X6F/c0SupSPZ8knWFYx6HCfHcxm0oVuGwqYGANe");

            migrationBuilder.CreateIndex(
                name: "IX_ReglasPremios_PencaInstanciaId",
                table: "ReglasPremios",
                column: "PencaInstanciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReglasPremios");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$JF6yA0BTWFRcxl02r1wQOOeX6y7MvUDgEdkJqgLSBxYRKH2HeLIdC");
        }
    }
}
