// Controllers/CajeroController.cs
using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.DTOs;
using ProyectoProgra3.Services;

namespace ProyectoProgra3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CajeroController : ControllerBase
    {
        private readonly BancoService _bancoService;

        public CajeroController(BancoService bancoService)
        {
            _bancoService = bancoService;
        }

        [HttpGet("buscar-usuario/{id}")]
        public IActionResult BuscarUsuario(int id)
        {
            var usuario = _bancoService.ObtenerUsuarioPorId(id);

            if (usuario == null || usuario.Rol != "Cliente")
                return NotFound(new { exitoso = false, mensaje = "Usuario no encontrado o no es cliente" });

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

        [HttpPost("deposito")]
        public async Task<IActionResult> Deposito([FromBody] DepositoDto dto)
        {
            if (dto.Monto <= 0)
                return BadRequest(new { exitoso = false, mensaje = "Monto debe ser mayor a 0" });

            var resultado = await _bancoService.Depositar(dto.UsuarioId, dto.Monto);

            if (!resultado)
                return NotFound(new { exitoso = false, mensaje = "Usuario no encontrado" });

            var usuario = _bancoService.ObtenerUsuarioPorId(dto.UsuarioId);
            return Ok(new
            {
                exitoso = true,
                mensaje = $"Depósito de Q{dto.Monto.ToString("F2")} realizado exitosamente",
                nuevoSaldo = usuario.SaldoBancario
            });
        }

        [HttpPost("retiro")]
        public async Task<IActionResult> Retiro([FromBody] RetiroDto dto)
        {
            if (dto.Monto <= 0)
                return BadRequest(new { exitoso = false, mensaje = "Monto debe ser mayor a 0" });

            var (exitoso, mensaje) = await _bancoService.Retirar(dto.UsuarioId, dto.Monto);

            if (!exitoso)
                return BadRequest(new { exitoso = false, mensaje });

            var usuario = _bancoService.ObtenerUsuarioPorId(dto.UsuarioId);
            return Ok(new
            {
                exitoso = true,
                mensaje = $"Retiro de Q{dto.Monto.ToString("F2")} realizado exitosamente",
                nuevoSaldo = usuario.SaldoBancario
            });
        }
    }
}
