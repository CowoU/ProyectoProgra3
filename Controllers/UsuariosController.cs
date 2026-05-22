using Microsoft.AspNetCore.Mvc;
using ProyectoProgra3.Services;
using ProyectoProgra3.Models;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoProgra3.Controllers
{
    /// <summary>
    /// Controlador de Gestión de Usuarios
    /// Maneja operaciones de listado, filtrado y edición de usuarios
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly DataService _dataService;
        private readonly PagoService _pagoService;

        public UsuariosController(DataService dataService, PagoService pagoService)
        {
            _dataService = dataService;
            _pagoService = pagoService;
        }

        /// <summary>
        /// Obtiene todos los usuarios del sistema
        /// </summary>
        [HttpGet("todos")]
        public IActionResult ObtenerTodosLosUsuarios()
        {
            try
            {
                var usuarios = _dataService.ObtenerTodosLosUsuarios();
                return Ok(new
                {
                    exitoso = true,
                    usuarios = usuarios.Select(u => new
                    {
                        id = u.Id,
                        nombre = u.Nombre,
                        rol = u.Rol,
                        saldoBancario = u.SaldoBancario,
                        pinOculto = "****"
                    })
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { exitoso = false, mensaje = "Error al obtener usuarios: " + ex.Message });
            }
        }

        /// <summary>
        /// Filtra usuarios por rol (Admin, Cajero, Cliente)
        /// </summary>
        [HttpGet("filtrar-por-rol/{rol}")]
        public IActionResult FiltrarPorRol(string rol)
        {
            try
            {
                var usuarios = _dataService.ObtenerTodosLosUsuarios()
                    .Where(u => u.Rol.ToLower() == rol.ToLower())
                    .ToList();

                return Ok(new
                {
                    exitoso = true,
                    rol = rol,
                    cantidad = usuarios.Count,
                    usuarios = usuarios.Select(u => new
                    {
                        id = u.Id,
                        nombre = u.Nombre,
                        rol = u.Rol,
                        saldoBancario = u.SaldoBancario,
                        pinOculto = "****"
                    })
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { exitoso = false, mensaje = "Error al filtrar usuarios: " + ex.Message });
            }
        }

        /// <summary>
        /// Obtiene detalles completos de un usuario (incluye PIN)
        /// Requiere validación de PIN del admin
        /// </summary>
        [HttpPost("obtener-detalles")]
        public IActionResult ObtenerDetallesUsuario([FromBody] VerificarPinRequest request)
        {
            try
            {
                if (request == null || request.IdAdmin <= 0 || request.PinAdmin == null || request.IdUsuario <= 0)
                {
                    return BadRequest(new { exitoso = false, mensaje = "Datos incompletos" });
                }

                // Validar que el admin sea realmente admin
                var admin = _dataService.ObtenerUsuarioPorId(request.IdAdmin);
                if (admin == null || admin.Rol != "Admin")
                {
                    return BadRequest(new { exitoso = false, mensaje = "No eres administrador" });
                }

                // Validar PIN del admin
                var adminValido = _pagoService.AutenticarUsuario(request.IdAdmin, request.PinAdmin);
                if (adminValido == null)
                {
                    return BadRequest(new { exitoso = false, mensaje = "PIN del administrador incorrecto" });
                }

                // Obtener detalles completos del usuario
                var usuario = _dataService.ObtenerUsuarioPorId(request.IdUsuario);
                if (usuario == null)
                {
                    return BadRequest(new { exitoso = false, mensaje = "Usuario no encontrado" });
                }

                return Ok(new
                {
                    exitoso = true,
                    usuario = new
                    {
                        id = usuario.Id,
                        nombre = usuario.Nombre,
                        pin = usuario.Pin,
                        rol = usuario.Rol,
                        saldoBancario = usuario.SaldoBancario
                    }
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { exitoso = false, mensaje = "Error: " + ex.Message });
            }
        }

        /// <summary>
        /// Cambia el PIN de un usuario (requiere validación del PIN del admin)
        /// </summary>
        [HttpPost("cambiar-pin")]
        public IActionResult CambiarPinUsuario([FromBody] CambiarPinRequest request)
        {
            try
            {
                if (request == null || request.IdAdmin <= 0 || request.PinAdmin == null || 
                    request.IdUsuario <= 0 || string.IsNullOrWhiteSpace(request.NuevoPin))
                {
                    return BadRequest(new { exitoso = false, mensaje = "Datos incompletos" });
                }

                // Validar que el admin sea realmente admin
                var admin = _dataService.ObtenerUsuarioPorId(request.IdAdmin);
                if (admin == null || admin.Rol != "Admin")
                {
                    return BadRequest(new { exitoso = false, mensaje = "No eres administrador" });
                }

                // Validar PIN del admin
                var adminValido = _pagoService.AutenticarUsuario(request.IdAdmin, request.PinAdmin);
                if (adminValido == null)
                {
                    return BadRequest(new { exitoso = false, mensaje = "PIN del administrador incorrecto" });
                }

                // Validar que el usuario exista
                var usuario = _dataService.ObtenerUsuarioPorId(request.IdUsuario);
                if (usuario == null)
                {
                    return BadRequest(new { exitoso = false, mensaje = "Usuario no encontrado" });
                }

                // Cambiar el PIN
                _dataService.ActualizarPinUsuario(request.IdUsuario, request.NuevoPin);

                return Ok(new
                {
                    exitoso = true,
                    mensaje = $"PIN del usuario {usuario.Nombre} actualizado correctamente",
                    idUsuario = usuario.Id,
                    nombreUsuario = usuario.Nombre
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { exitoso = false, mensaje = "Error al cambiar PIN: " + ex.Message });
            }
        }

        /// <summary>
        /// Registra una solicitud de recuperación de PIN (usuario anónimo)
        /// </summary>
        [HttpPost("solicitar-recuperacion-pin")]
        public IActionResult SolicitarRecuperacionPin([FromBody] SolicitarRecuperacionPinRequest request)
        {
            try
            {
                if (request == null || request.IdUsuario <= 0)
                {
                    return BadRequest(new { exitoso = false, mensaje = "ID de usuario requerido" });
                }

                var usuario = _dataService.ObtenerUsuarioPorId(request.IdUsuario);
                if (usuario == null)
                {
                    return BadRequest(new { exitoso = false, mensaje = "Usuario no encontrado" });
                }

                // Registrar solicitud (en memoria o BD)
                _dataService.RegistrarSolicitudRecuperacionPin(request.IdUsuario, usuario.Nombre);

                return Ok(new
                {
                    exitoso = true,
                    mensaje = $"Solicitud de recuperación de PIN registrada. El administrador será notificado.",
                    idUsuario = usuario.Id,
                    nombreUsuario = usuario.Nombre
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { exitoso = false, mensaje = "Error: " + ex.Message });
            }
        }

        /// <summary>
        /// <summary>
        /// Obtiene las solicitudes pendientes de recuperación de PIN (para mostrar en panel del admin)
        /// No requiere autenticación adicional (el admin ya está en su panel)
        /// </summary>
        [HttpGet("solicitudes-pendientes")]
        public IActionResult ObtenerSolicitudesPendientes()
        {
            try
            {
                var solicitudes = _dataService.ObtenerSolicitudesRecuperacionPin();

                return Ok(new
                {
                    exitoso = true,
                    cantidad = solicitudes.Count,
                    solicitudes = solicitudes.Select(s => new
                    {
                        idUsuario = s.IdUsuario,
                        nombreUsuario = s.NombreUsuario,
                        fechaSolicitud = s.FechaSolicitud,
                        procesada = s.Procesada
                    }).ToList()
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { exitoso = false, mensaje = "Error: " + ex.Message });
            }
        }

        /// <summary>
        /// Marca una solicitud de recuperación de PIN como resuelta
        /// </summary>
        [HttpPost("marcar-solicitud-resuelta/{idUsuario}")]
        public IActionResult MarcarSolicitudResuelta(int idUsuario)
        {
            try
            {
                if (idUsuario <= 0)
                {
                    return BadRequest(new { exitoso = false, mensaje = "ID de usuario requerido" });
                }

                _dataService.MarcarSolicitudComoProcesada(idUsuario);

                return Ok(new
                {
                    exitoso = true,
                    mensaje = "Solicitud marcada como resuelta"
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { exitoso = false, mensaje = "Error: " + ex.Message });
            }
        }

        /// <summary>
        /// Obtiene las solicitudes pendientes de recuperación de PIN (solo para admin con verificación)
        /// </summary>
        [HttpPost("obtener-solicitudes-pin")]
        public IActionResult ObtenerSolicitudesPin([FromBody] VerificarPinAdminRequest request)
        {
            try
            {
                if (request == null || request.IdAdmin <= 0 || request.PinAdmin == null)
                {
                    return BadRequest(new { exitoso = false, mensaje = "Datos incompletos" });
                }

                // Validar PIN del admin
                var adminValido = _pagoService.AutenticarUsuario(request.IdAdmin, request.PinAdmin);
                if (adminValido == null)
                {
                    return BadRequest(new { exitoso = false, mensaje = "PIN del administrador incorrecto" });
                }

                // Obtener solicitudes
                var solicitudes = _dataService.ObtenerSolicitudesRecuperacionPin();

                return Ok(new
                {
                    exitoso = true,
                    cantidad = solicitudes.Count,
                    solicitudes = solicitudes
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { exitoso = false, mensaje = "Error: " + ex.Message });
            }
        }
    }

    // DTOs para requests
    public class VerificarPinRequest
    {
        public int IdAdmin { get; set; }
        public string PinAdmin { get; set; }
        public int IdUsuario { get; set; }
    }

    public class CambiarPinRequest
    {
        public int IdAdmin { get; set; }
        public string PinAdmin { get; set; }
        public int IdUsuario { get; set; }
        public string NuevoPin { get; set; }
    }

    public class SolicitarRecuperacionPinRequest
    {
        public int IdUsuario { get; set; }
    }

    public class VerificarPinAdminRequest
    {
        public int IdAdmin { get; set; }
        public string PinAdmin { get; set; }
    }
}
