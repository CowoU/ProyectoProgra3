using ProyectoProgra3.Models;

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

        public PagoService(DataService dataService)
        {
            _dataService = dataService;
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
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Ya existe una cuota para este usuario, empresa y mes";
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
    /// Clase auxiliar para búsqueda de usuarios en cajero
    /// </summary>
    public class UsuarioConCuenta
    {
        public Usuario Usuario { get; set; }
    }
}
