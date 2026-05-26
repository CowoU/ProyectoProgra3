using ProyectoProgra3.Data;
using ProyectoProgra3.Models;
using Microsoft.EntityFrameworkCore;

namespace ProyectoProgra3.Services
{
    public class BancoService
    {
        private readonly ApplicationDbContext _context;

        public BancoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Usuario ValidarLogin(int id, string pin)
        {
            return _context.Usuarios.FirstOrDefault(u => u.Id == id && u.Pin == pin);
        }

        public Usuario ObtenerUsuarioPorId(int id)
        {
            return _context.Usuarios.Find(id);
        }

        public async Task<bool> Depositar(int usuarioId, decimal monto)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return false;

            usuario.SaldoBancario += monto;

            var transaccion = new Transaccion
            {
                UsuarioId = usuarioId,
                Concepto = "Depósito en ventanilla",
                MontoTotal = monto,
                ComisionBanco = 0,
                PagoEmpresa = 0,
                Fecha = DateTime.Now
            };

            _context.Transacciones.Add(transaccion);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool exitoso, string mensaje)> Retirar(int usuarioId, decimal monto)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return (false, "Usuario no encontrado");

            if (usuario.SaldoBancario < monto)
                return (false, "Saldo insuficiente");

            usuario.SaldoBancario -= monto;

            var transaccion = new Transaccion
            {
                UsuarioId = usuarioId,
                Concepto = "Retiro en ventanilla",
                MontoTotal = monto,
                ComisionBanco = 0,
                PagoEmpresa = 0,
                Fecha = DateTime.Now
            };

            _context.Transacciones.Add(transaccion);
            await _context.SaveChangesAsync();
            return (true, "Retiro exitoso");
        }

        public async Task<(bool exitoso, string mensaje, Transaccion transaccion)> PagarServicio(int usuarioId, decimal montoCobro, string conceptoServicio)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return (false, "Usuario no encontrado", null);

            if (usuario.SaldoBancario < montoCobro)
                return (false, "Saldo insuficiente", null);

            decimal comisionBanco = montoCobro * 0.05m;
            decimal pagoEmpresa = montoCobro * 0.95m;

            usuario.SaldoBancario -= montoCobro;

            var transaccion = new Transaccion
            {
                UsuarioId = usuarioId,
                Concepto = conceptoServicio,
                MontoTotal = montoCobro,
                ComisionBanco = comisionBanco,
                PagoEmpresa = pagoEmpresa,
                Fecha = DateTime.Now
            };

            _context.Transacciones.Add(transaccion);
            await _context.SaveChangesAsync();
            return (true, "Pago procesado exitosamente", transaccion);
        }

        public async Task<bool> CrearSolicitudRecuperacionPin(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return false;

            var solicitud = new SolicitudRecuperacionPin
            {
                UsuarioId = usuarioId,
                FechaSolicitud = DateTime.Now,
                Procesada = false
            };

            _context.SolicitudesRecuperacionPin.Add(solicitud);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CrearUsuario(string nombre, string pin, string rol, decimal saldoInicial)
        {
            var usuario = new Usuario
            {
                Nombre = nombre,
                Pin = pin,
                Rol = rol,
                SaldoBancario = saldoInicial
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Usuario>> ObtenerTodosLosUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }

        public async Task<bool> ActualizarPinUsuario(int usuarioId, string nuevoPin)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return false;

            usuario.Pin = nuevoPin;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarcarSolicitudComoProcesada(int solicitudId)
        {
            var solicitud = await _context.SolicitudesRecuperacionPin.FindAsync(solicitudId);
            if (solicitud == null)
                return false;

            solicitud.Procesada = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SolicitudRecuperacionPin>> ObtenerSolicitudesPendientes()
        {
            return await _context.SolicitudesRecuperacionPin
                .Where(s => !s.Procesada)
                .Include(s => s.Usuario)
                .ToListAsync();
        }
    }
}
