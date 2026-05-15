## 🚀 INICIO RÁPIDO

### 1. Verificar BD MySQL está ejecutándose
```powershell
# Desde un terminal con MySQL instalado
mysql -u root -p
```

### 2. Crear la base de datos
```sql
CREATE DATABASE db_pago_servicios;
```

### 3. Ejecutar el script SQL_INIT_SCRIPT.sql
- Localizar el archivo: `SQL_INIT_SCRIPT.sql` en la raíz del proyecto
- Ejecutar en MySQL (puedes usar MySQL Workbench o línea de comandos)

### 4. Desde Visual Studio o Terminal
```powershell
cd C:\Users\diego\source\repos\ProyectoProgra3\

# Compilar
dotnet clean
dotnet build

# Ejecutar
dotnet run
```

### 5. Acceder a la aplicación
- **Frontend**: http://localhost:5000
- **Testing API (Scalar)**: http://localhost:5000/scalar/v1

### 6. Login con usuario de prueba
- ID: 1 (Cliente con saldo Q5,000)
- PIN: 1234

---

## ✅ Todas las características implementadas:

✔️ **Cliente Panel**
- Muestra saldo actualizado en tiempo real desde BD
- Puede ver cuotas pendientes
- Puede pagar cuotas
- Puede retirar dinero
- Ve historial de pagos

✔️ **Cajero Panel**
- Busca clientes (SOLO rol "Cliente" - validado en servidor)
- Realiza depósitos
- Realiza retiros
- Bloquea búsqueda de Admin/Cajero

✔️ **Admin Panel**
- Crea nuevas cuotas
- Selecciona empresas
- Ve resumen de empresas

✔️ **Base de Datos**
- MySQL integrado con Pomelo
- Datos sincronizados
- Saldo actualizado en cada operación

✔️ **Archivos innecesarios eliminados**
- ✓ WeatherForecast.cs
- ✓ CuentaBancaria.cs
- ✓ WeatherForecastController.cs
- ✓ Documentos redundantes

---

## 📝 Notas de la Configuración

- **API URL**: http://localhost:5000/api
- **Conexión BD**: Configurada en appsettings.json
- **Usuario Admin**: root (sin contraseña por defecto en desarrollo)
- **Scalar**: Disponible en desarrollo automáticamente
