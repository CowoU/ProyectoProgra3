// ============================================================================
// APLICACIÓN FRONTEND - PORTAL DE PAGOS DE SERVICIOS
// ============================================================================

// VARIABLES GLOBALES
let usuarioActual = null;
let cuotaACtualizar = null;
let empresasCache = null; // Cache de empresas para búsquedas rápidas

// URL BASE DE LA API
const API_URL = 'https://proyectobanco-e5a8acfedfccfkbg.eastus2-01.azurewebsites.net/api';

// Mapeo de formatos de ID por rol
const FORMATO_ID = {
    'Cliente': 'cli',
    'Cajero': 'caj',
    'Admin': 'adm'
};

// Función para generar formato visual de ID
function generarFormatoId(idNumerico, rol) {
    const prefijo = FORMATO_ID[rol] || 'usr';
    const año = new Date().getFullYear().toString().slice(2); // últimos 2 dígitos del año
    const idFormato = String(idNumerico).padStart(2, '0');
    return `${prefijo}-${año}-${idFormato}`;
}

// Función para mostrar ID con formato visual
function mostrarIdFormato(idNumerico, rol) {
    return `<span title="ID: ${idNumerico}">${generarFormatoId(idNumerico, rol)}</span>`;
}

// ============================================================================
// FUNCIONES DE AUTENTICACIÓN
// ============================================================================

async function iniciarSesion(event) {
    event.preventDefault();
    const id = document.getElementById('idUsuario').value;
    const pin = document.getElementById('pinUsuario').value;
    const mensajeError = document.getElementById('mensajeError');

    try {
        const response = await fetch(`${API_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ id: parseInt(id), pin: pin })
        });

        const data = await response.json();

        if (data.exitoso) {
            // Obtener saldo actual del usuario desde la BD
            const usuarioResponse = await fetch(`${API_URL}/cajero/buscar-usuario/${id}`);
            const usuarioData = await usuarioResponse.json();

            usuarioActual = {
                id: data.usuario.id,
                nombre: data.usuario.nombre,
                rol: data.usuario.rol,
                saldo: usuarioData.usuario ? usuarioData.usuario.saldoBancario : 0
            };
            localStorage.setItem('usuarioActual', JSON.stringify(usuarioActual));

            mostrarPantallaPorRol(usuarioActual.rol);
        } else {
            mensajeError.textContent = data.mensaje || "Credenciales incorrectas";
            mensajeError.classList.remove('d-none');
        }
    } catch (error) {
        console.error("Error en login:", error);
        mensajeError.textContent = "Error de conexión con el servidor.";
        mensajeError.classList.remove('d-none');
    }
}

function cerrarSesion() {
    usuarioActual = null;
    localStorage.removeItem('usuarioActual');

    document.getElementById('navbar').style.display = 'none';
    document.getElementById('loginScreen').classList.remove('d-none');
    document.getElementById('panelCliente').classList.add('d-none');
    document.getElementById('panelCajero').classList.add('d-none');
    document.getElementById('panelAdmin').classList.add('d-none');
    document.getElementById('mensajeError').classList.add('d-none');
}

function mostrarPantallaPorRol(rol) {
    document.getElementById('navbar').style.display = 'block';
    document.getElementById('usuarioActual').innerHTML =
        `<i class="fas fa-user"></i> ${usuarioActual.nombre} ${mostrarIdFormato(usuarioActual.id, rol)} <span class="badge bg-info">${rol}</span>`;

    const pantallas = ['loginScreen', 'panelCliente', 'panelCajero', 'panelAdmin'];
    pantallas.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.classList.add('d-none');
    });

    if (rol.toUpperCase().includes('CLIENTE')) {
        document.getElementById('panelCliente').classList.remove('d-none');
        cargarPanelCliente();
    }
    else if (rol.toUpperCase().includes('CAJERO')) {
        document.getElementById('panelCajero').classList.remove('d-none');
    }
    else if (rol.toUpperCase().includes('ADMIN')) {
        document.getElementById('panelAdmin').classList.remove('d-none');
        cargarPanelAdmin();
    }
}

// ============================================================================
// FUNCIONES DEL PANEL DE CLIENTE
// ============================================================================

async function cargarPanelCliente() {
    try {
        document.getElementById('clienteNombre').textContent = usuarioActual.nombre;
        document.getElementById('clienteId').innerHTML = mostrarIdFormato(usuarioActual.id, usuarioActual.rol);

        // Cargar empresas en cache para uso posterior
        await cargarEmpresas();

        // Obtener saldo actualizado de la BD
        const saldoResponse = await fetch(`${API_URL}/cajero/buscar-usuario/${usuarioActual.id}`);
        const saldoData = await saldoResponse.json();
        const saldo = saldoData.usuario ? saldoData.usuario.saldoBancario : 0;

        document.getElementById('saldoActual').textContent = parseFloat(saldo).toFixed(2);
        usuarioActual.saldo = saldo;

        const response = await fetch(`${API_URL}/pagos/cuotas-pendientes/${usuarioActual.id}`);
        const data = await response.json();

        if (data.cuotasPendientes && data.cuotasPendientes.length > 0) {
            document.getElementById('sinCuotas').classList.add('d-none');
            document.getElementById('tablaCuotas').classList.remove('d-none');
            mostrarCuotasPendientes(data.cuotasPendientes);
        } else {
            document.getElementById('sinCuotas').classList.remove('d-none');
            document.getElementById('tablaCuotas').classList.add('d-none');
        }

        cargarHistorialCuotas();
    } catch (error) {
        console.error("Error cargando panel cliente:", error);
    }
}

function mostrarCuotasPendientes(cuotas) {
    const tablaCuotasDiv = document.getElementById('tablaCuotas');
    const sinCuotasDiv = document.getElementById('sinCuotas');

    if (!cuotas || cuotas.length === 0) {
        sinCuotasDiv.classList.remove('d-none');
        tablaCuotasDiv.innerHTML = '';
        return;
    }

    sinCuotasDiv.classList.add('d-none');

    let html = `
        <table class="table table-hover">
            <thead>
                <tr>
                    <th>ID</th>
                    <th>Empresa</th>
                    <th>Mes</th>
                    <th>Monto</th>
                    <th>Acción</th>
                </tr>
            </thead>
            <tbody>
    `;

    cuotas.forEach(cuota => {
        const empresa = empresasCache?.find(e => e.id === cuota.idEmpresa);
        const nombreEmpresa = empresa ? empresa.nombre : 'Desconocida';
        html += `
            <tr>
                <td><strong>${cuota.id}</strong></td>
                <td>${nombreEmpresa}</td>
                <td>${cuota.mes}</td>
                <td><strong>Q${parseFloat(cuota.monto).toFixed(2)}</strong></td>
                <td>
                    <button class="btn btn-sm btn-primary" 
                            onclick="abrirModalPago(${cuota.id}, '${nombreEmpresa}', '${cuota.mes}', ${cuota.monto})">
                        <i class="fas fa-money-bill"></i> Pagar
                    </button>
                </td>
            </tr>
        `;
    });

    html += `</tbody></table>`;
    tablaCuotasDiv.innerHTML = html;
}

function abrirModalPago(idCuota, nombreEmpresa, mes, monto) {
    cuotaACtualizar = idCuota;
    document.getElementById('detalleEmpresa').textContent = nombreEmpresa;
    document.getElementById('detalleMes').textContent = mes;
    document.getElementById('detalleMontoOriginal').textContent = parseFloat(monto).toFixed(2);

    // Llamar al backend para calcular la mora
    calcularYMostrarMora(idCuota, parseFloat(monto));

    const modal = new bootstrap.Modal(document.getElementById('modalConfirmarPago'));
    modal.show();
}

async function calcularYMostrarMora(idCuota, montoOriginal) {
    try {
        const response = await fetch(`${API_URL}/pagos/calcular-mora/${idCuota}`);
        const data = await response.json();

        if (data.exitoso) {
            const mora = parseFloat(data.mora);
            const total = parseFloat(data.total);

            // Mostrar la mora si es mayor a 0
            if (mora > 0) {
                document.getElementById('detalleFilaMora').style.display = 'flex';
                document.getElementById('detalleMoraAmount').textContent = mora.toFixed(2);

                // Calcular cuántos meses de atraso
                const mesesAtraso = Math.round(mora / 25);
                document.getElementById('detalleCalculoMora').textContent = `${mesesAtraso} mes(es) × Q25`;
            } else {
                document.getElementById('detalleFilaMora').style.display = 'none';
            }

            document.getElementById('detalleTotalPago').textContent = total.toFixed(2);
        } else {
            document.getElementById('detalleFilaMora').style.display = 'none';
            document.getElementById('detalleTotalPago').textContent = montoOriginal.toFixed(2);
        }
    } catch (error) {
        console.error('Error calculando mora:', error);
        document.getElementById('detalleFilaMora').style.display = 'none';
        document.getElementById('detalleTotalPago').textContent = montoOriginal.toFixed(2);
    }
}

async function confirmarPago() {
    try {
        const response = await fetch(`${API_URL}/pagos/pagar-cuota-con-mora`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                idUsuario: usuarioActual.id,
                idCuota: cuotaACtualizar
            })
        });

        const data = await response.json();
        bootstrap.Modal.getInstance(document.getElementById('modalConfirmarPago')).hide();

        if (data.exitoso) {
            mostrarResultadoPagoExitoso(data.detalles);
            setTimeout(() => { cargarPanelCliente(); }, 2000);
        } else {
            mostrarResultadoPagoError(data.mensaje);
        }

    } catch (error) {
        console.error('Error al procesar pago:', error);
        mostrarResultadoPagoError('Error al procesar el pago: ' + error.message);
    }
}

function mostrarResultadoPagoExitoso(detalles) {
    const html = `
        <div class="modal-header bg-success text-white">
            <h5 class="modal-title"><i class="fas fa-check-circle"></i> ¡Pago Realizado Exitosamente!</h5>
            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
        </div>
        <div class="modal-body">
            <div class="alert alert-success"><strong>Pago completado con éxito</strong></div>
            <div class="row">
                <div class="col-md-6">
                    <p><strong>Empresa:</strong></p>
                    <p>${detalles.empresa}</p>
                </div>
                <div class="col-md-6">
                    <p><strong>Fecha de Pago:</strong></p>
                    <p>${new Date(detalles.fechaPago).toLocaleString('es-ES')}</p>
                </div>
            </div>
            <hr>
            <div class="row">
                <div class="col-md-6">
                    <p><strong>Monto Pagado:</strong></p>
                    <p class="display-6" style="color: #dc2626;">Q${parseFloat(detalles.montoOriginal).toFixed(2)}</p>
                </div>
                <div class="col-md-6">
                    <p><strong>Saldo Restante:</strong></p>
                    <p class="display-6" style="color: #16a34a;">Q${parseFloat(detalles.saldoRestante).toFixed(2)}</p>
                </div>
            </div>
            <hr>
            <p class="small text-muted">
                <strong>Desglose del pago:</strong><br>
                - Monto para empresa (95%): Q${parseFloat(detalles.montoParaEmpresa).toFixed(2)}<br>
                - Comisión del banco (5%): Q${parseFloat(detalles.comisionBanco).toFixed(2)}
            </p>
        </div>
        <div class="modal-footer">
            <button type="button" class="btn btn-success" data-bs-dismiss="modal">
                <i class="fas fa-check"></i> Aceptar
            </button>
        </div>
    `;

    document.getElementById('modalContenido').innerHTML = html;
    const modal = new bootstrap.Modal(document.getElementById('modalResultadoPago'));
    modal.show();
}

function mostrarResultadoPagoError(mensaje) {
    const html = `
        <div class="modal-header bg-danger text-white">
            <h5 class="modal-title"><i class="fas fa-times-circle"></i> Error al Procesar Pago</h5>
            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
        </div>
        <div class="modal-body">
            <div class="alert alert-danger"><strong>${mensaje}</strong></div>
        </div>
        <div class="modal-footer">
            <button type="button" class="btn btn-danger" data-bs-dismiss="modal">
                <i class="fas fa-times"></i> Aceptar
            </button>
        </div>
    `;

    document.getElementById('modalContenido').innerHTML = html;
    const modal = new bootstrap.Modal(document.getElementById('modalResultadoPago'));
    modal.show();
}

async function cargarHistorialCuotas() {
    try {
        const response = await fetch(`${API_URL}/pagos/historial-cuotas/${usuarioActual.id}`);
        const data = await response.json();

        if (data.cuotas && data.cuotas.length > 0) {
            mostrarHistorialCuotas(data.cuotas);
        } else {
            document.getElementById('historialCuotas').innerHTML = 
                '<p class="text-muted">No hay historial de cuotas</p>';
        }
    } catch (error) {
        console.error('Error al cargar historial:', error);
    }
}

function mostrarHistorialCuotas(cuotas) {
    const historialDiv = document.getElementById('historialCuotas');

    let html = `
        <table class="table table-hover">
            <thead>
                <tr>
                    <th>Empresa</th>
                    <th>Mes</th>
                    <th>Monto</th>
                    <th>Estado</th>
                </tr>
            </thead>
            <tbody>
    `;

    cuotas.forEach(cuota => {
        const empresa = empresasCache?.find(e => e.id === cuota.idEmpresa);
        const nombreEmpresa = empresa ? empresa.nombre : 'Desconocida';
        
        let estadoTexto, rowClase, textColor;
        if (cuota.estado === 'Pagado') {
            estadoTexto = 'Pagado';
            rowClase = 'table-success';
            textColor = '#16a34a';
        } else {
            estadoTexto = 'Pendiente';
            rowClase = 'table-warning';
            textColor = '#f59e0b';
        }

        html += `
            <tr class="${rowClase}">
                <td><strong>${nombreEmpresa}</strong></td>
                <td><strong>${cuota.mes}</strong></td>
                <td>Q${parseFloat(cuota.monto).toFixed(2)}</td>
                <td><strong style="color: ${textColor}">${estadoTexto}</strong></td>
            </tr>
        `;
    });

    html += `</tbody></table>`;
    historialDiv.innerHTML = html;
}

// Función para retirar dinero (Cliente)
async function abrirModalRetiroCliente() {
    document.getElementById('montoRetiroCliente').value = '';
    const modal = new bootstrap.Modal(document.getElementById('modalRetiroCliente'));
    modal.show();
}

async function confirmarRetiroCliente() {
    const monto = parseFloat(document.getElementById('montoRetiroCliente').value);

    if (isNaN(monto) || monto <= 0) {
        mostrarAlertaRetiro('error', 'Ingrese un monto válido');
        return;
    }

    if (monto > usuarioActual.saldo) {
        mostrarAlertaRetiro('error', 'Saldo insuficiente para realizar el retiro');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/pagos/retirar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                idUsuario: usuarioActual.id,
                monto: monto
            })
        });

        const data = await response.json();

        if (data.exitoso) {
            bootstrap.Modal.getInstance(document.getElementById('modalRetiroCliente')).hide();
            mostrarAlertaRetiro('success', `Retiro de Q${monto.toFixed(2)} realizado exitosamente`);
            setTimeout(() => { cargarPanelCliente(); }, 1500);
        } else {
            mostrarAlertaRetiro('error', data.mensaje);
        }
    } catch (error) {
        console.error('Error:', error);
        mostrarAlertaRetiro('error', 'Error al procesar retiro: ' + error.message);
    }
}

function mostrarAlertaRetiro(tipo, mensaje) {
    const alerta = document.getElementById('alertaRetiroCliente');
    alerta.className = `alert alert-${tipo === 'success' ? 'success' : 'danger'}`;
    alerta.textContent = mensaje;
    alerta.classList.remove('d-none');

    setTimeout(() => {
        alerta.classList.add('d-none');
    }, 4000);
}

// ============================================================================
// FUNCIONES DEL PANEL DE CAJERO
// ============================================================================

async function buscarUsuarioCajero() {
    const valorInput = document.getElementById('idUsuarioBusqueda').value.trim();
    const idUsuario = parseInt(valorInput);

    if (isNaN(idUsuario) || idUsuario <= 0) {
        mostrarErrorBusqueda('Por favor, ingrese un ID válido');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/cajero/buscar-usuario/${idUsuario}`);
        const data = await response.json();

        if (data.exitoso) {
            // ✅ VALIDACIÓN: Solo permite buscar usuarios con rol "Cliente"
            if (data.usuario.rol.toUpperCase() !== 'CLIENTE') {
                mostrarErrorBusqueda('Solo se pueden realizar operaciones con usuarios de rol Cliente');
                document.getElementById('infoUsuarioBusqueda').classList.add('d-none');
                document.getElementById('montoDeposito').disabled = true;
                document.getElementById('btnDepositar').disabled = true;
                document.getElementById('btnRetiroCajero').disabled = true;
                return;
            }

            document.getElementById('usuarioBusquedaNombre').textContent = data.usuario.nombre;
            document.getElementById('idCuentaBancaria').innerHTML = mostrarIdFormato(data.usuario.id, data.usuario.rol);
            document.getElementById('saldoCuentaBancaria').textContent = 
                parseFloat(data.usuario.saldoBancario).toFixed(2);

            document.getElementById('infoUsuarioBusqueda').classList.remove('d-none');
            document.getElementById('errorBusqueda').classList.add('d-none');

            document.getElementById('montoDeposito').disabled = false;
            document.getElementById('btnDepositar').disabled = false;
            document.getElementById('btnRetiroCajero').disabled = false;

            document.getElementById('montoDeposito').dataset.idUsuario = data.usuario.id;

        } else {
            mostrarErrorBusqueda(data.mensaje);
            document.getElementById('infoUsuarioBusqueda').classList.add('d-none');
            document.getElementById('montoDeposito').disabled = true;
            document.getElementById('btnDepositar').disabled = true;
            document.getElementById('btnRetiroCajero').disabled = true;
        }
    } catch (error) {
        mostrarErrorBusqueda('Error al buscar usuario: ' + error.message);
        console.error('Error:', error);
    }
}

async function realizarDeposito(event) {
    event.preventDefault();

    const idUsuario = parseInt(document.getElementById('montoDeposito').dataset.idUsuario);
    const monto = parseFloat(document.getElementById('montoDeposito').value);

    if (monto <= 0) {
        mostrarMensajeDeposito('error', 'El monto debe ser mayor a cero');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/cajero/depositar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                idUsuario: idUsuario,
                monto: monto
            })
        });

        const data = await response.json();

        if (data.exitoso) {
            mostrarMensajeDeposito('success', 
                `✓ Depósito de Q${monto.toFixed(2)} realizado exitosamente`);

            document.getElementById('formDeposito').reset();
            document.getElementById('montoDeposito').disabled = true;
            document.getElementById('btnDepositar').disabled = true;

            setTimeout(() => {
                buscarUsuarioCajero();
            }, 1500);
        } else {
            mostrarMensajeDeposito('error', data.mensaje);
        }
    } catch (error) {
        mostrarMensajeDeposito('error', 'Error al realizar depósito: ' + error.message);
        console.error('Error:', error);
    }
}

async function abrirModalRetiroCajero() {
    const idUsuario = document.getElementById('montoDeposito').dataset.idUsuario;
    if (!idUsuario) {
        mostrarMensajeDeposito('error', 'Seleccione un usuario primero');
        return;
    }

    document.getElementById('montoRetiroCajero').value = '';
    document.getElementById('montoRetiroCajero').dataset.idUsuario = idUsuario;
    const modal = new bootstrap.Modal(document.getElementById('modalRetiroCajero'));
    modal.show();
}

async function confirmarRetiroCajero() {
    const monto = parseFloat(document.getElementById('montoRetiroCajero').value);
    const idUsuario = parseInt(document.getElementById('montoRetiroCajero').dataset.idUsuario);

    if (isNaN(monto) || monto <= 0) {
        mostrarMensajeDeposito('error', 'Ingrese un monto válido');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/cajero/retirar`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                idUsuario: idUsuario,
                monto: monto
            })
        });

        const data = await response.json();

        bootstrap.Modal.getInstance(document.getElementById('modalRetiroCajero')).hide();

        if (data.exitoso) {
            mostrarMensajeDeposito('success', 
                `✓ Retiro de Q${monto.toFixed(2)} realizado exitosamente`);
            setTimeout(() => { buscarUsuarioCajero(); }, 1500);
        } else {
            mostrarMensajeDeposito('error', data.mensaje);
        }
    } catch (error) {
        mostrarMensajeDeposito('error', 'Error al realizar retiro: ' + error.message);
    }
}

function mostrarErrorBusqueda(mensaje) {
    const errorDiv = document.getElementById('errorBusqueda');
    errorDiv.textContent = mensaje;
    errorDiv.classList.remove('d-none');
}

function mostrarMensajeDeposito(tipo, mensaje) {
    const mensajeDiv = document.getElementById('mensajeDeposito');
    mensajeDiv.className = `alert alert-${tipo}`;
    mensajeDiv.textContent = mensaje;
    mensajeDiv.classList.remove('d-none');

    setTimeout(() => {
        mensajeDiv.classList.add('d-none');
    }, 4000);
}

// ============================================================================
// FUNCIONES DEL PANEL DE ADMIN DE SERVICIOS
// ============================================================================

async function cargarPanelAdmin() {
    try {
        await cargarEmpresas();
        await cargarResumenEmpresas();
        await cargarTodasLasCuotas();
        await actualizarSugerenciaId();
    } catch (error) {
        console.error('Error al cargar panel admin:', error);
    }
}

async function cargarEmpresas() {
    try {
        if (empresasCache) return; // Si ya están cargadas, no cargar de nuevo
        
        const response = await fetch(`${API_URL}/pagos/empresas`);
        const data = await response.json();
        
        if (data && Array.isArray(data)) {
            empresasCache = data;
            
            // Si estamos en panel admin, llenar select
            const select = document.getElementById('idEmpresaAdmin');
            if (select && select.children.length <= 1) {
                data.forEach(empresa => {
                    const option = document.createElement('option');
                    option.value = empresa.id;
                    option.textContent = empresa.nombre;
                    select.appendChild(option);
                });
            }
        }
    } catch (error) {
        console.error('Error cargando empresas:', error);
    }
}

async function cargarResumenEmpresas() {
    try {
        const response = await fetch(`${API_URL}/pagos/empresas`);
        const data = await response.json();

        if (data && Array.isArray(data)) {
            let html = '';
            data.forEach(empresa => {
                html += `
                    <div class="card mb-3">
                        <div class="card-body">
                            <h6 class="card-title">${empresa.nombre}</h6>
                            <p class="card-text">
                                <strong>Saldo Acumulado:</strong> Q${parseFloat(empresa.saldoAcumulado).toFixed(2)}
                            </p>
                        </div>
                    </div>
                `;
            });
            document.getElementById('resumenEmpresas').innerHTML = html;
        }
    } catch (error) {
        console.error('Error cargando empresas:', error);
    }
}

async function cargarTodasLasCuotas() {
    try {
        const response = await fetch(`${API_URL}/adminservicios/todas-las-cuotas`);
        const cuotas = await response.json();

        const contenedor = document.getElementById('todasLasCuotas');

        if (cuotas && cuotas.length > 0) {
            let html = `
                <table class="table table-striped mt-3">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Usuario</th>
                            <th>Empresa</th>
                            <th>Mes</th>
                            <th>Monto</th>
                            <th>Estado</th>
                            <th>Acciones</th>
                        </tr>
                    </thead>
                    <tbody>`;

            cuotas.forEach(c => {
                const badgeClass = c.estado === 'Pagado' ? 'bg-success' : 'bg-warning';
                const rowClase = c.estado === 'Pagado' ? 'table-success' : 'table-warning';
                const empresa = empresasCache?.find(e => e.id === c.idEmpresa);
                const nombreEmpresa = empresa ? empresa.nombre : 'Desconocida';
                
                html += `
                    <tr class="${rowClase}">
                        <td>${c.id}</td>
                        <td>${c.idUsuario}</td>
                        <td>${nombreEmpresa}</td>
                        <td>${c.mes}</td>
                        <td>Q${parseFloat(c.monto).toFixed(2)}</td>
                        <td><span class="badge ${badgeClass}">${c.estado}</span></td>
                        <td>
                            <button class="btn btn-sm btn-danger" 
                                    onclick="borrarCuota(${c.id})">
                                <i class="fas fa-trash"></i> Eliminar
                            </button>
                        </td>
                    </tr>`;
            });

            html += `</tbody></table>`;
            contenedor.innerHTML = html;
        } else {
            contenedor.innerHTML = '<p class="text-center p-3">No hay cuotas registradas en el sistema.</p>';
        }
    } catch (error) {
        console.error('Error al cargar todas las cuotas:', error);
    }
}

async function actualizarSugerenciaId() {
    try {
        const rolSelect = document.getElementById('rolUsuario');
        const rol = rolSelect.value;
        
        if (!rol) {
            document.getElementById('formatoIdSugerencia').textContent = '--';
            document.getElementById('idNuevoUsuario').placeholder = 'Seleccione un rol primero';
            document.getElementById('idNuevoUsuario').value = '';
            return;
        }

        // Obtener el máximo ID actual para ese rol
        const response = await fetch(`${API_URL}/auth/obtener-max-id`);
        const data = await response.json();
        const maxId = data.maxId || 0;
        const siguienteId = maxId + 1;

        // Actualizar placeholder y formato visual
        document.getElementById('idNuevoUsuario').placeholder = `Sugerencia: ${siguienteId}`;
        document.getElementById('idNuevoUsuario').value = siguienteId;
        
        // Mostrar formato visual
        const formatoVisual = generarFormatoId(siguienteId, rol);
        document.getElementById('formatoIdSugerencia').textContent = formatoVisual;
    } catch (error) {
        console.error('Error actualizando sugerencia de ID:', error);
        document.getElementById('formatoIdSugerencia').textContent = '-- error --';
    }
}

async function crearNuevoUsuario(event) {
    event.preventDefault();

    const rol = document.getElementById('rolUsuario').value;
    const id = parseInt(document.getElementById('idNuevoUsuario').value);
    const nombre = document.getElementById('nombreNuevoUsuario').value;
    const pin = document.getElementById('pinNuevoUsuario').value;
    const saldoInicial = parseFloat(document.getElementById('saldoInicialUsuario').value) || 0;

    if (!rol || !id || !nombre || !pin) {
        mostrarMensajeCreacionUsuario('error', 'Todos los campos son requeridos');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/auth/crear-usuario`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                id: id,
                nombre: nombre,
                pin: pin,
                rol: rol,
                saldoBancario: saldoInicial
            })
        });

        const data = await response.json();

        if (data.exitoso) {
            const formatoId = generarFormatoId(id, rol);
            mostrarMensajeCreacionUsuario('success', 
                `✓ Usuario ${rol} creado exitosamente (${formatoId})`);
            document.getElementById('formCrearUsuario').reset();
            await actualizarSugerenciaId();
        } else {
            mostrarMensajeCreacionUsuario('error', data.mensaje);
        }
    } catch (error) {
        mostrarMensajeCreacionUsuario('error', 'Error al crear usuario: ' + error.message);
    }
}

function mostrarMensajeCreacionUsuario(tipo, mensaje) {
    const mensajeDiv = document.getElementById('mensajeCreacionUsuario');
    mensajeDiv.className = `alert alert-${tipo}`;
    mensajeDiv.textContent = mensaje;
    mensajeDiv.classList.remove('d-none');

    setTimeout(() => {
        mensajeDiv.classList.add('d-none');
    }, 4000);
}

async function crearNuevaCuota(event) {
    event.preventDefault();

    const idUsuario = parseInt(document.getElementById('idUsuarioAdmin').value);
    const idEmpresa = parseInt(document.getElementById('idEmpresaAdmin').value);
    const mes = document.getElementById('mesAdmin').value;
    const monto = parseFloat(document.getElementById('montoAdmin').value);

    try {
        const response = await fetch(`${API_URL}/adminservicios/crear-cuota`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                idUsuario: idUsuario,
                idEmpresa: idEmpresa,
                mes: mes,
                monto: monto
            })
        });

        const data = await response.json();

        if (data.exitoso) {
            mostrarMensajeCreacionCuota('success', 
                `✓ Cuota de Q${monto.toFixed(2)} creada exitosamente para usuario ${idUsuario}`);
            document.getElementById('formCrearCuota').reset();
            await cargarTodasLasCuotas();
        } else {
            mostrarMensajeCreacionCuota('error', data.mensaje);
        }
    } catch (error) {
        mostrarMensajeCreacionCuota('error', 'Error al crear cuota: ' + error.message);
    }
}

function mostrarMensajeCreacionCuota(tipo, mensaje) {
    const mensajeDiv = document.getElementById('mensajeCreacionCuota');
    mensajeDiv.className = `alert alert-${tipo}`;
    mensajeDiv.textContent = mensaje;
    mensajeDiv.classList.remove('d-none');

    setTimeout(() => {
        mensajeDiv.classList.add('d-none');
    }, 4000);
}

// Función para borrar cuota (Admin)
async function borrarCuota(idCuota) {
    if (!confirm('¿Desea eliminar esta cuota? Esta acción no se puede deshacer.')) return;

    try {
        const response = await fetch(`${API_URL}/adminservicios/borrar-cuota/${idCuota}`, {
            method: 'DELETE'
        });

        const data = await response.json();
        if (data.exitoso) {
            alert('Cuota eliminada correctamente');
            await cargarTodasLasCuotas();
        } else {
            alert('Error al eliminar cuota: ' + (data.mensaje || 'Error desconocido'));
        }
    } catch (error) {
        alert('Error al eliminar cuota: ' + error.message);
    }
}

// ============================================================================
// FUNCIONES PARA GESTIÓN DE USUARIOS Y RECUPERACIÓN DE PIN
// ============================================================================

/**
 * Muestra la pestaña seleccionada en el admin
 */
function mostrarTabAdmin(tab) {
    // Esta función es llamada por los botones de tab, pero Bootstrap ya lo maneja
    // Esta es más para asegurar que se carguen datos cuando sea necesario
    if (tab === 'usuarios') {
        cargarListaUsuarios();
    }
}

/**
 * Abre el modal de recuperación de PIN
 */
function abrirModalRecuperarPin() {
    document.getElementById('idUsuarioRecuperar').value = '';
    document.getElementById('formRecuperarPin').reset();
    const modal = new bootstrap.Modal(document.getElementById('modalRecuperarPin'));
    modal.show();
}

/**
 * Solicita la recuperación de PIN
 */
async function solicitarRecuperacionPin(event) {
    if (event) event.preventDefault();

    const idUsuario = document.getElementById('idUsuarioRecuperar').value;
    if (!idUsuario) {
        alert('Por favor ingresa tu ID de usuario');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/usuarios/solicitar-recuperacion-pin`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ idUsuario: parseInt(idUsuario) })
        });

        const data = await response.json();

        const mensajeDiv = document.getElementById('mensajeRecuperacion');
        if (data.exitoso) {
            mensajeDiv.className = 'alert alert-success m-3';
            mensajeDiv.textContent = '✓ ' + data.mensaje;
            mensajeDiv.classList.remove('d-none');

            setTimeout(() => {
                bootstrap.Modal.getInstance(document.getElementById('modalRecuperarPin')).hide();
                document.getElementById('formRecuperarPin').reset();
            }, 2000);
        } else {
            mensajeDiv.className = 'alert alert-danger m-3';
            mensajeDiv.textContent = '✗ ' + (data.mensaje || 'Error al solicitar');
            mensajeDiv.classList.remove('d-none');
        }
    } catch (error) {
        alert('Error: ' + error.message);
    }
}

/**
 * Carga la lista de usuarios en la tabla
 */
async function cargarListaUsuarios() {
    try {
        const response = await fetch(`${API_URL}/usuarios/todos`);
        const data = await response.json();

        if (data.exitoso) {
            mostrarTablaUsuarios(data.usuarios);
        } else {
            alert('Error al cargar usuarios: ' + (data.mensaje || 'Error desconocido'));
        }
    } catch (error) {
        alert('Error al cargar usuarios: ' + error.message);
    }
}

/**
 * Filtra usuarios por rol
 */
async function filtrarUsuarios(rol) {
    try {
        let url = `${API_URL}/usuarios/todos`;
        if (rol !== 'todos') {
            url = `${API_URL}/usuarios/filtrar-por-rol/${rol}`;
        }

        const response = await fetch(url);
        const data = await response.json();

        if (data.exitoso) {
            mostrarTablaUsuarios(data.usuarios || []);
        } else {
            alert('Error al filtrar usuarios: ' + (data.mensaje || 'Error desconocido'));
        }
    } catch (error) {
        alert('Error al filtrar usuarios: ' + error.message);
    }
}

/**
 * Muestra la tabla de usuarios con sus acciones
 */
function mostrarTablaUsuarios(usuarios) {
    const tbody = document.getElementById('cuerpoTablaUsuarios');
    tbody.innerHTML = '';

    if (!usuarios || usuarios.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">No hay usuarios</td></tr>';
        return;
    }

    usuarios.forEach(usuario => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${usuario.id}</td>
            <td>${usuario.nombre}</td>
            <td>
                <span class="badge bg-${getBadgeColor(usuario.rol)}">
                    ${usuario.rol}
                </span>
            </td>
            <td>
                <span class="text-muted">****</span>
            </td>
            <td>Q${parseFloat(usuario.saldoBancario || 0).toFixed(2)}</td>
            <td>
                <button class="btn btn-sm btn-info" onclick="abrirDetallesUsuario(${usuario.id}, '${usuario.nombre}')">
                    <i class="fas fa-eye"></i> Detalles
                </button>
            </td>
        `;
        tbody.appendChild(row);
    });
}

/**
 * Abre el modal de detalles del usuario
 */
function abrirDetallesUsuario(idUsuario, nombreUsuario) {
    // Guardamos el ID para usarlo después
    document.getElementById('idUsuarioDetalles').value = idUsuario;
    document.getElementById('nombreUsuarioDetalles').value = nombreUsuario;
    document.getElementById('pinAdminVerificacionDetalles').value = '';
    document.getElementById('detallesUsuarioInfo').innerHTML = '';

    const modal = new bootstrap.Modal(document.getElementById('modalDetallesUsuario'));
    modal.show();
}

/**
 * Obtiene los detalles completos del usuario (requiere PIN del admin)
 */
async function verDetallesUsuario() {
    const idAdmin = usuarioActual.id;
    const pinAdmin = document.getElementById('pinAdminVerificacionDetalles').value;
    const idUsuario = parseInt(document.getElementById('idUsuarioDetalles').value);

    if (!pinAdmin) {
        alert('Por favor ingresa tu PIN de administrador');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/usuarios/obtener-detalles`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                idAdmin: idAdmin,
                pinAdmin: pinAdmin,
                idUsuario: idUsuario
            })
        });

        const data = await response.json();

        if (data.exitoso) {
            const usuario = data.usuario;
            const info = `
                <div class="table-responsive">
                    <table class="table table-sm">
                        <tr><th>ID:</th><td>${usuario.id}</td></tr>
                        <tr><th>Nombre:</th><td>${usuario.nombre}</td></tr>
                        <tr><th>PIN:</th><td><code>${usuario.pin}</code></td></tr>
                        <tr><th>Rol:</th><td><span class="badge bg-${getBadgeColor(usuario.rol)}">${usuario.rol}</span></td></tr>
                        <tr><th>Saldo:</th><td>Q${parseFloat(usuario.saldoBancario || 0).toFixed(2)}</td></tr>
                    </table>
                </div>
                <button class="btn btn-danger w-100 mt-3" onclick="abrirModalCambiarPin(${usuario.id})">
                    <i class="fas fa-key"></i> Cambiar PIN
                </button>
            `;
            document.getElementById('detallesUsuarioInfo').innerHTML = info;
        } else {
            alert('Error: ' + (data.mensaje || 'PIN incorrecto o error al obtener detalles'));
        }
    } catch (error) {
        alert('Error: ' + error.message);
    }
}

/**
 * Abre el modal para cambiar PIN
 */
function abrirModalCambiarPin(idUsuario) {
    document.getElementById('idUsuarioCambiarPin').value = idUsuario;
    document.getElementById('pinAdminVerificacion').value = '';
    document.getElementById('nuevoPin').value = '';
    document.getElementById('formCambiarPin').reset();

    // Cerrar modal de detalles
    bootstrap.Modal.getInstance(document.getElementById('modalDetallesUsuario')).hide();

    const modal = new bootstrap.Modal(document.getElementById('modalCambiarPin'));
    modal.show();
}

/**
 * Cambia el PIN del usuario
 */
async function cambiarPinUsuario(event) {
    if (event) event.preventDefault();

    const idAdmin = usuarioActual.id;
    const pinAdmin = document.getElementById('pinAdminVerificacion').value;
    const idUsuario = parseInt(document.getElementById('idUsuarioCambiarPin').value);
    const nuevoPin = document.getElementById('nuevoPin').value;

    if (!pinAdmin || !nuevoPin) {
        alert('Por favor completa todos los campos');
        return;
    }

    if (nuevoPin.length !== 4 || !/^\d+$/.test(nuevoPin)) {
        alert('El PIN debe ser numérico y tener 4 dígitos');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/usuarios/cambiar-pin`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                idAdmin: idAdmin,
                pinAdmin: pinAdmin,
                idUsuario: idUsuario,
                nuevoPin: nuevoPin
            })
        });

        const data = await response.json();

        const mensajeDiv = document.getElementById('mensajeCambioPin');
        if (data.exitoso) {
            mensajeDiv.className = 'alert alert-success m-3';
            mensajeDiv.textContent = '✓ ' + data.mensaje;
            mensajeDiv.classList.remove('d-none');

            setTimeout(() => {
                bootstrap.Modal.getInstance(document.getElementById('modalCambiarPin')).hide();
                document.getElementById('formCambiarPin').reset();
                cargarListaUsuarios(); // Recargar lista
            }, 2000);
        } else {
            mensajeDiv.className = 'alert alert-danger m-3';
            mensajeDiv.textContent = '✗ ' + (data.mensaje || 'Error al cambiar PIN');
            mensajeDiv.classList.remove('d-none');
        }
    } catch (error) {
        alert('Error: ' + error.message);
    }
}

/**
 * Obtiene el color del badge según el rol
 */
function getBadgeColor(rol) {
    switch (rol) {
        case 'Admin':
            return 'danger';
        case 'Cajero':
            return 'warning';
        case 'Cliente':
            return 'success';
        default:
            return 'secondary';
    }
}

// ============================================================================
// FUNCIONES PARA ALERTAS DE RECUPERACIÓN PIN
// ============================================================================

/**
 * Carga y muestra las solicitudes de recuperación PIN en el panel del admin
 */
async function cargarSolicitudesRecuperacion() {
    try {
        const response = await fetch(`${API_URL}/usuarios/solicitudes-pendientes`);
        const data = await response.json();

        if (data.exitoso) {
            mostrarAlertasSolicitudes(data.solicitudes || []);
            actualizarContadorSolicitudes(data.cantidad || 0);
        } else {
            console.error('Error al cargar solicitudes:', data.mensaje);
        }
    } catch (error) {
        console.error('Error al cargar solicitudes:', error.message);
    }
}

/**
 * Muestra las solicitudes de recuperación PIN en formato de alertas
 */
function mostrarAlertasSolicitudes(solicitudes) {
    const container = document.getElementById('alertasSolicitudes');

    if (!solicitudes || solicitudes.length === 0) {
        container.innerHTML = '<p class="text-muted text-center">No hay solicitudes pendientes</p>';
        return;
    }

    let html = '<div class="list-group">';

    solicitudes.forEach(solicitud => {
        const fecha = new Date(solicitud.fechaSolicitud);
        const fechaFormato = fecha.toLocaleString('es-ES');
        const tiempoAtras = calcularTiempoAtras(fecha);

        html += `
            <div class="list-group-item list-group-item-danger">
                <div class="d-flex align-items-center justify-content-between">
                    <div>
                        <h6 class="mb-1">
                            <i class="fas fa-user-circle"></i> 
                            Usuario #${solicitud.idUsuario} - ${solicitud.nombreUsuario}
                        </h6>
                        <small class="text-muted">
                            <i class="fas fa-clock"></i> ${tiempoAtras}
                            <br>
                            ${fechaFormato}
                        </small>
                    </div>
                    <div class="btn-group btn-group-sm" role="group">
                        <button type="button" 
                                class="btn btn-warning" 
                                onclick="abrirModalCambiarPinSolicitud(${solicitud.idUsuario})">
                            <i class="fas fa-key"></i> Cambiar PIN
                        </button>
                        <button type="button" 
                                class="btn btn-success" 
                                onclick="marcarSolicitudResuelta(${solicitud.idUsuario})">
                            <i class="fas fa-check"></i> Resuelta
                        </button>
                    </div>
                </div>
            </div>
        `;
    });

    html += '</div>';
    container.innerHTML = html;
}

/**
 * Actualiza el contador de solicitudes pendientes
 */
function actualizarContadorSolicitudes(cantidad) {
    const contador = document.getElementById('contadorSolicitudes');
    if (contador) {
        contador.textContent = cantidad;
        // Mostrar el contador en rojo si hay solicitudes
        if (cantidad > 0) {
            contador.classList.remove('bg-light', 'text-danger');
            contador.classList.add('bg-danger', 'text-white', 'animate__animated', 'animate__pulse');
        } else {
            contador.classList.add('bg-light', 'text-danger');
            contador.classList.remove('bg-danger', 'text-white');
        }
    }
}

/**
 * Calcula el tiempo transcurrido desde una fecha en formato legible
 */
function calcularTiempoAtras(fecha) {
    const ahora = new Date();
    const diferencia = ahora - fecha;
    const minutos = Math.floor(diferencia / 60000);
    const horas = Math.floor(diferencia / 3600000);
    const días = Math.floor(diferencia / 86400000);

    if (minutos < 1) {
        return 'Hace menos de un minuto';
    } else if (minutos < 60) {
        return `Hace ${minutos} minuto${minutos > 1 ? 's' : ''}`;
    } else if (horas < 24) {
        return `Hace ${horas} hora${horas > 1 ? 's' : ''}`;
    } else {
        return `Hace ${días} día${días > 1 ? 's' : ''}`;
    }
}

/**
 * Abre el modal para cambiar PIN desde solicitud de recuperación
 */
function abrirModalCambiarPinSolicitud(idUsuario) {
    document.getElementById('idUsuarioCambiarPin').value = idUsuario;
    document.getElementById('pinAdminVerificacion').value = '';
    document.getElementById('nuevoPin').value = '';
    document.getElementById('formCambiarPin').reset();

    const modal = new bootstrap.Modal(document.getElementById('modalCambiarPin'));
    modal.show();
}

/**
 * Marca una solicitud de recuperación PIN como resuelta
 */
async function marcarSolicitudResuelta(idUsuario) {
    try {
        const response = await fetch(`${API_URL}/usuarios/marcar-solicitud-resuelta/${idUsuario}`, {
            method: 'POST'
        });

        const data = await response.json();

        if (data.exitoso) {
            alert('✓ Solicitud marcada como resuelta');
            // Recargar las solicitudes
            await cargarSolicitudesRecuperacion();
        } else {
            alert('Error: ' + (data.mensaje || 'No se pudo marcar como resuelta'));
        }
    } catch (error) {
        alert('Error: ' + error.message);
    }
}

/**
 * Se ejecuta cuando el admin abre la pestaña de usuarios
 * para cargar las solicitudes pendientes
 */
function mostrarTabAdmin(tab) {
    // Si es la pestaña de usuarios, cargar solicitudes
    if (tab === 'usuarios') {
        setTimeout(() => {
            cargarSolicitudesRecuperacion();
        }, 300); // Esperar a que la transición del tab termine
    }
}

// Inicializar la navbar como oculta al cargar la página
window.addEventListener('DOMContentLoaded', () => {
    document.getElementById('navbar').style.display = 'none';
});
