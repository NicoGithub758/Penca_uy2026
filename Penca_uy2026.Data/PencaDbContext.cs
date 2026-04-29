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
        public DbSet<SiteUser> SiteUsers { get; set; }
        public DbSet<Penca> Pencas { get; set; }
        public DbSet<PencaInstance> PencaInstances { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Participation> Participations { get; set; }
        public DbSet<Prediction> Predictions { get; set; }
        public DbSet<AccessRequest> AccessRequests { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PlatformAdmin> PlatformAdmins { get; set; }

        // Se sobreescribe el método de construcción del modelo de EF Core
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Se ejecuta la configuración base obligatoria
            base.OnModelCreating(modelBuilder);

            // Se itera por todas las relaciones de nuestro modelo cambiando el comportamiento de borrado
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                // Restrict: Evita que el borrado de una entidad padre elimine automáticamente a los hijos
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}