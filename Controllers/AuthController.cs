// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.DTOs;
using ProyectoProgra3.Services;

namespace ProyectoProgra3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly BancoService _bancoService;

        public AuthController(BancoService bancoService)
        {
            _bancoService = bancoService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var usuario = _bancoService.ValidarLogin(dto.Id, dto.Pin);

            if (usuario == null)
                return Unauthorized(new { exitoso = false, mensaje = "Credenciales inválidas" });

            return Ok(new
            {
                exitoso = true,
                usuario = new
                {
                    id = usuario.Id,
                    nombre = usuario.Nombre,
                    rol = usuario.Rol,
                    saldoBancario = usuario.SaldoBancario
                }
            });
        }

        [HttpPost("solicitar-recuperacion-pin")]
        public async Task<IActionResult> SolicitarRecuperacionPin([FromBody] SolicitudRecuperacionPinDto dto)
        {
            if (!ModelState.IsValid || dto == null || dto.UsuarioId <= 0)
                return BadRequest(new { exitoso = false, mensaje = "ID de usuario inválido" });

            var resultado = await _bancoService.CrearSolicitudRecuperacionPin(dto.UsuarioId);

            if (!resultado)
                return NotFound(new { exitoso = false, mensaje = "Usuario no encontrado" });

            return Ok(new { exitoso = true, mensaje = "Solicitud de recuperación creada" });
        }
    }
}


