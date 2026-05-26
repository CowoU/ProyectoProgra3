using Microsoft.EntityFrameworkCore;
using ProyectoProgra3.Models;

namespace ProyectoProgra3.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }
        public DbSet<SolicitudRecuperacionPin> SolicitudesRecuperacionPin { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>()
                .Property(u => u.SaldoBancario)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.MontoTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.ComisionBanco)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Transaccion>()
                .Property(t => t.PagoEmpresa)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.Transacciones)
                .WithOne(t => t.Usuario)
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Usuario>()
                .HasMany(u => u.SolicitudesRecuperacionPin)
                .WithOne(s => s.Usuario)
                .HasForeignKey(s => s.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
