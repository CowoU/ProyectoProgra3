// Controllers/AdminController.cs
using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.DTOs;
using ProyectoProgra3.Services;

namespace ProyectoProgra3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly BancoService _bancoService;

        public AdminController(BancoService bancoService)
        {
            _bancoService = bancoService;
        }

        [HttpGet("usuarios")]
        public async Task<IActionResult> ObtenerTodosLosUsuarios()
        {
            var usuarios = await _bancoService.ObtenerTodosLosUsuarios();
            return Ok(new
            {
                exitoso = true,
                usuarios = usuarios.Select(u => new
                {
                    id = u.Id,
                    nombre = u.Nombre,
                    rol = u.Rol,
                    saldoBancario = u.SaldoBancario
                }).ToList()
            });
        }

        [HttpPost("crear-usuario")]
        public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioDto dto)
        {
            var resultado = await _bancoService.CrearUsuario(dto.Nombre, dto.Pin, dto.Rol, dto.SaldoBancario);

            if (!resultado)
                return BadRequest(new { exitoso = false, mensaje = "Error al crear usuario" });

            return Ok(new { exitoso = true, mensaje = "Usuario creado exitosamente" });
        }

        [HttpPost("restablecer-pin/{usuarioId}")]
        public async Task<IActionResult> RestablecerPin(int usuarioId, [FromBody] RestablecerPinDto dto)
        {
            var usuario = _bancoService.ObtenerUsuarioPorId(usuarioId);
            if (usuario == null)
                return NotFound(new { exitoso = false, mensaje = "Usuario no encontrado" });

            var resultado = await _bancoService.ActualizarPinUsuario(usuarioId, dto.NuevoPin);

            if (!resultado)
                return BadRequest(new { exitoso = false, mensaje = "Error al actualizar PIN" });

            return Ok(new { exitoso = true, mensaje = "PIN actualizado exitosamente" });
        }

        [HttpGet("solicitudes-pendientes")]
        public async Task<IActionResult> ObtenerSolicitudesPendientes()
        {
            var solicitudes = await _bancoService.ObtenerSolicitudesPendientes();
            return Ok(new
            {
                exitoso = true,
                cantidad = solicitudes.Count,
                solicitudes = solicitudes.Select(s => new
                {
                    id = s.Id,
                    usuarioId = s.UsuarioId,
                    usuario = s.Usuario?.Nombre,
                    fechaSolicitud = s.FechaSolicitud,
                    procesada = s.Procesada
                }).ToList()
            });
        }

        [HttpPost("procesar-solicitud/{solicitudId}")]
        public async Task<IActionResult> ProcesarSolicitud(int solicitudId)
        {
            var resultado = await _bancoService.MarcarSolicitudComoProcesada(solicitudId);

            if (!resultado)
                return NotFound(new { exitoso = false, mensaje = "Solicitud no encontrada" });

            return Ok(new { exitoso = true, mensaje = "Solicitud procesada" });
        }
    }
}
