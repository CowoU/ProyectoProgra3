# 🚀 GUÍA DE DESPLIEGUE Y TESTING - PORTAL DE PAGOS

## 📋 Tabla de Contenidos

1. [Testing Local](#testing-local)
2. [Despliegue en IIS](#despliegue-en-iis)
3. [Despliegue en Azure](#despliegue-en-azure)
4. [Troubleshooting](#troubleshooting)

---

## 🧪 Testing Local

### Verificación Rápida

```bash
# 1. Restaurar dependencias
dotnet restore

# 2. Compilar proyecto
dotnet build

# 3. Ejecutar la aplicación
dotnet run

# 4. Acceder a:
# Frontend: http://localhost:5000
# API: http://localhost:5000/api
```

### Prueba de Endpoints con cURL

```bash
# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"id\": 1, \"pin\": \"1234\"}"

# Obtener cuotas pendientes
curl -X GET http://localhost:5000/api/pagos/cuotas-pendientes/1

# Pagar cuota
curl -X POST http://localhost:5000/api/pagos/pagar-cuota \
  -H "Content-Type: application/json" \
  -d "{\"idUsuario\": 1, \"idCuota\": 1}"
```

### Prueba con Postman

1. Descargar Postman desde https://www.postman.com
2. Importar colección (crear manualmente o importar JSON):

```json
{
  "info": {
    "name": "Portal Pagos API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Login",
      "request": {
        "method": "POST",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "body": {"raw": "{\"id\": 1, \"pin\": \"1234\"}"},
        "url": {"raw": "http://localhost:5000/api/auth/login"}
      }
    },
    {
      "name": "Cuotas Pendientes",
      "request": {
        "method": "GET",
        "url": {"raw": "http://localhost:5000/api/pagos/cuotas-pendientes/1"}
      }
    },
    {
      "name": "Pagar Cuota",
      "request": {
        "method": "POST",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "body": {"raw": "{\"idUsuario\": 1, \"idCuota\": 1}"},
        "url": {"raw": "http://localhost:5000/api/pagos/pagar-cuota"}
      }
    }
  ]
}
```

### Casos de Prueba

| Caso | Usuario | PIN | Resultado Esperado |
|------|---------|-----|-------------------|
| Login exitoso | 1 | 1234 | ✓ Acceso como cliente |
| Login fallido | 1 | 9999 | ✗ Error "ID o PIN inválidos" |
| Cajero | 6 | 2468 | ✓ Acceso panel cajero |
| Admin | 7 | 1357 | ✓ Acceso panel admin |
| Pago exitoso | 1 | - | ✓ Cuota pagada, saldo actualizado |
| Saldo insuficiente | 4 | - | ✗ Error "Saldo Insuficiente" |
| Crear cuota | 7 | - | ✓ Cuota creada |

---

## 🖥️ Despliegue en IIS (Windows Server)

### Prerequisitos

- Windows Server 2016 o superior
- IIS 8.0 o superior
- .NET Runtime (Hosting Bundle)

### Pasos

**1. Instalar .NET Hosting Bundle**

```
Descargar desde: https://dotnet.microsoft.com/en-us/download/dotnet/latest/runtime
Seleccionar "ASP.NET Core Runtime" → "Hosting Bundle"
Ejecutar instalador
Reiniciar servidor
```

**2. Publicar la aplicación**

```bash
dotnet publish -c Release -o ./publish
```

**3. Crear sitio en IIS**

- Abrir IIS Manager
- Click derecho en "Sites" → "Add Website"
- Configurar:
  - Site name: `PortalPagos`
  - Physical path: `C:\inetpub\wwwroot\PortalPagos\publish`
  - Binding: `http` / Puerto `80`

**4. Configurar Application Pool**

- Pool name: `.NET AppPool v10.0` (o similar)
- .NET CLR version: `No Managed Code`
- Managed pipeline mode: `Integrated`

**5. Copiar archivos publicados**

```bash
# Desde máquina local
xcopy publish C:\inetpub\wwwroot\PortalPagos\publish /E /I /Y
```

**6. Verificar permisos**

```bash
# En PowerShell (como Admin)
icacls "C:\inetpub\wwwroot\PortalPagos" /grant "IIS AppPool\PortalPagos":(OI)(CI)F /T
```

**7. Acceder a la aplicación**

```
http://<nombre-servidor>/
```

---

## ☁️ Despliegue en Azure

### Opción 1: Azure App Service

**1. Crear App Service**

```bash
# Conectar a Azure
az login

# Crear grupo de recursos
az group create --name PortalPagosRG --location "East US"

# Crear plan App Service
az appservice plan create --name PortalPagosPlan \
  --resource-group PortalPagosRG --sku B2

# Crear App Service
az webapp create --resource-group PortalPagosRG \
  --plan PortalPagosPlan --name PortalPagosApp \
  --runtime "DOTNET|10.0"
```

**2. Publicar desde Visual Studio**

- Click derecho en proyecto → "Publish"
- Seleccionar "Azure" → "Azure App Service"
- Configurar:
  - Resource Group: `PortalPagosRG`
  - App Service: `PortalPagosApp`
- Click "Publish"

**3. Configurar base de datos**

```bash
# Crear SQL Server
az sql server create --name PortalPagosSrv \
  --resource-group PortalPagosRG \
  --admin-user adminuser \
  --admin-password <password>

# Crear base de datos
az sql db create --resource-group PortalPagosRG \
  --server PortalPagosSrv --name PortalPagosDB
```

**4. Configurar Connection String**

```bash
az webapp config connection-string set \
  --resource-group PortalPagosRG \
  --name PortalPagosApp \
  --connection-string-type SQLServer \
  --settings DefaultConnection="Server=tcp:PortalPagosSrv.database.windows.net,1433;..."
```

### Opción 2: Azure Container Instances (Docker)

**1. Crear Dockerfile**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ProyectoProgra3.csproj", "."]
RUN dotnet restore "ProyectoProgra3.csproj"
COPY . .
RUN dotnet build "ProyectoProgra3.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProyectoProgra3.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProyectoProgra3.dll"]
```

**2. Construir imagen Docker**

```bash
docker build -t portalpagos:latest .
```

**3. Ejecutar localmente para probar**

```bash
docker run -d -p 5000:5000 portalpagos:latest
```

**4. Subir a Azure Container Registry**

```bash
# Crear registry
az acr create --resource-group PortalPagosRG \
  --name portalpagosacr --sku Basic

# Login en registry
az acr login --name portalpagosacr

# Tag de imagen
docker tag portalpagos:latest portalpagosacr.azurecr.io/portalpagos:latest

# Push a Azure
docker push portalpagosacr.azurecr.io/portalpagos:latest

# Crear container instance
az container create --resource-group PortalPagosRG \
  --name portalpagos-container \
  --image portalpagosacr.azurecr.io/portalpagos:latest \
  --cpu 1 --memory 1 \
  --registry-login-server portalpagosacr.azurecr.io \
  --registry-username <username> \
  --registry-password <password> \
  --ip-address Public \
  --ports 5000
```

---

## 🔧 Troubleshooting

### Problema: Puerto 5000 ya está en uso

```bash
# Windows - Buscar proceso usando puerto 5000
netstat -ano | findstr :5000

# Matar proceso
taskkill /PID <PID> /F

# O cambiar puerto en launchSettings.json
```

### Problema: Error de CORS

```
Access to XMLHttpRequest blocked by CORS policy
```

**Solución:**

```csharp
// En Program.cs, asegurar que CORS está habilitado
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

app.UseCors("AllowAll");
```

### Problema: Base de datos no conecta

```
Connection refused / Server not found
```

**Solución:**

```bash
# Verificar SQL Server está corriendo
sqlcmd -S . -U sa -P <password>

# Verificar connection string
# En appsettings.json

# Verificar firewall permite puerto 1433
netsh advfirewall firewall add rule name="SQL Server" dir=in action=allow protocol=tcp localport=1433
```

### Problema: Cambios no se guardan en BD

**Causa:** Olvidó llamar `SaveChanges()`

```csharp
// ❌ Incorrecto
var usuario = context.Usuarios.Find(1);
usuario.Nombre = "Nuevo Nombre";
// Los cambios NO se guardan

// ✓ Correcto
var usuario = context.Usuarios.Find(1);
usuario.Nombre = "Nuevo Nombre";
context.SaveChanges(); // ← Importante
```

### Problema: Error 500 en la API

```
Internal Server Error
```

**Solución:**

1. Revisar logs en `wwwroot/logs/` o consola de Visual Studio
2. Verificar que todos los servicios están registrados en `Program.cs`
3. Usar try-catch para capturar excepciones

```csharp
try
{
    // Código que falla
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
    throw;
}
```

### Problema: Migraciones no se aplican

```bash
# Verificar migraciones pendientes
dotnet ef migrations list

# Aplicar migraciones
dotnet ef database update

# Crear nueva migración
dotnet ef migrations add <NombreMigracion>

# Ver script SQL
dotnet ef migrations script
```

---

## 📊 Monitoreo en Producción

### Azure Monitor

```bash
# Ver logs en tiempo real
az webapp log tail --resource-group PortalPagosRG \
  --name PortalPagosApp

# Descargar logs
az webapp log download --resource-group PortalPagosRG \
  --name PortalPagosApp --log-file logs.zip
```

### Application Insights

```bash
# Crear recurso
az monitor app-insights component create \
  --app PortalPagosInsights \
  --location "East US" \
  --resource-group PortalPagosRG

# Conectar a App Service
az webapp config appsettings set \
  --resource-group PortalPagosRG \
  --name PortalPagosApp \
  --settings APPINSIGHTS_INSTRUMENTATIONKEY=<key>
```

---

## ✅ Checklist Pre-Producción

- [ ] Código revisado y aprobado
- [ ] Tests unitarios pasando
- [ ] Tests de integración pasando
- [ ] Datos sensitivos en Key Vault (no en código)
- [ ] HTTPS habilitado
- [ ] CORS configurado correctamente
- [ ] Rate limiting implementado
- [ ] Logging habilitado
- [ ] Backup de BD configurado
- [ ] Disaster recovery plan documentado
- [ ] Load testing realizado
- [ ] Security scan completado
- [ ] Performance baseline establecido
- [ ] Runbook de operaciones creado

---

## 📞 Contacto y Soporte

Para preguntas o problemas:
- Revisar logs
- Consultar documentación en README.md
- Revisar comments en el código
- Crear issue en el repositorio

---

**Última actualización:** Diciembre 2024
