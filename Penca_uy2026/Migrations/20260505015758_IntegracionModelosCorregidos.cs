using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Penca_uy2026.Migrations
{
    /// <inheritdoc />
    public partial class IntegracionModelosCorregidos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Deportes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deportes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlataformaAdmins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlataformaAdmins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sitios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sitios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pencas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeporteId = table.Column<int>(type: "int", nullable: false),
                    CantidadEquipos = table.Column<int>(type: "int", nullable: false),
                    Modo = table.Column<int>(type: "int", nullable: false),
                    Finalizada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pencas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pencas_Deportes_DeporteId",
                        column: x => x.DeporteId,
                        principalTable: "Deportes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invitaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SitioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitaciones_Sitios_SitioId",
                        column: x => x.SitioId,
                        principalTable: "Sitios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesIngreso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    SitioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesIngreso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesIngreso_Sitios_SitioId",
                        column: x => x.SitioId,
                        principalTable: "Sitios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosSitio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SitioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosSitio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosSitio_Sitios_SitioId",
                        column: x => x.SitioId,
                        principalTable: "Sitios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PencaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipos_Pencas_PencaId",
                        column: x => x.PencaId,
                        principalTable: "Pencas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Partidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PencaId = table.Column<int>(type: "int", nullable: false),
                    Local = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Visitante = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GolesLocal = table.Column<int>(type: "int", nullable: true),
                    GolesVisitante = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fase = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Jugado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Partidos_Pencas_PencaId",
                        column: x => x.PencaId,
                        principalTable: "Pencas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PencaInstancias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PorcentajeComision = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PencaId = table.Column<int>(type: "int", nullable: false),
                    SitioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PencaInstancias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PencaInstancias_Pencas_PencaId",
                        column: x => x.PencaId,
                        principalTable: "Pencas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PencaInstancias_Sitios_SitioId",
                        column: x => x.SitioId,
                        principalTable: "Sitios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FueLeida = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioSitioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_UsuariosSitio_UsuarioSitioId",
                        column: x => x.UsuarioSitioId,
                        principalTable: "UsuariosSitio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Participaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EstaPagado = table.Column<bool>(type: "bit", nullable: false),
                    PuntajeTotal = table.Column<int>(type: "int", nullable: false),
                    UsuarioSitioId = table.Column<int>(type: "int", nullable: false),
                    PencaInstanciaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participaciones_PencaInstancias_PencaInstanciaId",
                        column: x => x.PencaInstanciaId,
                        principalTable: "PencaInstancias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Participaciones_UsuariosSitio_UsuarioSitioId",
                        column: x => x.UsuarioSitioId,
                        principalTable: "UsuariosSitio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MensajesChat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Contenido = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParticipacionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensajesChat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensajesChat_Participaciones_ParticipacionId",
                        column: x => x.ParticipacionId,
                        principalTable: "Participaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdTransaccionExterna = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParticipacionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Participaciones_ParticipacionId",
                        column: x => x.ParticipacionId,
                        principalTable: "Participaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Predicciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PuntosObtenidos = table.Column<int>(type: "int", nullable: false),
                    GolesEquipoLocal = table.Column<int>(type: "int", nullable: false),
                    GolesEquipoVisitante = table.Column<int>(type: "int", nullable: false),
                    ParticipacionId = table.Column<int>(type: "int", nullable: false),
                    PartidoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predicciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Predicciones_Participaciones_ParticipacionId",
                        column: x => x.ParticipacionId,
                        principalTable: "Participaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Predicciones_Partidos_PartidoId",
                        column: x => x.PartidoId,
                        principalTable: "Partidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Deportes",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Fútbol" },
                    { 2, "Básquetbol" },
                    { 3, "Tenis" },
                    { 4, "Vóleibol" }
                });

            migrationBuilder.InsertData(
                table: "PlataformaAdmins",
                columns: new[] { "Id", "Email", "PasswordHash" },
                values: new object[] { 1, "admin@tupenca.uy", "$2a$11$7fBNmRk6wlJafWu7wSnxiOQX6emTcWJUAHBnZqf/OSdO0IQoigaUC" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_PencaId",
                table: "Equipos",
                column: "PencaId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitaciones_SitioId",
                table: "Invitaciones",
                column: "SitioId");

            migrationBuilder.CreateIndex(
                name: "IX_MensajesChat_ParticipacionId",
                table: "MensajesChat",
                column: "ParticipacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioSitioId",
                table: "Notificaciones",
                column: "UsuarioSitioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ParticipacionId",
                table: "Pagos",
                column: "ParticipacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Participaciones_PencaInstanciaId",
                table: "Participaciones",
                column: "PencaInstanciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Participaciones_UsuarioSitioId",
                table: "Participaciones",
                column: "UsuarioSitioId");

            migrationBuilder.CreateIndex(
                name: "IX_Partidos_PencaId",
                table: "Partidos",
                column: "PencaId");

            migrationBuilder.CreateIndex(
                name: "IX_PencaInstancias_PencaId",
                table: "PencaInstancias",
                column: "PencaId");

            migrationBuilder.CreateIndex(
                name: "IX_PencaInstancias_SitioId",
                table: "PencaInstancias",
                column: "SitioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pencas_DeporteId",
                table: "Pencas",
                column: "DeporteId");

            migrationBuilder.CreateIndex(
                name: "IX_Predicciones_ParticipacionId",
                table: "Predicciones",
                column: "ParticipacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Predicciones_PartidoId",
                table: "Predicciones",
                column: "PartidoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesIngreso_SitioId",
                table: "SolicitudesIngreso",
                column: "SitioId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosSitio_SitioId",
                table: "UsuariosSitio",
                column: "SitioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Equipos");

            migrationBuilder.DropTable(
                name: "Invitaciones");

            migrationBuilder.DropTable(
                name: "MensajesChat");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "PlataformaAdmins");

            migrationBuilder.DropTable(
                name: "Predicciones");

            migrationBuilder.DropTable(
                name: "SolicitudesIngreso");

            migrationBuilder.DropTable(
                name: "Participaciones");

            migrationBuilder.DropTable(
                name: "Partidos");

            migrationBuilder.DropTable(
                name: "PencaInstancias");

            migrationBuilder.DropTable(
                name: "UsuariosSitio");

            migrationBuilder.DropTable(
                name: "Pencas");

            migrationBuilder.DropTable(
                name: "Sitios");

            migrationBuilder.DropTable(
                name: "Deportes");
        }
    }
}
