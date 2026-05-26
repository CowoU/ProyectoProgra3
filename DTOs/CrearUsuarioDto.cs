using System.ComponentModel.DataAnnotations;

namespace ProyectoProgra3.DTOs
{
    public class CrearUsuarioDto
    {
        [Required]
        [StringLength(200)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(50)]
        public string Pin { get; set; }

        [Required]
        [StringLength(50)]
        public string Rol { get; set; }

        [Required]
        public decimal SaldoBancario { get; set; }
    }
}
