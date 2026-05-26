// Controllers/ClienteController.cs
using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.DTOs;
using ProyectoProgra3.Services;

namespace ProyectoProgra3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteController : ControllerBase
    {
        private readonly BancoService _bancoService;

        public ClienteController(BancoService bancoService)
        {
            _bancoService = bancoService;
        }

        [HttpPost("retiro")]
        public async Task<IActionResult> Retiro([FromBody] RetiroDto dto)
        {
            var (exitoso, mensaje) = await _bancoService.Retirar(dto.UsuarioId, dto.Monto);

            if (!exitoso)
                return BadRequest(new { exitoso = false, mensaje });

            return Ok(new { exitoso = true, mensaje });
        }

        [HttpPost("pagar-servicio")]
        public async Task<IActionResult> PagarServicio([FromBody] PagarServicioDto dto)
        {
            var (exitoso, mensaje, transaccion) = await _bancoService.PagarServicio(
                dto.UsuarioId,
                dto.Monto,
                dto.Concepto
            );

            if (!exitoso)
                return BadRequest(new { exitoso = false, mensaje });

            return Ok(new
            {
                exitoso = true,
                mensaje = "Pago procesado exitosamente",
                transaccion = new
                {
                    id = transaccion.Id,
                    usuarioId = transaccion.UsuarioId,
                    concepto = transaccion.Concepto,
                    montoTotal = transaccion.MontoTotal,
                    comisionBanco = transaccion.ComisionBanco,
                    pagoEmpresa = transaccion.PagoEmpresa,
                    fecha = transaccion.Fecha
                }
            });
        }
    }
}
