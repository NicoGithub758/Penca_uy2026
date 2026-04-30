using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class SistemaPencasCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$O98kmr4BifIo.yO3kIguQeg3Z3twpuy7qG39RNbXJv4Qc3KyDcJhS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PlataformaAdmins",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$25WrGtvqkAe1iK8m5nDcIeAls.6f6tu3M7u/w4wdUafkOlvDMFBYe");
        }
    }
}
