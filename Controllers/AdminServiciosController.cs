using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.Services;

namespace ProyectoProgra3.Controllers
{
    /// <summary>
    /// Controlador del Panel de Admin de Servicios
    /// Maneja la creación de nuevas cuotas para usuarios
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AdminServiciosController : ControllerBase
    {
        private readonly PagoService _pagoService;

        public AdminServiciosController(PagoService pagoService)
        {
            _pagoService = pagoService;
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
                return BadRequest(new { 
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
