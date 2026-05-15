# Portal Multitenant de Pago de Servicios

## 📋 Descripción
Sistema web ASP.NET Core 10 para gestionar pagos de servicios (cementerios y condominios) con tres roles de usuario: Cliente, Cajero y Administrador de Servicios.

## 🚀 Características Principales

### Panel de Cliente
- ✅ Ver saldo actual en tiempo real desde la BD
- ✅ Visualizar cuotas pendientes y pagarlas
- ✅ Ver historial de pagos
- ✅ Retirar dinero de su cuenta

### Panel de Cajero
- ✅ Buscar clientes (solo usuarios con rol "Cliente")
- ✅ Realizar depósitos a cuentas de clientes
- ✅ Retirar dinero de cuentas de clientes
- ✅ Validación de rol en servidor

### Panel de Administrador
- ✅ Crear nuevas cuotas para usuarios
- ✅ Seleccionar empresa de la BD
- ✅ Ver resumen de empresas
- ✅ Gestionar el sistema de cuotas

## 🛠️ Tecnologías Utilizadas

- **Backend**: ASP.NET Core 10
- **BD**: MySQL 8.0
- **ORM**: Entity Framework Core con Pomelo.EntityFrameworkCore.MySql 9.0.0
- **Testing API**: Scalar 2.14.11
- **Frontend**: HTML5, Bootstrap 5.3, JavaScript (Vanilla)
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
   - Ejecutar el script de inicialización:
   ```
   Localización: SQL_INIT_SCRIPT.sql
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
   - Scalar (Testing API): http://localhost:5000/scalar/v1

## 👤 Usuarios de Prueba

| ID | Nombre | PIN | Rol |
|---|---|---|---|
| 1 | Juan Carlos García | 1234 | Cliente |
| 6 | María López | 2468 | Cajero |
| 7 | Admin Sistema | 1357 | Admin |

*Nota: Juan Carlos García tiene saldo inicial de Q5,000.00*

## 📌 Funcionalidades Implementadas

### ✅ Retiros de Dinero
- **Cliente**: Puede retirar dinero de su cuenta con validación de saldo
- **Cajero**: Puede retirar dinero de cuentas de clientes
- Ambos usan endpoints seguros con validación de saldo

### ✅ Restricción de Búsqueda (Cajero)
- El cajero solo puede buscar y operar con usuarios que tengan rol "Cliente"
- Validación en servidor para mayor seguridad
- Mensajes de error claros si intenta buscar otro rol

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
- `GET /api/pagos/historial-cuotas/{id}` - Obtener historial
- `POST /api/pagos/pagar-cuota` - Pagar una cuota
- `POST /api/pagos/retirar` - Retirar dinero (Cliente)
- `GET /api/pagos/empresas` - Obtener empresas

### Admin de Servicios
- `POST /api/adminservicios/crear-cuota` - Crear nueva cuota

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
```

## 🧪 Testing con Scalar

1. Acceder a: http://localhost:5000/scalar/v1
2. Realizar login:
   - Endpoint: `POST /api/auth/login`
   - Body: `{ "id": 1, "pin": "1234" }`
3. Usar el token/respuesta para tests posteriores

## ⚙️ Configuración de Datos

La aplicación utiliza una BD MySQL con los siguientes datos iniciales:

**Cliente:**
- ID: 1, Nombre: Juan Carlos García, PIN: 1234, Saldo: 5000.00

**Cajero:**
- ID: 6, Nombre: María López, PIN: 2468

**Admin:**
- ID: 7, Nombre: Admin Sistema, PIN: 1357

**Empresas:**
- ID 1: Cementerio El Descanso
- ID 2: Condominio Las Flores

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

## 📄 Licencia
Proyecto privado para uso educativo.

## 👨‍💻 Autor
Desarrollado para fines académicos.
