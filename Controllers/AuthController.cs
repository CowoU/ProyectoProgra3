using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.Services;
using ProyectoProgra3.Models;

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
                    cuenta = new
                    {
                        id = usuario.Id,
                        saldo = usuario.SaldoBancario
                    }
                },
                mensaje = "Login exitoso"
            });
        }

        [HttpPost("crear-usuario")]
        public IActionResult CrearUsuario([FromBody] CrearUsuarioRequest request)
        {
            if (request == null || request.Id <= 0 || string.IsNullOrWhiteSpace(request.Nombre) || 
                string.IsNullOrWhiteSpace(request.Pin) || string.IsNullOrWhiteSpace(request.Rol))
            {
                return BadRequest(new { exitoso = false, mensaje = "ID, Nombre, PIN y Rol son requeridos" });
            }

            var resultado = _pagoService.CrearUsuario(request.Id, request.Nombre, request.Pin, request.Rol, request.SaldoBancario);

            if (resultado.Exitoso)
            {
                return Ok(new
                {
                    exitoso = true,
                    mensaje = resultado.Mensaje,
                    usuario = resultado.Usuario
                });
            }
            else
            {
                return BadRequest(new { exitoso = false, mensaje = resultado.Mensaje });
            }
        }

        [HttpGet("obtener-max-id")]
        public IActionResult ObtenerMaxId()
        {
            var maxId = _pagoService.ObtenerMaxIdUsuario();
            return Ok(new { maxId = maxId });
        }
    }

    public class LoginRequest
    {
        public int Id { get; set; }
        public string Pin { get; set; }
    }

    public class CrearUsuarioRequest
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Pin { get; set; }
        public string Rol { get; set; }
        public decimal SaldoBancario { get; set; } = 0m;
    }
}