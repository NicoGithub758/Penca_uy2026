using Microsoft.EntityFrameworkCore;
using Penca_uy2026.Models;

namespace Penca_uy2026.Data
{
    // Se hereda de DbContext, que es la clase base de EF Core
    public class PencaDbContext : DbContext
    {
        // Constructor que recibe las opciones de conexión (Inyección de Dependencias)
        public PencaDbContext(DbContextOptions<PencaDbContext> options) : base(options)
        {
        }

        // Representación de las tablas en la base de datos
        public DbSet<Site> Sites { get; set; }
        public DbSet<User> Users { get; set; }
    }
}