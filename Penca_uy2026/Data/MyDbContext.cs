using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Models;
using Penca_uy2026.Interfaces;
using System.Linq.Expressions;

namespace Penca_uy2026.Data
{
    public class MyDbContext : DbContext
    {
        private readonly int? _currentSitioId;

        public MyDbContext(DbContextOptions<MyDbContext> options, ITenantService tenantService)
            : base(options)
        {
            _currentSitioId = tenantService.GetTenantId();
        }

        // --- Tablas Globales ---
        public DbSet<PlataformaAdmin> PlataformaAdmins { get; set; }
        public DbSet<Deporte> Deportes { get; set; }
        public DbSet<Penca> Pencas { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Partido> Partidos { get; set; }

        // --- Tablas Multi-tenant (Aisladas por Sitio) ---
        public DbSet<Sitio> Sitios { get; set; }
        public DbSet<PencaInstancia> PencaInstancias { get; set; }
        public DbSet<UsuarioSitio> UsuariosSitio { get; set; }
        public DbSet<Participacion> Participaciones { get; set; }
        public DbSet<Prediccion> Predicciones { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<MensajeChat> MensajesChat { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<Invitacion> Invitaciones { get; set; }
        public DbSet<SolicitudIngreso> SolicitudesIngreso { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Evitar borrado en cascada
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // 2. FILTRO GLOBAL MULTI-TENANT
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var body = Expression.Equal(
                        Expression.Property(parameter, "SitioId"),
                        Expression.Constant(_currentSitioId, typeof(int?)));

                    var filter = Expression.Lambda(body, parameter);
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }

            // 3. SEED: Admin inicial
            modelBuilder.Entity<PlataformaAdmin>().HasData(new PlataformaAdmin
            {
                Id = 1,
                Email = "admin@tupenca.uy",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
            });

            // 4. SEED: Deportes
            modelBuilder.Entity<Deporte>().HasData(
                new Deporte { Id = 1, Nombre = "Fútbol" },
                new Deporte { Id = 2, Nombre = "Básquetbol" },
                new Deporte { Id = 3, Nombre = "Tenis" },
                new Deporte { Id = 4, Nombre = "Vóleibol" }
            );
        }
    }
}