using System.ComponentModel.DataAnnotations;

namespace ProyectoProgra3.DTOs
{
    public class LoginDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string Pin { get; set; }
    }
}
