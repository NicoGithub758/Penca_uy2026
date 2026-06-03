using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class AgregaCostoPencaYCamposPayPal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Costo",
                table: "PencaInstancias",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Pagos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MetodoPago",
                table: "Pagos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$KYsnBpbqKy7UZm6m4l0Z8e/ipyusbsso1w1dM4izTQpWUPMEU.qTu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Costo",
                table: "PencaInstancias");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "MetodoPago",
                table: "Pagos");

            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$H6dXjfSzGWfJgxCp.Bu4k.4mh1tBJxNRdFxd37duB6qrW.3QXpUKm");
        }
    }
}
