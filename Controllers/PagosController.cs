using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.Services;

namespace ProyectoProgra3.Controllers
{
    /// <summary>
    /// Controlador de Pagos
    /// Maneja todas las operaciones relacionadas con el pago de cuotas
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PagosController : ControllerBase
    {
        private readonly PagoService _pagoService;

        public PagosController(PagoService pagoService)
        {
            _pagoService = pagoService;
        }

        /// <summary>
        /// Endpoint para PAGAR UNA CUOTA
        /// Implementa la lógica de negocio: descuento, comisiones, actualización de estados
        /// </summary>
        /// <param name="request">Contiene idUsuario e idCuota</param>
        /// <returns>Resultado del pago (exitoso o error)</returns>
        [HttpPost("pagar-cuota")]
        public IActionResult PagarCuota([FromBody] PagarCuotaRequest request)
        {
            // Validar datos
            if (request == null || request.IdUsuario <= 0 || request.IdCuota <= 0)
            {
                return BadRequest(new { 
                    exitoso = false, 
                    mensaje = "IdUsuario e IdCuota son requeridos y deben ser mayores a 0" 
                });
            }

            // Procesar el pago
            var resultado = _pagoService.ProcesarPagoCuota(request.IdUsuario, request.IdCuota);

            // Retornar resultado
            if (resultado.Exitoso)
            {
                return Ok(new
                {
                    exitoso = true,
                    mensaje = resultado.Mensaje,
                    detalles = new
                    {
                        montoOriginal = resultado.MontoOriginal,
                        montoParaEmpresa = resultado.MontoParaEmpresa,
                        comisionBanco = resultado.ComisionBanco,
                        saldoRestante = resultado.SaldoRestante,
                        empresa = resultado.Empresa,
                        fechaPago = resultado.FechaPago
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
        /// Endpoint para PAGAR UNA CUOTA Y LIQUIDAR ANTERIORES VENCIDAS (aplicando mora)
        /// </summary>
        [HttpPost("pagar-cuota-con-mora")]
        public IActionResult PagarCuotaConMora([FromBody] PagarCuotaRequest request)
        {
            if (request == null || request.IdUsuario <= 0 || request.IdCuota <= 0)
            {
                return BadRequest(new { exitoso = false, mensaje = "IdUsuario e IdCuota son requeridos" });
            }

            var resultado = _pagoService.ProcesarPagoCuotaConMora(request.IdUsuario, request.IdCuota);

            if (resultado.Exitoso)
            {
                return Ok(new
                {
                    exitoso = true,
                    mensaje = resultado.Mensaje,
                    detalles = new
                    {
                        montoOriginal = resultado.MontoOriginal,
                        montoParaEmpresa = resultado.MontoParaEmpresa,
                        comisionBanco = resultado.ComisionBanco,
                        saldoRestante = resultado.SaldoRestante,
                        empresa = resultado.Empresa,
                        fechaPago = resultado.FechaPago
                    }
                });
            }

            return BadRequest(new { exitoso = false, mensaje = resultado.Mensaje });
        }

        /// <summary>
        /// Endpoint para CALCULAR LA MORA DE UNA CUOTA
        /// Retorna el monto de mora acumulada sin realizar el pago
        /// </summary>
        [HttpGet("calcular-mora/{idCuota}")]
        public IActionResult CalcularMora(int idCuota)
        {
            var cuota = _pagoService.ObtenerCuota(idCuota);
            if (cuota == null)
            {
                return BadRequest(new { exitoso = false, mensaje = "Cuota no encontrada" });
            }

            var mora = _pagoService.CalcularMora(cuota, DateTime.Now);
            var total = cuota.Monto + mora;

            return Ok(new
            {
                exitoso = true,
                idCuota = cuota.Id,
                montoOriginal = cuota.Monto,
                mora = mora,
                total = total,
                estado = cuota.Estado,
                fechaVencimiento = cuota.FechaVencimiento?.ToString("yyyy-MM-dd")
            });
        }

        /// <summary>
        /// Endpoint para OBTENER CUOTAS PENDIENTES DE UN USUARIO
        /// </summary>
        /// <param name="idUsuario">ID del usuario</param>
        /// <returns>Lista de cuotas pendientes</returns>
        [HttpGet("cuotas-pendientes/{idUsuario}")]
        public IActionResult ObtenerCuotasPendientes(int idUsuario)
        {
            if (idUsuario <= 0)
            {
                return BadRequest(new { 
                    exitoso = false, 
                    mensaje = "IdUsuario inválido" 
                });
            }

            // Obtener cuotas pendientes
            var cuotas = _pagoService.ObtenerCuotasPendientes(idUsuario);

            if (cuotas == null || cuotas.Count == 0)
            {
                return Ok(new
                {
                    exitoso = true,
                    mensaje = "El usuario no tiene cuotas pendientes",
                    cuotasPendientes = new List<object>()
                });
            }

            return Ok(new
            {
                exitoso = true,
                mensaje = "Cuotas pendientes obtenidas",
                cuotasPendientes = cuotas.Select(c => new
                {
                    id = c.Id,
                    idEmpresa = c.IdEmpresa,
                    mes = c.Mes,
                    monto = c.Monto,
                    estado = c.Estado
                }).ToList()
            });
        }

        /// <summary>
        /// Endpoint para OBTENER HISTORIAL DE CUOTAS (pagadas y pendientes)
        /// </summary>
        /// <param name="idUsuario">ID del usuario</param>
        /// <returns>Lista completa de cuotas</returns>
        [HttpGet("historial-cuotas/{idUsuario}")]
        public IActionResult ObtenerHistorialCuotas(int idUsuario)
        {
            if (idUsuario <= 0)
            {
                return BadRequest(new { 
                    exitoso = false, 
                    mensaje = "IdUsuario inválido" 
                });
            }

            var cuotas = _pagoService.ObtenerHistorialCuotas(idUsuario);

            if (cuotas == null || cuotas.Count == 0)
            {
                return Ok(new
                {
                    exitoso = true,
                    mensaje = "El usuario no tiene cuotas registradas",
                    cuotas = new List<object>()
                });
            }

            return Ok(new
            {
                exitoso = true,
                mensaje = "Historial de cuotas obtenido",
                cuotas = cuotas.Select(c => new
                {
                    id = c.Id,
                    idEmpresa = c.IdEmpresa,
                    mes = c.Mes,
                    monto = c.Monto,
                    estado = c.Estado
                }).ToList()
            });
        }

        /// <summary>
        /// Endpoint para RETIRAR DINERO (Cliente)
        /// </summary>
        [HttpPost("retirar")]
        public IActionResult Retirar([FromBody] RetiroClienteRequest request)
        {
            if (request == null || request.IdUsuario <= 0 || request.Monto <= 0)
            {
                return BadRequest(new { 
                    exitoso = false, 
                    mensaje = "IdUsuario y Monto válidos son requeridos" 
                });
            }

            var usuario = _pagoService.BuscarUsuarioParaDeposito(request.IdUsuario);
            if (usuario == null)
            {
                return NotFound(new { 
                    exitoso = false, 
                    mensaje = "Usuario no encontrado" 
                });
            }

            if (usuario.SaldoBancario < request.Monto)
            {
                return BadRequest(new
                {
                    exitoso = false,
                    mensaje = "Saldo insuficiente para realizar el retiro"
                });
            }

            bool retiroExitoso = _pagoService.RetirarDinero(request.IdUsuario, request.Monto);

            if (retiroExitoso)
            {
                var usuarioActualizado = _pagoService.BuscarUsuarioParaDeposito(request.IdUsuario);
                return Ok(new
                {
                    exitoso = true,
                    mensaje = "Retiro realizado exitosamente",
                    detalles = new
                    {
                        idUsuario = request.IdUsuario,
                        montoRetirado = request.Monto,
                        nuevoSaldo = usuarioActualizado?.SaldoBancario ?? 0
                    }
                });
            }

            return BadRequest(new
            {
                exitoso = false,
                mensaje = "No se pudo realizar el retiro"
            });
        }

        /// <summary>
        /// Endpoint para OBTENER EMPRESAS
        /// </summary>
        [HttpGet("empresas")]
        public IActionResult ObtenerEmpresas()
        {
            var empresas = _pagoService.ObtenerEmpresas();
            
            return Ok(empresas.Select(e => new
            {
                id = e.Id,
                nombre = e.Nombre,
                saldoAcumulado = e.SaldoAcumulado
            }).ToList());
        }
    }

    /// <summary>
    /// Modelo de solicitud para pagar una cuota
    /// </summary>
    public class PagarCuotaRequest
    {
        public int IdUsuario { get; set; }
        public int IdCuota { get; set; }
    }

    /// <summary>
    /// Modelo de solicitud para retirar dinero (Cliente)
    /// </summary>
    public class RetiroClienteRequest
    {
        public int IdUsuario { get; set; }
        public decimal Monto { get; set; }
    }
}

