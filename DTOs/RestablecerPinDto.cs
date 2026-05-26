// DTOs/RestablecerPinDto.cs
using System.ComponentModel.DataAnnotations;

namespace ProyectoProgra3.DTOs
{
    public class RestablecerPinDto
    {
        [Required]
        public string NuevoPin { get; set; }
    }
}
