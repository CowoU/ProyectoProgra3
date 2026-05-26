using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoProgra3.Data;

namespace ProyectoProgra3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransaccionesController
        : ControllerBase
    {
        private readonly
        ApplicationDbContext
        _context;

        public TransaccionesController(
            ApplicationDbContext context
        )
        {
            _context =
                context;
        }

        [HttpGet]
        public async Task<IActionResult>
        Obtener()
        {
            var datos =
                await _context
                .Transacciones
                .Select(t => new
                {
                    t.Id,

                    t.UsuarioId,

                    t.Concepto,

                    t.MontoTotal,

                    t.ComisionBanco,

                    t.PagoEmpresa,

                    t.Fecha
                })
                .ToListAsync();

            return Ok(
                datos
            );
        }
    }
}