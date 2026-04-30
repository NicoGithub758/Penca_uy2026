using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Models;

namespace Penca_uy2026.Data
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

        public DbSet<PlataformaAdmin> PlataformaAdmins { get; set; }
        public DbSet<Deporte> Deportes { get; set; }
        public DbSet<Penca> Pencas { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Partido> Partidos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Mantenemos el ignore para que no falle por el hash dinámico de BCrypt
            optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed del Admin inicial
            modelBuilder.Entity<PlataformaAdmin>().HasData(new PlataformaAdmin
            {
                Id = 1,
                Email = "admin@tupenca.uy",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
            });

            // Seed de Deportes iniciales (puedes agregar más)
            modelBuilder.Entity<Deporte>().HasData(
                new Deporte { Id = 1, Nombre = "Fútbol" },
                new Deporte { Id = 2, Nombre = "Básquetbol" },
                new Deporte { Id = 3, Nombre = "Tenis" },
                new Deporte { Id = 4, Nombre = "Vóleibol" }
            );
        }
    }
}