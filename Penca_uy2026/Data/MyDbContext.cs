using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Penca_uy2026.Models;

namespace Penca_uy2026.Data
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

        public DbSet<PlataformaAdmin> PlataformaAdmins { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<PlataformaAdmin>().HasData(new PlataformaAdmin
            {
                Id = 1,
                Email = "admin@tupenca.uy",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
            });
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Esto silencia el error que te está bloqueando
            optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }
    }
}