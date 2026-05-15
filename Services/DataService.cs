using ProyectoProgra3.Models;
using ProyectoProgra3.Data;
using System.Collections.Generic;
using System.Linq;
using System;

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

        public List<Cuota> ObtenerCuotasPendientesPorUsuario(int idUsuario) =>
            _context.Cuotas.Where(c => c.IdUsuario == idUsuario && c.Estado == "Pendiente").ToList();

        public List<Cuota> ObtenerCuotasPorUsuario(int idUsuario) =>
            _context.Cuotas.Where(c => c.IdUsuario == idUsuario).ToList();

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
                _context.SaveChanges();
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
            var nuevaCuota = new Cuota
            {
                IdUsuario = idUsuario,
                IdEmpresa = idEmpresa,
                Mes = mes,
                Monto = monto,
                Estado = "Pendiente"
            };

            _context.Cuotas.Add(nuevaCuota);
            _context.SaveChanges();
            return nuevaCuota;
        }
    }
}