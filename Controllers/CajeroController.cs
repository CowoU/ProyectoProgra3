using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.Services;

namespace ProyectoProgra3.Controllers
{
    /// <summary>
    /// Controlador del Panel de Cajero (Banco)
    /// Maneja operaciones de depósito y búsqueda de usuarios
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CajeroController : ControllerBase
    {
        private readonly PagoService _pagoService;

        public CajeroController(PagoService pagoService)
        {
            _pagoService = pagoService;
        }

        /// <summary>
        /// Endpoint para BUSCAR UN USUARIO
        /// Usado por los Cajeros para localizar a un usuario y depositar dinero
        /// ✅ VALIDACIÓN: Solo permite buscar usuarios con rol "Cliente"
        /// </summary>
        /// <param name="idUsuario">ID del usuario a buscar</param>
        /// <returns>Información del usuario y su saldo</returns>
        [HttpGet("buscar-usuario/{idUsuario}")]
        public IActionResult BuscarUsuario(int idUsuario)
        {
            if (idUsuario <= 0)
            {
                return BadRequest(new { 
                    exitoso = false, 
                    mensaje = "IdUsuario inválido" 
                });
            }

            var usuario = _pagoService.BuscarUsuarioParaDeposito(idUsuario);

            if (usuario == null)
            {
                return NotFound(new { 
                    exitoso = false, 
                    mensaje = "Usuario no encontrado" 
                });
            }

            // ✅ VALIDAR QUE SOLO SEA CLIENTE
            if (usuario.Rol.ToUpper() != "CLIENTE")
            {
                return BadRequest(new
                {
                    exitoso = false,
                    mensaje = "Solo se pueden realizar operaciones con usuarios de rol Cliente"
                });
            }

            return Ok(new
            {
                exitoso = true,
                usuario = new
                {
                    id = usuario.Id,
                    nombre = usuario.Nombre,
                    rol = usuario.Rol,
                    saldoBancario = usuario.SaldoBancario
                },
                mensaje = "Usuario encontrado"
            });
        }

        /// <summary>
        /// Endpoint para REALIZAR UN DEPÓSITO
        /// El Cajero deposita dinero a la cuenta de un usuario
        /// </summary>
        /// <param name="request">Contiene idUsuario y monto</param>
        /// <returns>Confirmación del depósito</returns>
        [HttpPost("depositar")]
        public IActionResult Depositar([FromBody] DepositoRequest request)
        {
            // Validar datos
            if (request == null || request.IdUsuario <= 0 || request.Monto <= 0)
            {
                return BadRequest(new { 
                    exitoso = false, 
                    mensaje = "IdUsuario y Monto válidos son requeridos" 
                });
            }

            // Realizar el depósito
            bool depositoExitoso = _pagoService.RealizarDeposito(request.IdUsuario, request.Monto);

            if (depositoExitoso)
            {
                var usuarioActualizado = _pagoService.BuscarUsuarioParaDeposito(request.IdUsuario);

                return Ok(new
                {
                    exitoso = true,
                    mensaje = "Depósito realizado exitosamente",
                    detalles = new
                    {
                        idUsuario = request.IdUsuario,
                        montoDepositado = request.Monto,
                        nuevoSaldo = usuarioActualizado?.SaldoBancario ?? 0
                    }
                });
            }
            else
            {
                return BadRequest(new
                {
                    exitoso = false,
                    mensaje = "No se pudo realizar el depósito. Verifique el usuario y monto."
                });
            }
        }

        /// <summary>
        /// Endpoint para RETIRAR DINERO
        /// El Cajero retira dinero de la cuenta de un usuario cliente
        /// </summary>
        [HttpPost("retirar")]
        public IActionResult Retirar([FromBody] RetiroRequest request)
        {
            if (request == null || request.IdUsuario <= 0 || request.Monto <= 0)
            {
                return BadRequest(new { 
                    exitoso = false, 
                    mensaje = "IdUsuario y Monto válidos son requeridos" 
                });
            }

            // Verificar que sea cliente
            var usuario = _pagoService.BuscarUsuarioParaDeposito(request.IdUsuario);
            if (usuario == null || usuario.Rol.ToUpper() != "CLIENTE")
            {
                return BadRequest(new
                {
                    exitoso = false,
                    mensaje = "Solo se pueden realizar retiros de usuarios con rol Cliente"
                });
            }

            if (usuario.SaldoBancario < request.Monto)
            {
                return BadRequest(new
                {
                    exitoso = false,
                    mensaje = "Saldo insuficiente"
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
    }

    /// <summary>
    /// Modelo de solicitud para realizar un depósito
    /// </summary>
    public class DepositoRequest
    {
        public int IdUsuario { get; set; }
        public decimal Monto { get; set; }
    }

    /// <summary>
    /// Modelo de solicitud para realizar un retiro
    /// </summary>
    public class RetiroRequest
    {
        public int IdUsuario { get; set; }
        public decimal Monto { get; set; }
    }
}
