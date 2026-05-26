using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.Services;
using ProyectoProgra3.DTOs;
using ProyectoProgra3.Data;
using ProyectoProgra3.Models;

namespace ProyectoProgra3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiciosExternosController
        : ControllerBase
    {
        private readonly CementerioService _cementerio;

        private readonly ApplicationDbContext _context;

        public ServiciosExternosController(
            CementerioService cementerio,
            ApplicationDbContext context
        )
        {
            _cementerio =
                cementerio;

            _context =
                context;
        }

        // CONSULTAR CUOTAS
        [HttpGet(
            "cementerio/{clienteId}"
        )]
        public async Task<IActionResult>
        Obtener(
            int clienteId
        )
        {
            var cuotas =
                await _cementerio
                .ObtenerCuotas(
                    clienteId
                );

            if (cuotas == null)
            {
                return NotFound(
                    new
                    {
                        mensaje =
                            "Sin cuotas"
                    }
                );
            }

            return Ok(
                cuotas
            );
        }

        // PAGAR CUOTA
        [HttpPost(
            "cementerio/pagar"
        )]
        public async Task<IActionResult>
        Pagar(
            PagarServicioDto dto
        )
        {
            var usuario =
                await _context
                .Usuarios
                .FindAsync(
                    dto.UsuarioId
                );

            if (usuario == null)
            {
                return NotFound(
                    new
                    {
                        mensaje =
                            "Usuario no encontrado"
                    }
                );
            }

            if (
                usuario
                .SaldoBancario
                <
                dto.Monto
            )
            {
                return BadRequest(
                    new
                    {
                        mensaje =
                            "Saldo insuficiente"
                    }
                );
            }

            usuario
                .SaldoBancario
                -=
                dto.Monto;

            var transaccion =
                new Transaccion
                {
                    UsuarioId =
                        usuario.Id,

                    Concepto =
                        dto.Concepto,

                    MontoTotal =
                        dto.Monto,

                    ComisionBanco =
                        dto.Monto
                        *
                        0.05m,

                    PagoEmpresa =
                        dto.Monto
                        *
                        0.95m,

                    Fecha =
                        DateTime.Now
                };

            _context
                .Transacciones
                .Add(
                    transaccion
                );

            await _context
                .SaveChangesAsync();

            var actualizado =
                await _cementerio
                .MarcarPagada(
                    dto.CuotaId
                );

            if (!actualizado)
            {
                return BadRequest(
                    new
                    {
                        mensaje =
                            "Cobro realizado pero no se actualizó Cementerio"
                    }
                );
            }

            return Ok(
                new
                {
                    exitoso =
                        true,

                    saldoActual =
                        usuario
                        .SaldoBancario
                }
            );
        }
    }
}