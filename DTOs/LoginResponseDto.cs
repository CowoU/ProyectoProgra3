namespace ProyectoProgra3.DTOs
{
    public class LoginResponseDto
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public UsuarioResponseDto Usuario { get; set; }
    }

    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public decimal SaldoBancario { get; set; }
    }
}
