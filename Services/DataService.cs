using ProyectoProgra3.Models;
using ProyectoProgra3.Data;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;

namespace ProyectoProgra3.Services
{
    public class DataService
    {
        private readonly ApplicationDbContext _context;

        // Variable temporal en memoria para las comisiones del banco (ya que usualmente no tienen tabla propia)
        private static decimal _comisionesTotalesBanco = 0;

        public DataService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // MÉTODOS DE LECTURA (GET)
        // ==========================================
        public Usuario ValidarLogin(int id, string pin) =>
            _context.Usuarios.FirstOrDefault(u => u.Id == id && u.Pin == pin);

        public Usuario ObtenerUsuarioPorId(int idUsuario) =>
            _context.Usuarios.Find(idUsuario);

        public Cuota ObtenerCuotaPorId(int idCuota) =>
            _context.Cuotas.Find(idCuota);

        public Empresa ObtenerEmpresaPorId(int idEmpresa) =>
            _context.Empresas.Find(idEmpresa);

        public List<Empresa> ObtenerTodasLasEmpresas() =>
            _context.Empresas.ToList();

        // Intentamos la consulta normal; si falla (por discrepancia de esquema), devolvemos una proyección segura
        public List<Cuota> ObtenerCuotasPendientesPorUsuario(int idUsuario)
        {
            try
            {
                return _context.Cuotas.Where(c => c.IdUsuario == idUsuario && c.Estado == "Pendiente").ToList();
            }
            catch (Exception)
            {
                try
                {
                    var rows = _context.Cuotas
                        .Where(c => c.IdUsuario == idUsuario)
                        .Select(c => new { c.Id, c.IdUsuario, c.IdEmpresa, c.Mes, c.Monto })
                        .ToList();

                    return rows.Select(r => new Cuota
                    {
                        Id = r.Id,
                        IdUsuario = r.IdUsuario,
                        IdEmpresa = r.IdEmpresa,
                        Mes = r.Mes,
                        Monto = r.Monto,
                        Estado = "Pendiente"
                    }).ToList();
                }
                catch (Exception)
                {
                    return new List<Cuota>();
                }
            }
        }

        public List<Cuota> ObtenerCuotasPorUsuario(int idUsuario)
        {
            try
            {
                return _context.Cuotas.Where(c => c.IdUsuario == idUsuario).ToList();
            }
            catch (Exception)
            {
                try
                {
                    var rows = _context.Cuotas
                        .Where(c => c.IdUsuario == idUsuario)
                        .Select(c => new { c.Id, c.IdUsuario, c.IdEmpresa, c.Mes, c.Monto })
                        .ToList();

                    return rows.Select(r => new Cuota
                    {
                        Id = r.Id,
                        IdUsuario = r.IdUsuario,
                        IdEmpresa = r.IdEmpresa,
                        Mes = r.Mes,
                        Monto = r.Monto,
                        Estado = "Pendiente"
                    }).ToList();
                }
                catch (Exception)
                {
                    return new List<Cuota>();
                }
            }
        }

        public decimal ObtenerComisionesBanco() => _comisionesTotalesBanco;

        // ==========================================
        // MÉTODOS DE ESCRITURA (UPDATE / INSERT)
        // ==========================================
        public void MarcarCuotaComoPagada(int idCuota)
        {
            var cuota = _context.Cuotas.Find(idCuota);
            if (cuota != null)
            {
                cuota.Estado = "Pagado";
                try
                {
                    _context.SaveChanges();
                }
                catch (Exception)
                {
                    // Si SaveChanges falla (p.ej. columna inexistente), revertimos los cambios en la entidad
                    _context.Entry(cuota).State = EntityState.Unchanged;
                }
            }
        }

        public void ActualizarSaldoUsuario(int idUsuario, decimal nuevoSaldo)
        {
            var usuario = _context.Usuarios.Find(idUsuario);
            if (usuario != null)
            {
                usuario.SaldoBancario = nuevoSaldo;
                _context.SaveChanges();
            }
        }

        public void ActualizarSaldoEmpresa(int idEmpresa, decimal nuevoSaldo)
        {
            var empresa = _context.Empresas.Find(idEmpresa);
            if (empresa != null)
            {
                empresa.SaldoAcumulado = nuevoSaldo;
                _context.SaveChanges();
            }
        }

        public void ActualizarComisionesBanco(decimal nuevasComisiones)
        {
            _comisionesTotalesBanco = nuevasComisiones;
        }

        public bool DepositarDinero(int idUsuario, decimal monto)
        {
            var usuario = _context.Usuarios.Find(idUsuario);
            if (usuario != null)
            {
                usuario.SaldoBancario += monto;
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        public bool RetirarDinero(int idUsuario, decimal monto)
        {
            var usuario = _context.Usuarios.Find(idUsuario);
            if (usuario != null && usuario.SaldoBancario >= monto)
            {
                usuario.SaldoBancario -= monto;
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        public Cuota CrearNuevaCuota(int idUsuario, int idEmpresa, string mes, decimal monto)
        {
            // Convertir el mes (formato "YYYY-MM") a una fecha válida
            // Establecer como el último día del mes especificado (fecha de vencimiento)
            DateTime fechaVencimiento;
            try
            {
                // Parsear el mes en formato "YYYY-MM"
                if (DateTime.TryParseExact(mes + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out var fechaParsed))
                {
                    // Obtener el último día del mes
                    fechaVencimiento = new DateTime(fechaParsed.Year, fechaParsed.Month, 
                        DateTime.DaysInMonth(fechaParsed.Year, fechaParsed.Month));
                }
                else
                {
                    // Si no se puede parsear, usar primer día del mes actual
                    fechaVencimiento = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
            }
            catch
            {
                // Fallback: usar fecha actual
                fechaVencimiento = DateTime.Now;
            }

            var nuevaCuota = new Cuota
            {
                IdUsuario = idUsuario,
                IdEmpresa = idEmpresa,
                Mes = mes,
                Monto = monto,
                Estado = "Pendiente",
                FechaVencimiento = fechaVencimiento,
                Mora = 0m
            };

            try
            {
                _context.Cuotas.Add(nuevaCuota);
                _context.SaveChanges();
                return nuevaCuota;
            }
            catch (Exception)
            {
                // Si falla por esquema, intentamos insertar sin la columna Estado (inserción mínima)
                try
                {
                    var sql = "INSERT INTO Cuotas (usuario_id, empresa_id, mes, monto, fecha_vencimiento, mora) VALUES (@p0, @p1, @p2, @p3, @p4, @p5);";
                    _context.Database.ExecuteSqlRaw(sql, idUsuario, idEmpresa, mes, monto, fechaVencimiento, 0m);

                    // Intentar recuperar la cuota insertada (última por usuario+mes)
                    var creada = _context.Cuotas.OrderByDescending(c => c.Id)
                        .FirstOrDefault(c => c.IdUsuario == idUsuario && c.IdEmpresa == idEmpresa && c.Mes == mes);
                    return creada;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        // ==========================================
        // MÉTODOS AÑADIDOS PARA CORREGIR ERRORES CS1061
        // ==========================================

        public int ObtenerMaxIdUsuario()
        {
            if (!_context.Usuarios.Any())
                return 0;

            return _context.Usuarios.Max(u => u.Id);
        }

        public Usuario CrearNuevoUsuario(int id, string nombre, string pin, string rol, decimal saldoInicial)
        {
            // Armamos el objeto Usuario con los 5 datos recibidos
            var nuevoUsuario = new Usuario
            {
                Id = id,
                Nombre = nombre,
                Pin = pin,
                Rol = rol,
                SaldoBancario = saldoInicial
            };

            // Lo guardamos en la base de datos
            _context.Usuarios.Add(nuevoUsuario);
            _context.SaveChanges();

            return nuevoUsuario;
        }

        public void MarcarCuotaComoPagadaConMora(int idCuota, decimal mora)
        {
            var cuota = _context.Cuotas.Find(idCuota);
            if (cuota != null)
            {
                cuota.Estado = "Pagado";
                try
                {
                    cuota.Mora = mora;
                    _context.SaveChanges();
                }
                catch (Exception)
                {
                    _context.Entry(cuota).State = EntityState.Unchanged;
                }
            }
        }

        public bool BorrarCuota(int idCuota)
        {
            var cuota = _context.Cuotas.Find(idCuota);
            if (cuota != null)
            {
                _context.Cuotas.Remove(cuota);
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        // ==========================================
        // MÉTODOS PARA GESTIÓN DE USUARIOS
        // ==========================================

        /// <summary>
        /// Obtiene todos los usuarios del sistema
        /// </summary>
        public List<Usuario> ObtenerTodosLosUsuarios()
        {
            try
            {
                return _context.Usuarios.ToList();
            }
            catch (Exception)
            {
                return new List<Usuario>();
            }
        }

        /// <summary>
        /// Actualiza el PIN de un usuario
        /// </summary>
        public void ActualizarPinUsuario(int idUsuario, string nuevoPin)
        {
            var usuario = _context.Usuarios.Find(idUsuario);
            if (usuario != null)
            {
                usuario.Pin = nuevoPin;
                _context.SaveChanges();
            }
        }

        // Variable estática para almacenar solicitudes de recuperación de PIN en memoria
        private static List<SolicitudRecuperacionPin> _solicitudesRecuperacionPin = new List<SolicitudRecuperacionPin>();

        /// <summary>
        /// Registra una solicitud de recuperación de PIN
        /// </summary>
        public void RegistrarSolicitudRecuperacionPin(int idUsuario, string nombreUsuario)
        {
            try
            {
                var solicitud = new SolicitudRecuperacionPin
                {
                    IdUsuario = idUsuario,
                    NombreUsuario = nombreUsuario,
                    FechaSolicitud = DateTime.Now,
                    Procesada = false
                };
                _context.SolicitudesRecuperacionPin.Add(solicitud);
                _context.SaveChanges();
            }
            catch (Exception)
            {
                // Fallback a memoria si hay error con BD
                var solicitud = new SolicitudRecuperacionPin
                {
                    IdUsuario = idUsuario,
                    NombreUsuario = nombreUsuario,
                    FechaSolicitud = DateTime.Now,
                    Procesada = false
                };
                _solicitudesRecuperacionPin.Add(solicitud);
            }
        }

        /// <summary>
        /// Obtiene todas las solicitudes pendientes de recuperación de PIN
        /// </summary>
        public List<SolicitudRecuperacionPin> ObtenerSolicitudesRecuperacionPin()
        {
            try
            {
                return _context.SolicitudesRecuperacionPin.Where(s => !s.Procesada).ToList();
            }
            catch (Exception)
            {
                // Fallback a memoria si hay error con BD
                return _solicitudesRecuperacionPin.Where(s => !s.Procesada).ToList();
            }
        }

        /// <summary>
        /// Marca una solicitud como procesada
        /// </summary>
        public void MarcarSolicitudComoProcesada(int idUsuario)
        {
            try
            {
                var solicitud = _context.SolicitudesRecuperacionPin.FirstOrDefault(s => s.IdUsuario == idUsuario && !s.Procesada);
                if (solicitud != null)
                {
                    solicitud.Procesada = true;
                    _context.SaveChanges();
                }
            }
            catch (Exception)
            {
                // Fallback a memoria si hay error con BD
                var solicitud = _solicitudesRecuperacionPin.FirstOrDefault(s => s.IdUsuario == idUsuario && !s.Procesada);
                if (solicitud != null)
                {
                    solicitud.Procesada = true;
                }
            }
        }
    }

}