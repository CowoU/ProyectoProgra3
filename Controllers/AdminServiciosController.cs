using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoProgra3.Data;
using ProyectoProgra3.Services;
using System.Threading.Tasks;

namespace ProyectoProgra3.Controllers
{
    /// <summary>
    /// Controlador del Panel de Admin de Servicios
    /// Maneja la creación y visualización de cuotas para usuarios
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AdminServiciosController : ControllerBase
    {
        private readonly PagoService _pagoService;
        private readonly ApplicationDbContext _context;

        // Inyectamos tanto el servicio de pagos como el contexto de la BD
        public AdminServiciosController(PagoService pagoService, ApplicationDbContext context)
        {
            _pagoService = pagoService;
            _context = context;
        }

        /// <summary>
        /// Endpoint para OBTENER TODAS LAS CUOTAS DEL SISTEMA
        /// Devuelve la lista completa para la tabla del panel de administrador
        /// </summary>
        [HttpGet("todas-las-cuotas")]
        public async Task<IActionResult> ObtenerTodasLasCuotas()
        {
            try
            {
                var cuotas = await _context.Cuotas.ToListAsync();
                return Ok(cuotas);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    exitoso = false,
                    mensaje = "Error al obtener las cuotas desde la base de datos: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Endpoint para CREAR UNA NUEVA CUOTA
        /// El Admin del servicio crea una nueva cuota para un usuario
        /// </summary>
        /// <param name="request">Contiene idUsuario, idEmpresa, mes y monto</param>
        /// <returns>Resultado de la creación</returns>
        [HttpPost("crear-cuota")]
        public IActionResult CrearCuota([FromBody] CrearCuotaRequest request)
        {
            // Validar datos
            if (request == null || request.IdUsuario <= 0 || request.IdEmpresa <= 0 ||
                string.IsNullOrWhiteSpace(request.Mes) || request.Monto <= 0)
            {
                return BadRequest(new
                {
                    exitoso = false,
                    mensaje = "IdUsuario, IdEmpresa, Mes (YYYY-MM) y Monto son requeridos"
                });
            }

            // Crear la cuota
            var resultado = _pagoService.CrearCuota(
                request.IdUsuario,
                request.IdEmpresa,
                request.Mes,
                request.Monto
            );

            if (resultado.Exitoso)
            {
                return Ok(new
                {
                    exitoso = true,
                    mensaje = resultado.Mensaje,
                    detalles = new
                    {
                        idCuota = resultado.Cuota.Id,
                        usuario = resultado.UsuarioNombre,
                        empresa = resultado.EmpresaNombre,
                        mes = resultado.Cuota.Mes,
                        monto = resultado.Cuota.Monto,
                        estado = resultado.Cuota.Estado
                    }
                });
            }
            else
            {
                return BadRequest(new
                {
                    exitoso = false,
                    mensaje = resultado.Mensaje
                });
            }
        }

        /// <summary>
        /// Endpoint para BORRAR una cuota por su ID (uso admin)
        /// </summary>
        [HttpDelete("borrar-cuota/{idCuota}")]
        public IActionResult BorrarCuota(int idCuota)
        {
            if (idCuota <= 0)
            {
                return BadRequest(new { exitoso = false, mensaje = "IdCuota inválido" });
            }

            try
            {
                var borrado = _context.Cuotas.Find(idCuota);
                if (borrado == null)
                {
                    return NotFound(new { exitoso = false, mensaje = "La cuota no existe" });
                }

                _context.Cuotas.Remove(borrado);
                _context.SaveChanges();

                return Ok(new { exitoso = true, mensaje = "Cuota eliminada" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { exitoso = false, mensaje = "Error al eliminar cuota: " + ex.Message });
            }
        }
    }

    /// <summary>
    /// Modelo de solicitud para crear una cuota
    /// </summary>
    public class CrearCuotaRequest
    {
        public int IdUsuario { get; set; }
        public int IdEmpresa { get; set; }
        public string Mes { get; set; }
        public decimal Monto { get; set; }
    }
}