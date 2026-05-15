using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoProgra3.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; }

        [Column("pin")]
        public string Pin { get; set; }

        [Column("rol")]
        public string Rol { get; set; }

        [Column("saldo_bancario")]
        public decimal SaldoBancario { get; set; }
    }
}
