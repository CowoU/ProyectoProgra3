// wwwroot/js/app.js
const API_URL = "https://proyectobanco-e5a8acfedfccfkbg.eastus2-01.azurewebsites.net/api";
let usuarioActual = null;
let clienteSeleccionadoCajero = null;

function mostrarSeccion(seccion) {
    document.querySelectorAll('.section').forEach(s => s.classList.remove('active'));
    document.getElementById(seccion).classList.add('active');
}

async function iniciarSesion(event) {
    event.preventDefault();
    const id = parseInt(document.getElementById('usuarioId').value);
    const pin = document.getElementById('usuarioPin').value;
    const errorDiv = document.getElementById('loginError');

    try {
        const response = await fetch(`${API_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ id, pin })
        });

        const data = await response.json();

        if (data.exitoso) {
            usuarioActual = data.usuario;
            localStorage.setItem('usuarioActual', JSON.stringify(usuarioActual));

            document.getElementById('navbar').style.display = 'block';
            document.getElementById('usuarioNombre').textContent = `${usuarioActual.nombre} (${usuarioActual.rol})`;
            document.getElementById('formLogin').reset();
            errorDiv.style.display = 'none';

            if (usuarioActual.rol === 'Admin') {
                mostrarSeccion('adminSection');
                cargarPanelAdmin();
            } else if (usuarioActual.rol === 'Cajero') {
                mostrarSeccion('cajeroSection');
            } else if (usuarioActual.rol === 'Cliente') {
                mostrarSeccion('clienteSection');
                cargarPanelCliente();
            }
        } else {
            errorDiv.textContent = data.mensaje;
            errorDiv.style.display = 'block';
        }
    } catch (error) {
        console.error('Error:', error);
        errorDiv.textContent = 'Error de conexión con el servidor';
        errorDiv.style.display = 'block';
    }
}

function procesarRecuperacionPin() {
    const id = parseInt(document.getElementById('idRecuperacion').value);

    if (!id || id <= 0) {
        alert('Por favor ingrese un ID válido');
        return;
    }

    solicitarRecuperacionPin(id);
}

async function solicitarRecuperacionPin(usuarioId) {
    try {
        const response = await fetch(`${API_URL}/auth/solicitar-recuperacion-pin`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ usuarioId })
        });

        const data = await response.json();

        if (data.exitoso) {
            alert('Solicitud de recuperación enviada. Un administrador la procesará pronto.');
            document.getElementById('idRecuperacion').value = '';
            bootstrap.Modal.getInstance(document.getElementById('modalRecuperarPin')).hide();
        } else {
            alert(data.mensaje);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('Error al enviar solicitud de recuperación');
    }
}

function cerrarSesion() {
    usuarioActual = null;
    localStorage.removeItem('usuarioActual');
    document.getElementById('navbar').style.display = 'none';
    mostrarSeccion('loginSection');
    document.getElementById('formLogin').reset();
}

// ========== PANEL ADMIN ==========
async function cargarPanelAdmin() {
    await cargarUsuarios();
    await cargarSolicitudes();
    setInterval(cargarSolicitudes, 5000);
}

async function cargarUsuarios() {
    try {
        const response = await fetch(`${API_URL}/admin/usuarios`);
        const data = await response.json();

        const tbody = document.getElementById('tablaUsuarios');
        tbody.innerHTML = '';

        if (data.usuarios && data.usuarios.length > 0) {
            data.usuarios.forEach(u => {
                const row = `<tr>
                    <td>${u.id}</td>
                    <td>${u.nombre}</td>
                    <td><span class="badge bg-secondary">${u.rol}</span></td>
                    <td>Q${parseFloat(u.saldoBancario).toFixed(2)}</td>
                    <td><button class="btn btn-sm btn-warning" onclick="abrirModalRestablecer(${u.id}, '${u.nombre}')"><i class="fas fa-key"></i> PIN</button></td>
                </tr>`;
                tbody.innerHTML += row;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-4">No hay usuarios</td></tr>';
        }
    } catch (error) {
        console.error('Error:', error);
    }
}

async function cargarSolicitudes() {
    try {
        const response = await fetch(`${API_URL}/admin/solicitudes-pendientes`);
        const data = await response.json();

        const tbody = document.getElementById('tablaSolicitudes');
        tbody.innerHTML = '';

        if (data.solicitudes && data.solicitudes.length > 0) {
            const solicitudesNoProcessadas = data.solicitudes.filter(s => !s.procesada);

            if (solicitudesNoProcessadas.length > 0) {
                console.log(`⚠️ NUEVA SOLICITUD DE RECUPERACIÓN PIN: ${solicitudesNoProcessadas.length} pendiente(s)`);
            }

            data.solicitudes.forEach(s => {
                const badge = s.procesada ? 'bg-secondary' : 'bg-danger';
                const estado = s.procesada ? 'Procesada' : 'Pendiente';
                const btnDisabled = s.procesada ? 'disabled' : '';
                const row = `<tr>
                    <td>${s.id}</td>
                    <td>${s.usuario}</td>
                    <td>${new Date(s.fechaSolicitud).toLocaleString('es-ES')}</td>
                    <td><span class="badge ${badge}">${estado}</span></td>
                    <td><button class="btn btn-sm btn-success" onclick="procesarSolicitud(${s.id})" ${btnDisabled}><i class="fas fa-check"></i> Procesar</button></td>
                </tr>`;
                tbody.innerHTML += row;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-4">No hay solicitudes</td></tr>';
        }
    } catch (error) {
        console.error('Error:', error);
    }
}

async function crearUsuario(event) {
    event.preventDefault();
    const nombre = document.getElementById('nombreUsuario').value;
    const pin = document.getElementById('pinUsuario').value;
    const rol = document.getElementById('rolUsuario').value;
    const saldo = parseFloat(document.getElementById('saldoUsuario').value);

    try {
        const response = await fetch(`${API_URL}/admin/crear-usuario`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ nombre, pin, rol, saldoBancario: saldo })
        });

        const data = await response.json();
        const msgDiv = document.getElementById('mensajeCrearUsuario');

        if (data.exitoso) {
            msgDiv.className = 'alert alert-success';
            msgDiv.textContent = '✓ ' + data.mensaje;
            document.getElementById('formCrearUsuario').reset();
            await cargarUsuarios();
        } else {
            msgDiv.className = 'alert alert-danger';
            msgDiv.textContent = '✗ ' + data.mensaje;
        }
        msgDiv.style.display = 'block';
    } catch (error) {
        console.error('Error:', error);
    }
}

function abrirModalRestablecer(usuarioId, nombre) {
    const nuevoPin = prompt(`Ingrese nuevo PIN para ${nombre}:`);
    if (nuevoPin) {
        restablecerPin(usuarioId, nuevoPin);
    }
}

async function restablecerPin(usuarioId, nuevoPin) {
    try {
        const response = await fetch(`${API_URL}/admin/restablecer-pin/${usuarioId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ nuevoPin })
        });

        const data = await response.json();
        alert(data.mensaje);
        if (data.exitoso) {
            await cargarUsuarios();
        }
    } catch (error) {
        console.error('Error:', error);
    }
}

async function procesarSolicitud(solicitudId) {
    try {
        const response = await fetch(`${API_URL}/admin/procesar-solicitud/${solicitudId}`, {
            method: 'POST'
        });

        const data = await response.json();
        alert(data.mensaje);
        if (data.exitoso) {
            await cargarSolicitudes();
        }
    } catch (error) {
        console.error('Error:', error);
    }
}

// ========== PANEL CAJERO ==========
async function buscarClienteCajero() {
    const idCliente = parseInt(document.getElementById('idClienteBuscar').value);
    const errorDiv = document.getElementById('errorCajero');

    if (!idCliente || idCliente <= 0) {
        errorDiv.textContent = 'Por favor ingrese un ID válido';
        errorDiv.style.display = 'block';
        document.getElementById('datosClienteCajero').style.display = 'none';
        document.getElementById('cardDeposito').style.display = 'none';
        document.getElementById('cardRetiro').style.display = 'none';
        return;
    }

    try {
        const response = await fetch(`${API_URL}/cajero/buscar-usuario/${idCliente}`);
        const data = await response.json();

        if (data.exitoso) {
            clienteSeleccionadoCajero = data.usuario;
            document.getElementById('nombreClienteCajero').textContent = data.usuario.nombre;
            document.getElementById('saldoClienteCajero').textContent = `Q${parseFloat(data.usuario.saldoBancario).toFixed(2)}`;
            document.getElementById('datosClienteCajero').style.display = 'block';
            document.getElementById('cardDeposito').style.display = 'block';
            document.getElementById('cardRetiro').style.display = 'block';
            errorDiv.style.display = 'none';
        } else {
            errorDiv.textContent = data.mensaje;
            errorDiv.style.display = 'block';
            document.getElementById('datosClienteCajero').style.display = 'none';
            document.getElementById('cardDeposito').style.display = 'none';
            document.getElementById('cardRetiro').style.display = 'none';
        }
    } catch (error) {
        console.error('Error:', error);
        errorDiv.textContent = 'Error de conexión';
        errorDiv.style.display = 'block';
        document.getElementById('datosClienteCajero').style.display = 'none';
        document.getElementById('cardDeposito').style.display = 'none';
        document.getElementById('cardRetiro').style.display = 'none';
    }
}

async function realizarDeposito() {
    const monto = parseFloat(document.getElementById('montoDeposito').value);
    const msgDiv = document.getElementById('mensajeCajero');

    if (!clienteSeleccionadoCajero || monto <= 0) {
        msgDiv.className = 'alert alert-danger';
        msgDiv.textContent = '✗ Seleccione un cliente y monto válido';
        msgDiv.style.display = 'block';
        return;
    }

    try {
        const response = await fetch(`${API_URL}/cajero/deposito`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ usuarioId: clienteSeleccionadoCajero.id, monto })
        });

        const data = await response.json();
        msgDiv.className = data.exitoso ? 'alert alert-success' : 'alert alert-danger';
        msgDiv.textContent = (data.exitoso ? '✓ ' : '✗ ') + data.mensaje;
        msgDiv.style.display = 'block';

        if (data.exitoso) {
            document.getElementById('montoDeposito').value = '';
            await buscarClienteCajero();
        }
    } catch (error) {
        console.error('Error:', error);
    }
}

async function realizarRetiro() {
    const monto = parseFloat(document.getElementById('montoRetiro').value);
    const msgDiv = document.getElementById('mensajeCajero');

    if (!clienteSeleccionadoCajero || monto <= 0) {
        msgDiv.className = 'alert alert-danger';
        msgDiv.textContent = '✗ Seleccione un cliente y monto válido';
        msgDiv.style.display = 'block';
        return;
    }

    try {
        const response = await fetch(`${API_URL}/cajero/retiro`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ usuarioId: clienteSeleccionadoCajero.id, monto })
        });

        const data = await response.json();
        msgDiv.className = data.exitoso ? 'alert alert-success' : 'alert alert-danger';
        msgDiv.textContent = (data.exitoso ? '✓ ' : '✗ ') + data.mensaje;
        msgDiv.style.display = 'block';

        if (data.exitoso) {
            document.getElementById('montoRetiro').value = '';
            await buscarClienteCajero();
        }
    } catch (error) {
        console.error('Error:', error);
    }
}

// ========== PANEL CLIENTE ==========
async function cargarPanelCliente() {
    const saldoDiv = document.getElementById('saldoCliente');
    saldoDiv.textContent = `Q${parseFloat(usuarioActual.saldoBancario).toFixed(2)}`;
}

function retiroClienteModal() {
    const monto = parseFloat(document.getElementById('montoRetiroCliente').value);

    if (monto <= 0) {
        alert('Ingrese un monto válido');
        return;
    }

    if (monto > usuarioActual.saldoBancario) {
        alert('Saldo insuficiente');
        return;
    }

    document.getElementById('montoRetiroConfirm').textContent = monto.toFixed(2);
    new bootstrap.Modal(document.getElementById('modalConfirmRetiro')).show();
}

async function confirmarRetiroCliente() {
    const monto = parseFloat(document.getElementById('montoRetiroCliente').value);

    try {
        const response = await fetch(`${API_URL}/cliente/retiro`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ usuarioId: usuarioActual.id, monto })
        });

        const data = await response.json();
        bootstrap.Modal.getInstance(document.getElementById('modalConfirmRetiro')).hide();

        const msgDiv = document.getElementById('mensajeCliente');
        msgDiv.className = data.exitoso ? 'alert alert-success' : 'alert alert-danger';
        msgDiv.textContent = (data.exitoso ? '✓ ' : '✗ ') + data.mensaje;
        msgDiv.style.display = 'block';

        if (data.exitoso) {
            usuarioActual.saldoBancario -= monto;
            document.getElementById('saldoCliente').textContent = `Q${parseFloat(usuarioActual.saldoBancario).toFixed(2)}`;
            document.getElementById('montoRetiroCliente').value = '';
        }
    } catch (error) {
        console.error('Error:', error);
    }
}

async function pagarServicio() {
    const concepto = document.getElementById('conceptoServicio').value;
    const monto = parseFloat(document.getElementById('montoServicio').value);
    const msgDiv = document.getElementById('mensajeCliente');

    if (!concepto || monto <= 0) {
        msgDiv.className = 'alert alert-danger';
        msgDiv.textContent = '✗ Ingrese concepto y monto válidos';
        msgDiv.style.display = 'block';
        return;
    }

    if (monto > usuarioActual.saldoBancario) {
        msgDiv.className = 'alert alert-danger';
        msgDiv.textContent = '✗ Saldo insuficiente';
        msgDiv.style.display = 'block';
        return;
    }

    try {
        const response = await fetch(`${API_URL}/cliente/pagar-servicio`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ usuarioId: usuarioActual.id, monto, concepto })
        });

        const data = await response.json();
        msgDiv.className = data.exitoso ? 'alert alert-success' : 'alert alert-danger';
        msgDiv.textContent = (data.exitoso ? '✓ ' : '✗ ') + data.mensaje;
        msgDiv.style.display = 'block';

        if (data.exitoso) {
            usuarioActual.saldoBancario -= monto;
            document.getElementById('saldoCliente').textContent = `Q${parseFloat(usuarioActual.saldoBancario).toFixed(2)}`;
            document.getElementById('conceptoServicio').value = '';
            document.getElementById('montoServicio').value = '';
        }
    } catch (error) {
        console.error('Error:', error);
    }
}
