using Microsoft.EntityFrameworkCore;
using ProyectoProgra3.Models;

namespace ProyectoProgra3.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<Cuota> Cuotas { get; set; }
        public DbSet<SolicitudRecuperacionPin> SolicitudesRecuperacionPin { get; set; }
    }
}