# Portal Multitenant de Pago de Servicios

## 📋 Descripción
Sistema web ASP.NET Core 10 para gestionar pagos de servicios (cementerios y condominios) con tres roles de usuario: Cliente, Cajero y Administrador de Servicios. Incluye funcionalidades avanzadas como cálculo de mora, gestión de usuarios, recuperación de PIN y alertas administrativas.

## 🚀 Características Principales

### Panel de Cliente
- ✅ Ver saldo actual en tiempo real desde la BD
- ✅ Visualizar cuotas pendientes y pagarlas
- ✅ Cálculo automático de mora por cuotas vencidas
- ✅ Ver historial completo de pagos (pagadas y pendientes)
- ✅ Retirar dinero de su cuenta
- ✅ Solicitar recuperación de PIN si lo olvida

### Panel de Cajero
- ✅ Buscar clientes (solo usuarios con rol "Cliente")
- ✅ Realizar depósitos a cuentas de clientes
- ✅ Retirar dinero de cuentas de clientes
- ✅ Validación de rol en servidor
- ✅ Ver saldo actualizado en tiempo real

### Panel de Administrador
- ✅ **Panel Principal**: Crear cuotas, ver resumen de empresas e historial del sistema
- ✅ **Crear Usuarios**: Añadir nuevos usuarios al sistema (clientes, cajeros, admins)
- ✅ **Lista de Usuarios**: Gestionar usuarios, cambiar PINs, ver solicitudes de recuperación
- ✅ **Alertas de Recuperación**: Notificaciones visuales cuando un cliente solicita recuperar PIN (mostrar ID y nombre)
- ✅ Administración completa de cuotas y empresa

## 🛠️ Tecnologías Utilizadas

- **Backend**: ASP.NET Core 10 (.NET 10)
- **BD**: MySQL 8.0
- **ORM**: Entity Framework Core con Pomelo.EntityFrameworkCore.MySql 9.0.0
- **Testing API**: Scalar 2.14.11
- **Frontend**: HTML5, Bootstrap 5.3, JavaScript (Vanilla)
- **Hosting**: Azure Web App Service
- **Autenticación**: ID + PIN

## 📦 Instalación

### Requisitos Previos
- .NET 10 SDK
- MySQL 8.0+
- Visual Studio 2026 o VS Code

### Pasos de Instalación

1. **Clonar/Abrir el proyecto**
   ```powershell
   cd C:\Users\diego\source\repos\ProyectoProgra3\
   ```

2. **Configurar la BD**
   - Crear la BD MySQL:
   ```sql
   CREATE DATABASE db_pago_servicios;
   ```
   - Crear tabla de solicitudes de recuperación PIN:
   ```sql
   CREATE TABLE `solicitudes_recuperacion_pin` (
	 `id` INT NOT NULL AUTO_INCREMENT,
	 `id_usuario` INT NOT NULL,
	 `nombre_usuario` VARCHAR(200) NOT NULL,
	 `fecha_solicitud` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
	 `procesada` TINYINT(1) NOT NULL DEFAULT 0,
	 PRIMARY KEY (`id`),
	 INDEX `ix_solicitudes_id_usuario` (`id_usuario`)
   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
   ```

3. **Configurar la conexión en appsettings.json**
   ```json
   "ConnectionStrings": {
	 "DefaultConnection": "Server=localhost;Database=db_pago_servicios;User=root;Password=Diego_1090;"
   }
   ```

4. **Compilar el proyecto**
   ```powershell
   dotnet clean
   dotnet build
   ```

5. **Ejecutar la aplicación**
   ```powershell
   dotnet run
   ```

6. **Acceder a la aplicación**
   - Frontend: http://localhost:5000
   - API: http://localhost:5019/api
   - Scalar (Testing API): http://localhost:5000/scalar/v1

## 👤 Usuarios de Prueba

| ID | Nombre | PIN | Rol |
|---|---|---|---|
| 1 | Juan Carlos García | 1234 | Cliente |
| 6 | María López | 2468 | Cajero |
| 7 | Admin Sistema | 1357 | Admin |

*Nota: Juan Carlos García tiene saldo inicial de Q5,000.00*

## 📌 Funcionalidades Implementadas

### ✅ Cálculo de Mora
- Se calcula automáticamente cuando una cuota vence
- Porcentaje: 5% de la cuota por mes vencido
- Se suma al monto total a pagar
- Se registra en la BD al procesar el pago

### ✅ Gestión de Usuarios
- Admin puede crear nuevos usuarios (clientes, cajeros, admins)
- Ver listado completo de usuarios con filtros
- Cambiar PIN de usuarios (requiere PIN del admin para verificación)
- Ver información detallada de cada usuario

### ✅ Recuperación de PIN
- Cliente puede solicitar recuperación si olvida su PIN desde login
- Genera alerta en el panel del admin
- Admin ve ID y nombre del usuario que solicita
- Admin puede marcar como resuelta después de cambiar el PIN
- Solicitudes persistentes en BD (sobreviven reinicio del servidor)

### ✅ Alertas Visuales
- Panel admin muestra alertas en rojo cuando hay solicitudes de recuperación
- Badge muestra número de solicitudes pendientes
- Lista detallada con ID, nombre y fecha de cada solicitud
- Botones para cambiar PIN o marcar como resuelta

### ✅ Retiros de Dinero
- **Cliente**: Puede retirar dinero de su cuenta con validación de saldo
- **Cajero**: Puede retirar dinero de cuentas de clientes
- Ambos usan endpoints seguros con validación de saldo

### ✅ Saldo en Tiempo Real
- El saldo se obtiene directamente de la BD en cada carga del panel
- Se actualiza automáticamente después de depósitos, retiros y pagos
- La interfaz siempre muestra datos actuales

## 🔌 Endpoints de la API

### Autenticación
- `POST /api/auth/login` - Iniciar sesión

### Cajero
- `GET /api/cajero/buscar-usuario/{id}` - Buscar usuario (solo Clientes)
- `POST /api/cajero/depositar` - Realizar depósito
- `POST /api/cajero/retirar` - Realizar retiro

### Pagos
- `GET /api/pagos/cuotas-pendientes/{id}` - Obtener cuotas pendientes
- `GET /api/pagos/historial-cuotas/{id}` - Obtener historial completo
- `POST /api/pagos/pagar-cuota` - Pagar una cuota (calcula mora automáticamente)
- `POST /api/pagos/retirar` - Retirar dinero (Cliente)
- `GET /api/pagos/empresas` - Obtener empresas

### Admin de Servicios
- `POST /api/adminservicios/crear-cuota` - Crear nueva cuota
- `POST /api/adminservicios/borrar-cuota/{id}` - Eliminar cuota

### Usuarios
- `GET /api/usuarios/todos` - Obtener listado de usuarios
- `GET /api/usuarios/filtrar` - Filtrar usuarios por rol
- `GET /api/usuarios/obtener-detalles/{id}` - Obtener detalles de usuario (requiere PIN admin)
- `POST /api/usuarios/crear` - Crear nuevo usuario (admin)
- `POST /api/usuarios/cambiar-pin` - Cambiar PIN de usuario
- `POST /api/usuarios/solicitar-recuperacion-pin` - Solicitar recuperación de PIN
- `GET /api/usuarios/solicitudes-pendientes` - Obtener solicitudes sin procesar
- `POST /api/usuarios/marcar-solicitud-resuelta/{id}` - Marcar solicitud como procesada

## 🗄️ Estructura de Base de Datos

### Tabla: Usuarios
```sql
- Id (INT, PK)
- Nombre (VARCHAR)
- Pin (VARCHAR)
- Rol (VARCHAR) - 'Cliente', 'Cajero', 'Admin'
- SaldoBancario (DECIMAL)
```

### Tabla: Empresas
```sql
- Id (INT, PK)
- Nombre (VARCHAR)
- SaldoAcumulado (DECIMAL)
```

### Tabla: Cuotas
```sql
- Id (INT, PK)
- IdUsuario (INT, FK)
- IdEmpresa (INT, FK)
- Mes (VARCHAR)
- Monto (DECIMAL)
- Estado (VARCHAR) - 'Pendiente', 'Pagado'
- FechaVencimiento (DATETIME)
- Mora (DECIMAL)
```

### Tabla: SolicitudesRecuperacionPin
```sql
- Id (INT, PK)
- IdUsuario (INT)
- NombreUsuario (VARCHAR)
- FechaSolicitud (DATETIME)
- Procesada (TINYINT)
```

## 🧪 Testing con Scalar

1. Acceder a: http://localhost:5000/scalar/v1
2. Realizar login:
   - Endpoint: `POST /api/auth/login`
   - Body: `{ "id": 1, "pin": "1234" }`
3. Usar el token/respuesta para tests posteriores

## 🐛 Solución de Problemas

### El saldo muestra 0
- Verificar que la BD está ejecutándose
- Confirmar que el usuario existe en la tabla Usuarios
- Revisar la conexión en appsettings.json

### Cajero busca usuario Admin/Cajero
- El servidor rechaza la búsqueda automáticamente
- Solo permite búsquedas de usuarios con rol "Cliente"

### Error de conexión a la BD
- Verificar que MySQL está ejecutándose
- Confirmar usuario y contraseña en appsettings.json
- Verificar que la BD `db_pago_servicios` existe

### Alertas de recuperación PIN no aparecen
- Verificar que la tabla `solicitudes_recuperacion_pin` existe en la BD
- Login como admin (ID: 7, PIN: 1357)
- Ir a pestaña "Lista de Usuarios"
- Solicitudes deben aparecer automáticamente después de que cliente solicite recuperación

### Mora no se calcula
- Verificar que la cuota tiene FechaVencimiento configurada
- Comprobar que fecha actual es posterior a FechaVencimiento
- Mora = (Monto * 0.05 * meses_vencidos)

## 📋 Estructura del Proyecto

```
ProyectoProgra3/
├── Controllers/
│   ├── AuthController.cs
│   ├── AdminServiciosController.cs
│   ├── CajeroController.cs
│   ├── PagosController.cs
│   └── UsuariosController.cs
├── Models/
│   ├── Usuario.cs
│   ├── Empresa.cs
│   ├── Cuota.cs
│   └── SolicitudRecuperacionPin.cs
├── Services/
│   └── DataService.cs
├── Data/
│   └── ApplicationDbContext.cs
├── wwwroot/
│   ├── index.html
│   ├── css/
│   │   └── estilos.css
│   └── js/
│       └── app.js
├── appsettings.json
└── Program.cs
```

## 🚀 Despliegue en Azure

La aplicación está desplegada en Azure Web App Service. Para actualizar:

1. Compilar en Release: `dotnet publish -c Release`
2. Subir archivos publicados a Azure App Service
3. Asegurar que variables de entorno de BD estén configuradas

URL en producción: https://proyectobanco-e5a8acfedfccfkbg.eastus2-01.azurewebsites.net/

## 📄 Licencia
Proyecto privado para uso educativo.

## 👨‍💻 Autor
Desarrollado para fines académicos.
