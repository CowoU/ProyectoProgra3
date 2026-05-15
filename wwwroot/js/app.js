// ============================================================================
// APLICACIÓN FRONTEND - PORTAL DE PAGOS DE SERVICIOS
// ============================================================================

// VARIABLES GLOBALES
let usuarioActual = null;
let cuotaACtualizar = null;

// URL BASE DE LA API
const API_URL = 'http://localhost:5019/api';

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
        `<i class="fas fa-user"></i> ${usuarioActual.nombre} <span class="badge bg-info">${rol}</span>`;

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
        document.getElementById('clienteId').textContent = usuarioActual.id;

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
                    <th>Mes</th>
                    <th>Monto</th>
                    <th>Acción</th>
                </tr>
            </thead>
            <tbody>
    `;

    cuotas.forEach(cuota => {
        html += `
            <tr>
                <td><strong>${cuota.id}</strong></td>
                <td>${cuota.mes}</td>
                <td><strong>Q${parseFloat(cuota.monto).toFixed(2)}</strong></td>
                <td>
                    <button class="btn btn-sm btn-primary" 
                            onclick="abrirModalPago(${cuota.id}, '${cuota.mes}', ${cuota.monto})">
                        <i class="fas fa-money-bill"></i> Pagar
                    </button>
                </td>
            </tr>
        `;
    });

    html += `</tbody></table>`;
    tablaCuotasDiv.innerHTML = html;
}

function abrirModalPago(idCuota, mes, monto) {
    cuotaACtualizar = idCuota;
    document.getElementById('detalleMes').textContent = mes;
    document.getElementById('detalleMonto').textContent = parseFloat(monto).toFixed(2);

    const modal = new bootstrap.Modal(document.getElementById('modalConfirmarPago'));
    modal.show();
}

async function confirmarPago() {
    try {
        const response = await fetch(`${API_URL}/pagos/pagar-cuota`, {
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
                    <th>Mes</th>
                    <th>Monto</th>
                    <th>Estado</th>
                </tr>
            </thead>
            <tbody>
    `;

    cuotas.forEach(cuota => {
        const badgeClase = cuota.estado === 'Pagado' ? 'badge-success' : 'badge-warning';
        const estadoTexto = cuota.estado === 'Pagado' ? '✓ Pagado' : '⏳ Pendiente';

        html += `
            <tr>
                <td><strong>${cuota.mes}</strong></td>
                <td>Q${parseFloat(cuota.monto).toFixed(2)}</td>
                <td><span class="badge ${badgeClase}">${estadoTexto}</span></td>
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
            document.getElementById('idCuentaBancaria').textContent = data.usuario.id;
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
    } catch (error) {
        console.error('Error al cargar panel admin:', error);
    }
}

async function cargarEmpresas() {
    const empresas = [
        { id: 1, nombre: 'Cementerio El Descanso' },
        { id: 2, nombre: 'Condominio Las Flores' }
    ];

    const select = document.getElementById('idEmpresaAdmin');
    empresas.forEach(empresa => {
        const option = document.createElement('option');
        option.value = empresa.id;
        option.textContent = empresa.nombre;
        select.appendChild(option);
    });
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
    // Placeholder - se implementaría si hay endpoint para obtener todas las cuotas
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
                `✓ Cuota de Q${monto.toFixed(2)} creada exitosamente para ${mes}`);
            document.getElementById('formCrearCuota').reset();
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

// Inicializar la navbar como oculta al cargar la página
window.addEventListener('DOMContentLoaded', () => {
    document.getElementById('navbar').style.display = 'none';
});
