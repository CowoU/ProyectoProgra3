using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoProgra3.Models
{
    [Table("transacciones")]
    public class Transaccion
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario Usuario { get; set; }

        [Required]
        [Column("concepto")]
        [StringLength(150)]
        public string Concepto { get; set; }

        [Required]
        [Column("monto_total")]
        public decimal MontoTotal { get; set; }

        [Required]
        [Column("comision_banco")]
        public decimal ComisionBanco { get; set; }

        [Required]
        [Column("pago_empresa")]
        public decimal PagoEmpresa { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
