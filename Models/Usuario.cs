using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoProgra3.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(100)]
        public string Nombre { get; set; }

        [Required]
        [Column("pin")]
        [StringLength(10)]
        public string Pin { get; set; }

        [Column("rol")]
        [StringLength(20)]
        public string Rol { get; set; } = "Cliente";

        [Column("saldo_bancario")]
        public decimal SaldoBancario { get; set; } = 0.00m;

        public ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
        public ICollection<SolicitudRecuperacionPin> SolicitudesRecuperacionPin { get; set; } = new List<SolicitudRecuperacionPin>();
    }
}
