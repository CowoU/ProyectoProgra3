using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.DTOs;
using ProyectoProgra3.Services;

namespace ProyectoProgra3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly BancoService _bancoService;

        public UsuariosController(BancoService bancoService)
        {
            _bancoService = bancoService;
        }

        private bool EsAdmin(int usuarioId)
        {
            var usuario = _bancoService.ObtenerUsuarioPorId(usuarioId);
            return usuario?.Rol == "Admin";
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto dto, [FromQuery] int adminId)
        {
            if (!EsAdmin(adminId))
                return Forbid("Solo administradores pueden crear usuarios");

            var resultado = await _bancoService.CrearUsuario(dto.Nombre, dto.Pin, dto.Rol, dto.SaldoBancario);

            if (!resultado)
                return BadRequest(new { exitoso = false, mensaje = "Error al crear usuario" });

            return Ok(new { exitoso = true, mensaje = "Usuario creado exitosamente" });
        }

        [HttpGet("todos")]
        public async Task<IActionResult> ObtenerTodos([FromQuery] int adminId)
        {
            if (!EsAdmin(adminId))
                return Forbid("Solo administradores pueden ver usuarios");

            var usuarios = await _bancoService.ObtenerTodosLosUsuarios();
            return Ok(new { exitoso = true, usuarios });
        }

        [HttpPut("actualizar-pin/{usuarioId}")]
        public async Task<IActionResult> ActualizarPin(int usuarioId, [FromBody] dynamic dto, [FromQuery] int adminId)
        {
            // Ahora se requiere validar la contraseña (PIN) del admin que está en sesión
            string adminPin = dto?.AdminPin;
            string nuevoPin = dto?.NuevoPin;

            if (string.IsNullOrEmpty(adminPin))
                return Forbid("Se requiere PIN del administrador para esta operación");

            var admin = _bancoService.ValidarLogin(adminId, adminPin);
            if (admin == null || admin.Rol != "Admin")
                return Forbid("Credenciales de administrador inválidas");

            var resultado = await _bancoService.ActualizarPinUsuario(usuarioId, nuevoPin);

            if (!resultado)
                return NotFound(new { exitoso = false, mensaje = "Usuario no encontrado" });

            return Ok(new { exitoso = true, mensaje = "PIN actualizado" });
        }

        [HttpGet("solicitudes-pendientes")]
        public async Task<IActionResult> ObtenerSolicitudesPendientes([FromQuery] int adminId)
        {
            if (!EsAdmin(adminId))
                return Forbid("Solo administradores pueden ver solicitudes");

            var solicitudes = await _bancoService.ObtenerSolicitudesPendientes();
            return Ok(new { exitoso = true, cantidad = solicitudes.Count, solicitudes });
        }

        [HttpPost("marcar-solicitud-resuelta/{solicitudId}")]
        public async Task<IActionResult> MarcarSolicitudResuelta(int solicitudId, [FromBody] dynamic dto, [FromQuery] int adminId)
        {
            string adminPin = dto?.AdminPin;
            if (string.IsNullOrEmpty(adminPin))
                return Forbid("Se requiere PIN del administrador para esta operación");

            var admin = _bancoService.ValidarLogin(adminId, adminPin);
            if (admin == null || admin.Rol != "Admin")
                return Forbid("Credenciales de administrador inválidas");

            var resultado = await _bancoService.MarcarSolicitudComoProcesada(solicitudId);

            if (!resultado)
                return NotFound(new { exitoso = false, mensaje = "Solicitud no encontrada" });

            return Ok(new { exitoso = true, mensaje = "Solicitud marcada como procesada" });
        }
    }
}
