using System.ComponentModel.DataAnnotations;

namespace ProyectoProgra3.DTOs
{
    public class DepositoDto
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public decimal Monto { get; set; }
    }
}
