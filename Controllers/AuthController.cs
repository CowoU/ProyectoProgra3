using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.Services;

namespace ProyectoProgra3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly PagoService _pagoService;

        public AuthController(PagoService pagoService)
        {
            _pagoService = pagoService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request == null || request.Id <= 0 || string.IsNullOrWhiteSpace(request.Pin))
            {
                return BadRequest(new { mensaje = "ID y PIN son requeridos" });
            }

            var usuario = _pagoService.AutenticarUsuario(request.Id, request.Pin);

            if (usuario == null)
            {
                return Unauthorized(new { mensaje = "ID o PIN inválidos" });
            }

            // IMPORTANTE: Devolvemos la estructura que el frontend espera
            return Ok(new
            {
                exitoso = true,
                usuario = new
                {
                    id = usuario.Id,
                    nombre = usuario.Nombre,
                    rol = usuario.Rol,
                    // Creamos este objeto 'cuenta' aunque el saldo esté en 'usuario'
                    // para que el frontend no falle al buscar usuario.cuenta.saldo
                    cuenta = new
                    {
                        id = usuario.Id,
                        saldo = usuario.SaldoBancario
                    }
                },
                mensaje = "Login exitoso"
            });
        }
    }

    public class LoginRequest
    {
        public int Id { get; set; }
        public string Pin { get; set; }
    }
}