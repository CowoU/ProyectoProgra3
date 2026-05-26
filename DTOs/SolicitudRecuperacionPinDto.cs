using System.ComponentModel.DataAnnotations;

namespace ProyectoProgra3.DTOs
{
    public class SolicitudRecuperacionPinDto
    {
        [Required]
        public int UsuarioId { get; set; }
    }
}
