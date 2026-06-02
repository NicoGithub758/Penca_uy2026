using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Models;
using Penca_uy2026.Interfaces;
using System.Linq.Expressions;
using System.Reflection;

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
        public DbSet<InvitacionAdmin> InvitacionesAdmin { get; set; }
        public DbSet<Deporte> Deportes { get; set; }
        public DbSet<Penca> Pencas { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Partido> Partidos { get; set; }
        public DbSet<Sitio> Sitios { get; set; } // Sitio suele ser global para poder buscarlo

        // --- Tablas Multi-tenant (Aisladas por Sitio) ---
        public DbSet<PencaInstancia> PencaInstancias { get; set; }
        public DbSet<UsuarioSitio> UsuariosSitio { get; set; }
        public DbSet<Participacion> Participaciones { get; set; }
        public DbSet<Prediccion> Predicciones { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<MensajeChat> MensajesChat { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<Invitacion> Invitaciones { get; set; }
        public DbSet<SolicitudIngreso> SolicitudesIngreso { get; set; }
        public DbSet<PreferenciaNotificacion> PreferenciasNotificacion { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Evitar borrado en cascada para evitar ciclos
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // 2. FILTRO GLOBAL MULTI-TENANT DINÁMICO
            // Este bloque soluciona el error "Equal is not defined for int and int?"
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
                {
                    // Llamamos al método auxiliar definido abajo
                    var method = typeof(MyDbContext)
                        .GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.MakeGenericMethod(entityType.ClrType);

                    method?.Invoke(this, new object[] { modelBuilder });
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

        // Método auxiliar para aplicar el filtro de forma segura
        private void ApplyMutableFilter<T>(ModelBuilder modelBuilder) where T : class, IMultiTenant
        {
            modelBuilder.Entity<T>().HasQueryFilter(e =>
                _currentSitioId == null || e.SitioId == _currentSitioId);
        }


        private void ApplyTenantFilter<T>(ModelBuilder modelBuilder) where T : class, IMultiTenant
        {
            modelBuilder.Entity<T>().HasQueryFilter(e =>
                _currentSitioId == null || e.SitioId == _currentSitioId);
        }
    }


}