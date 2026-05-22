using ProyectoProgra3.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoProgra3.Services
{
    /// <summary>
    /// Servicio que contiene toda la lógica de negocio del portal de pagos
    /// Implementa las reglas de negocio: validaciones, cálculos, actualizaciones
    /// </summary>
    public class PagoService
    {
        // Inyección de dependencia: usamos DataService para acceder a los datos
        private readonly DataService _dataService;

        // Constantes de negocio
        private const decimal PORCENTAJE_EMPRESA = 0.95m; // 95% al servicio
        private const decimal PORCENTAJE_COMISION_BANCO = 0.05m; // 5% al banco

        // Nueva constante de mora por mes
        private const decimal MORA_POR_MES = 25m;

        public PagoService(DataService dataService)
        {
            _dataService = dataService;
        }

        // ============================================================================
        // MÉTODOS PÚBLICOS DE ACCESO A DATOS
        // ============================================================================

        /// <summary>
        /// Obtiene una cuota por su ID (acceso público para controladores)
        /// </summary>
        public Cuota ObtenerCuota(int idCuota)
        {
            return _dataService.ObtenerCuotaPorId(idCuota);
        }

        // ============================================================================
        // MÉTODOS DE VALIDACIÓN
        // ============================================================================

        /// <summary>
        /// Valida las credenciales de login de un usuario
        /// </summary>
        /// <param name="id">ID del usuario</param>
        /// <param name="pin">PIN del usuario</param>
        /// <returns>Usuario si credenciales son válidas, null en caso contrario</returns>
        public Usuario AutenticarUsuario(int id, string pin)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(pin))
                return null;

            var usuario = _dataService.ValidarLogin(id, pin);
            return usuario;
        }

        /// <summary>
        /// Verifica si un usuario tiene saldo suficiente para pagar una cuota
        /// </summary>
        /// <param name="idUsuario">ID del usuario</param>
        /// <param name="monto">Monto a verificar</param>
        /// <returns>true si tiene saldo suficiente, false en caso contrario</returns>
        public bool TieneSaldoSuficiente(int idUsuario, decimal monto)
        {
            var usuario = _dataService.ObtenerUsuarioPorId(idUsuario);
            return usuario != null && usuario.SaldoBancario >= monto;
        }

        /// <summary>
        /// Verifica que una cuota sea válida y esté pendiente
        /// </summary>
        /// <param name="idCuota">ID de la cuota</param>
        /// <returns>Cuota si es válida, null en caso contrario</returns>
        public Cuota ValidarCuota(int idCuota)
        {
            var cuota = _dataService.ObtenerCuotaPorId(idCuota);
            if (cuota == null || cuota.Estado != "Pendiente")
                return null;

            return cuota;
        }

        // ============================================================================
        // MÉTODOS DE PAGO - LÓGICA PRINCIPAL
        // ============================================================================

        /// <summary>
        /// Procesa el pago de una cuota por parte de un usuario
        /// 
        /// REGLAS DE NEGOCIO:
        /// 1. Validar que el usuario tenga saldo suficiente
        /// 2. Restar el 100% de la cuota de la cuenta del usuario
        /// 3. Sumar el 95% del monto a la empresa
        /// 4. Sumar el 5% a las comisiones del banco
        /// 5. Cambiar estado de la cuota a "pagado"
        /// </summary>
        /// <param name="idUsuario">ID del usuario que paga</param>
        /// <param name="idCuota">ID de la cuota a pagar</param>
        /// <returns>Objeto con información del resultado del pago</returns>
        public ResultadoPago ProcesarPagoCuota(int idUsuario, int idCuota)
        {
            var resultado = new ResultadoPago();

            try
            {
                // PASO 1: Validar que la cuota exista y esté pendiente
                var cuota = ValidarCuota(idCuota);
                if (cuota == null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "La cuota no existe o ya ha sido pagada";
                    return resultado;
                }

                // PASO 2: Validar que la cuota pertenezca al usuario
                if (cuota.IdUsuario != idUsuario)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "No tiene permiso para pagar esta cuota";
                    return resultado;
                }

                // PASO 3: Obtener el usuario
                var usuario = _dataService.ObtenerUsuarioPorId(idUsuario);
                if (usuario == null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "El usuario no existe en el sistema";
                    return resultado;
                }

                // PASO 4: Validar saldo suficiente
                if (!TieneSaldoSuficiente(idUsuario, cuota.Monto))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Saldo insuficiente. Su saldo actual es Q" + usuario.SaldoBancario.ToString("F2");
                    return resultado;
                }

                // PASO 5: Obtener la empresa que recibirá el pago
                var empresa = _dataService.ObtenerEmpresaPorId(cuota.IdEmpresa);
                if (empresa == null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "La empresa no existe en el sistema";
                    return resultado;
                }

                // ========== REALIZAR LA TRANSACCIÓN DE PAGO ==========

                // 5.1: Restar el 100% de la cuota de la cuenta del usuario
                decimal nuevoSaldoUsuario = usuario.SaldoBancario - cuota.Monto;
                _dataService.ActualizarSaldoUsuario(usuario.Id, nuevoSaldoUsuario);

                // 5.2: Calcular comisiones
                decimal montoParaEmpresa = cuota.Monto * PORCENTAJE_EMPRESA; // 95%
                decimal comisionBanco = cuota.Monto * PORCENTAJE_COMISION_BANCO; // 5%

                // 5.3: Sumar 95% a la empresa
                decimal nuevoSaldoEmpresa = empresa.SaldoAcumulado + montoParaEmpresa;
                _dataService.ActualizarSaldoEmpresa(empresa.Id, nuevoSaldoEmpresa);

                // 5.4: Sumar 5% a las comisiones del banco
                decimal nuevasComisiones = _dataService.ObtenerComisionesBanco() + comisionBanco;
                _dataService.ActualizarComisionesBanco(nuevasComisiones);

                // 5.5: Marcar cuota como pagada
                _dataService.MarcarCuotaComoPagada(idCuota);

                // CONSTRUCCIÓN DEL RESULTADO EXITOSO
                resultado.Exitoso = true;
                resultado.Mensaje = "Pago realizado exitosamente";
                resultado.MontoOriginal = cuota.Monto;
                resultado.MontoParaEmpresa = montoParaEmpresa;
                resultado.ComisionBanco = comisionBanco;
                resultado.SaldoRestante = nuevoSaldoUsuario;
                resultado.Empresa = empresa.Nombre;
                resultado.FechaPago = DateTime.Now;

                return resultado;
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "Error al procesar el pago: " + ex.Message;
                return resultado;
            }
        }

        /// <summary>
        /// Calcula la mora acumulada (Q25 por mes atrasado) entre la fecha de vencimiento y la fecha actual
        /// </summary>
        public decimal CalcularMora(Cuota cuota, DateTime fechaReferencia)
        {
            if (cuota == null || !cuota.FechaVencimiento.HasValue)
                return 0m;

            var fechaVenc = cuota.FechaVencimiento.Value.Date;
            if (fechaReferencia.Date <= fechaVenc)
                return 0m;

            // calcular meses completos entre fechaVenc y fechaReferencia
            int mesesAtraso = ((fechaReferencia.Year - fechaVenc.Year) * 12) + (fechaReferencia.Month - fechaVenc.Month);
            if (mesesAtraso < 1) return 0m;

            return mesesAtraso * MORA_POR_MES;
        }

        /// <summary>
        /// Obtiene cuotas pendientes vencidas de un usuario (FechaVencimiento anterior a fechaReferencia)
        /// </summary>
        public List<Cuota> ObtenerCuotasVencidas(int idUsuario, DateTime fechaReferencia)
        {
            var todas = _dataService.ObtenerCuotasPendientesPorUsuario(idUsuario);
            return todas.Where(c => c.FechaVencimiento.HasValue && c.FechaVencimiento.Value.Date < fechaReferencia.Date).ToList();
        }

        /// <summary>
        /// Procesa el pago de una cuota, pero antes verifica y liquida cuotas vencidas (más antiguas) aplicando mora acumulada.
        /// Regla: si hay cuotas anteriores pendientes, se cobrarán primero (incluyendo mora) en orden cronológico.
        /// </summary>
        public ResultadoPago ProcesarPagoCuotaConMora(int idUsuario, int idCuota)
        {
            var resultado = new ResultadoPago();

            try
            {
                // Obtener cuota objetivo (debe estar pendiente)
                var cuotaObjetivo = _dataService.ObtenerCuotaPorId(idCuota);
                if (cuotaObjetivo == null || cuotaObjetivo.Estado != "Pendiente")
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "La cuota no existe o no está pendiente";
                    return resultado;
                }

                if (cuotaObjetivo.IdUsuario != idUsuario)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "No tiene permiso para pagar esta cuota";
                    return resultado;
                }

                var usuario = _dataService.ObtenerUsuarioPorId(idUsuario);
                if (usuario == null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Usuario no encontrado";
                    return resultado;
                }

                // Obtener todas las cuotas pendientes del usuario y ordenar por fecha de vencimiento asc
                var pendientes = _dataService.ObtenerCuotasPendientesPorUsuario(idUsuario)
                    .OrderBy(c => c.FechaVencimiento ?? DateTime.MaxValue).ToList();

                // Fecha de referencia: hoy
                var hoy = DateTime.Now.Date;

                // Verificar y liquidar cuotas vencidas anteriores a la cuota objetivo
                decimal totalMoraAplicada = 0m;
                decimal totalPagado = 0m;

                // Recorremos las cuotas pendientes en orden; cuando encontramos la cuotaObjetivo, la procesamos al final
                foreach (var cuota in pendientes)
                {
                    if (cuota.Id == idCuota)
                    {
                        // procesaremos la cuota objetivo después de limpiar anteriores
                        break;
                    }

                    // Solo cuotas con FechaVencimiento anterior a hoy se consideran vencidas
                    if (cuota.FechaVencimiento.HasValue && cuota.FechaVencimiento.Value.Date < hoy)
                    {
                        // Calcular mora acumulada
                        var mora = CalcularMora(cuota, hoy);
                        var totalAPagar = cuota.Monto + mora;

                        if (usuario.SaldoBancario >= totalAPagar)
                        {
                            // Pagar cuota atrasada
                            usuario.SaldoBancario -= totalAPagar;

                            var montoParaEmpresa = cuota.Monto * PORCENTAJE_EMPRESA;
                            var comisionBanco = cuota.Monto * PORCENTAJE_COMISION_BANCO;

                            // Actualizar empresa y comisiones
                            var empresa = _dataService.ObtenerEmpresaPorId(cuota.IdEmpresa);
                            if (empresa != null)
                            {
                                _dataService.ActualizarSaldoEmpresa(empresa.Id, empresa.SaldoAcumulado + montoParaEmpresa);
                            }

                            _dataService.ActualizarComisionesBanco(_dataService.ObtenerComisionesBanco() + comisionBanco);

                            // Marcar cuota como pagada con mora
                            _dataService.MarcarCuotaComoPagadaConMora(cuota.Id, mora);

                            totalMoraAplicada += mora;
                            totalPagado += totalAPagar;
                        }
                        else
                        {
                            resultado.Exitoso = false;
                            resultado.Mensaje = "Saldo insuficiente para liquidar cuotas vencidas anteriores. Por favor, abone primero.";
                            return resultado;
                        }
                    }
                }

                // Actualizar el saldo del usuario en la BD antes de procesar la cuota objetivo
                _dataService.ActualizarSaldoUsuario(usuario.Id, usuario.SaldoBancario);

                // Ahora procesar la cuota objetivo (puede ser que ya esté pagada si era anterior)
                var cuotaActualizada = _dataService.ObtenerCuotaPorId(idCuota);
                if (cuotaActualizada == null || cuotaActualizada.Estado != "Pendiente")
                {
                    // Si la cuota ya fue pagada por el proceso anterior, devolver éxito
                    resultado.Exitoso = true;
                    resultado.Mensaje = "Se pagaron las cuotas vencidas anteriores.";
                    resultado.MontoOriginal = totalPagado;
                    resultado.MontoParaEmpresa = 0m;
                    resultado.ComisionBanco = 0m;
                    resultado.SaldoRestante = usuario.SaldoBancario;
                    resultado.Empresa = cuotaObjetivo != null ? _dataService.ObtenerEmpresaPorId(cuotaObjetivo.IdEmpresa)?.Nombre : string.Empty;
                    resultado.FechaPago = DateTime.Now;
                    return resultado;
                }

                // Validar saldo suficiente para cuota objetivo (solo monto, mora si aplica)
                var moraParaObjetivo = CalcularMora(cuotaActualizada, hoy);
                var totalObjetivo = cuotaActualizada.Monto + moraParaObjetivo;

                if (usuario.SaldoBancario < totalObjetivo)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Saldo insuficiente para pagar la cuota actual (incluyendo mora).";
                    return resultado;
                }

                // Realizar el pago de la cuota objetivo
                usuario.SaldoBancario -= totalObjetivo;
                var montoParaEmpresaObj = cuotaActualizada.Monto * PORCENTAJE_EMPRESA;
                var comisionBancoObj = cuotaActualizada.Monto * PORCENTAJE_COMISION_BANCO;

                var empresaObj = _dataService.ObtenerEmpresaPorId(cuotaActualizada.IdEmpresa);
                if (empresaObj != null)
                {
                    _dataService.ActualizarSaldoEmpresa(empresaObj.Id, empresaObj.SaldoAcumulado + montoParaEmpresaObj);
                }

                _dataService.ActualizarComisionesBanco(_dataService.ObtenerComisionesBanco() + comisionBancoObj);

                // Marcar cuota objetivo como pagada con mora
                _dataService.MarcarCuotaComoPagadaConMora(cuotaActualizada.Id, moraParaObjetivo);

                // Actualizar saldo del usuario
                _dataService.ActualizarSaldoUsuario(usuario.Id, usuario.SaldoBancario);

                // Construir resultado final
                resultado.Exitoso = true;
                resultado.Mensaje = "Pago realizado: cuotas vencidas anteriores (si existían) y cuota seleccionada.";
                resultado.MontoOriginal = totalPagado + cuotaActualizada.Monto;
                resultado.MontoParaEmpresa = (totalPagado + cuotaActualizada.Monto) * PORCENTAJE_EMPRESA;
                resultado.ComisionBanco = (totalPagado + cuotaActualizada.Monto) * PORCENTAJE_COMISION_BANCO;
                resultado.SaldoRestante = usuario.SaldoBancario;
                resultado.Empresa = empresaObj?.Nombre;
                resultado.FechaPago = DateTime.Now;

                return resultado;
            }
            catch (Exception ex)
            {
                return new ResultadoPago
                {
                    Exitoso = false,
                    Mensaje = "Error procesando pago con mora: " + ex.Message
                };
            }
        }

        // ============================================================================
        // MÉTODOS PARA OPERACIONES DE CAJERO (Banco)
        // ============================================================================

        /// <summary>
        /// Realiza un depósito de dinero a la cuenta de un usuario
        /// Operación realizada por un Cajero del banco
        /// </summary>
        /// <param name="idUsuario">ID del usuario</param>
        /// <param name="monto">Monto a depositar</param>
        /// <returns>true si el depósito fue exitoso, false en caso contrario</returns>
        public bool RealizarDeposito(int idUsuario, decimal monto)
        {
            // Validar que el monto sea positivo
            if (monto <= 0)
                return false;

            // Realizar el depósito
            return _dataService.DepositarDinero(idUsuario, monto);
        }

        /// <summary>
        /// Busca el usuario y retorna su información
        /// Usado por los Cajeros para localizar usuarios
        /// </summary>
        /// <param name="idUsuario">ID del usuario a buscar</param>
        /// <returns>Usuario encontrado o null</returns>
        public Usuario BuscarUsuarioParaDeposito(int idUsuario)
        {
            return _dataService.ObtenerUsuarioPorId(idUsuario);
        }

        // ============================================================================
        // MÉTODOS PARA OPERACIONES DE ADMIN DE SERVICIOS
        // ============================================================================

        /// <summary>
        /// Crea una nueva cuota para un usuario
        /// Operación realizada por un Admin del servicio (Empresa)
        /// </summary>
        /// <param name="idUsuario">ID del usuario</param>
        /// <param name="idEmpresa">ID de la empresa</param>
        /// <param name="mes">Mes en formato "YYYY-MM" o nombre de mes</param>
        /// <param name="monto">Monto de la cuota</param>
        /// <returns>Objeto con resultado de la operación</returns>
        public ResultadoCreacionCuota CrearCuota(int idUsuario, int idEmpresa, string mes, decimal monto)
        {
            var resultado = new ResultadoCreacionCuota();

            try
            {
                // Validar usuario
                var usuario = _dataService.ObtenerUsuarioPorId(idUsuario);
                if (usuario == null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "El usuario no existe";
                    return resultado;
                }

                // Validar empresa
                var empresa = _dataService.ObtenerEmpresaPorId(idEmpresa);
                if (empresa == null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "La empresa no existe";
                    return resultado;
                }

                // Validar monto
                if (monto <= 0)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "El monto debe ser mayor a cero";
                    return resultado;
                }

                // Verificar que no exista una cuota duplicada para el mismo usuario, empresa y mes
                var cuotasExistentes = _dataService.ObtenerCuotasPorUsuario(idUsuario)
                    .Where(c => c.IdEmpresa == idEmpresa && c.Mes == mes)
                    .ToList();

                if (cuotasExistentes.Any())
                {
                    var cuotaExistente = cuotasExistentes.First();
                    resultado.Exitoso = false;

                    // Mejorar el mensaje indicando si está pagada o pendiente
                    if (cuotaExistente.Estado == "Pagado")
                    {
                        resultado.Mensaje = "Esta cuota ya fue pagada. No se puede crear una cuota duplicada.";
                    }
                    else
                    {
                        resultado.Mensaje = "Ya existe una cuota pendiente de pago para este usuario, empresa y mes. El usuario debe pagarla antes de crear una nueva.";
                    }
                    return resultado;
                }

                // Crear la cuota
                var cuota = _dataService.CrearNuevaCuota(idUsuario, idEmpresa, mes, monto);

                resultado.Exitoso = true;
                resultado.Mensaje = "Cuota creada exitosamente";
                resultado.Cuota = cuota;
                resultado.UsuarioNombre = usuario.Nombre;
                resultado.EmpresaNombre = empresa.Nombre;

                return resultado;
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "Error al crear la cuota: " + ex.Message;
                return resultado;
            }
        }

        /// <summary>
        /// Obtiene todas las cuotas pendientes de un usuario
        /// </summary>
        public List<Cuota> ObtenerCuotasPendientes(int idUsuario)
        {
            return _dataService.ObtenerCuotasPendientesPorUsuario(idUsuario);
        }

        /// <summary>
        /// Obtiene todas las cuotas (pendientes y pagadas) de un usuario
        /// </summary>
        public List<Cuota> ObtenerHistorialCuotas(int idUsuario)
        {
            return _dataService.ObtenerCuotasPorUsuario(idUsuario);
        }

        /// <summary>
        /// Obtiene la información de todas las empresas del sistema
        /// </summary>
        public List<Empresa> ObtenerEmpresas()
        {
            return _dataService.ObtenerTodasLasEmpresas();
        }

        /// <summary>
        /// Retira dinero de la cuenta del usuario
        /// </summary>
        public bool RetirarDinero(int idUsuario, decimal monto)
        {
            if (monto <= 0)
                return false;

            return _dataService.RetirarDinero(idUsuario, monto);
        }

        /// <summary>
        /// Crea un nuevo usuario en el sistema
        /// </summary>
        public ResultadoCreacionUsuario CrearUsuario(int id, string nombre, string pin, string rol, decimal saldoInicial)
        {
            var resultado = new ResultadoCreacionUsuario();

            try
            {
                // Validar que el usuario no exista
                var usuarioExistente = _dataService.ObtenerUsuarioPorId(id);
                if (usuarioExistente != null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Ya existe un usuario con ese ID";
                    return resultado;
                }

                // Validar datos
                if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(pin))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Nombre y PIN son requeridos";
                    return resultado;
                }

                // Crear el usuario
                var usuario = _dataService.CrearNuevoUsuario(id, nombre, pin, rol, saldoInicial);

                resultado.Exitoso = true;
                resultado.Mensaje = "Usuario creado exitosamente";
                resultado.Usuario = usuario;

                return resultado;
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.Mensaje = "Error al crear usuario: " + ex.Message;
                return resultado;
            }
        }

        /// <summary>
        /// Obtiene el máximo ID de usuario en el sistema
        /// </summary>
        public int ObtenerMaxIdUsuario()
        {
            return _dataService.ObtenerMaxIdUsuario();
        }
    }

    // ============================================================================
    // CLASES DE RESULTADO Y RESPONSE
    // ============================================================================

    /// <summary>
    /// Clase que contiene el resultado de un pago
    /// </summary>
    public class ResultadoPago
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public decimal MontoOriginal { get; set; }
        public decimal MontoParaEmpresa { get; set; }
        public decimal ComisionBanco { get; set; }
        public decimal SaldoRestante { get; set; }
        public string Empresa { get; set; }
        public DateTime FechaPago { get; set; }
    }

    /// <summary>
    /// Clase que contiene el resultado de la creación de una cuota
    /// </summary>
    public class ResultadoCreacionCuota
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public Cuota Cuota { get; set; }
        public string UsuarioNombre { get; set; }
        public string EmpresaNombre { get; set; }
    }

    /// <summary>
    /// Clase que contiene el resultado de la creación de un usuario
    /// </summary>
    public class ResultadoCreacionUsuario
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public Usuario Usuario { get; set; }
    }

    /// <summary>
    /// Clase auxiliar para búsqueda de usuarios en cajero
    /// </summary>
    public class UsuarioConCuenta
    {
        public Usuario Usuario { get; set; }
    }
}
