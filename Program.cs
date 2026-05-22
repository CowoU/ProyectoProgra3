using Microsoft.EntityFrameworkCore;
using ProyectoProgra3.Data;
using ProyectoProgra3.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 1. CONFIGURACIÓN DE SERVICIOS (Antes de builder.Build())
// ============================================================================

// Configurar la cadena de conexión a MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Registrar tus servicios de la lógica de negocio
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<PagoService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configurar Políticas de CORS (Centralizadas en un solo bloque)
builder.Services.AddCors(options =>
{
    // Política 1: Permitir todo (Ideal para quitar el error de conexión rápido en Azure)
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Política 2: Restringido (Más segura para cuando ya esté listo en producción)
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.WithOrigins("https://tu-frontend-en-azure.azurewebsites.net") // <-- Coloca aquí la URL de tu frontend de Azure si está separado
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// INSTANCIA ÚNICA DE LA APP (Solo se debe compilar una vez)
var app = builder.Build();

// ============================================================================
// 2. CONFIGURACIÓN DEL PIPELINE DE MIDDLEWARES (El orden es estricto)
// ============================================================================

// Configurar documentación y testing (Solo se activa en Entorno de Desarrollo)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Portal de Pagos - API Scalar")
               .WithTheme(ScalarTheme.BluePlanet)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();

// Servir archivos del Frontend (si los tienes dentro de wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

// ACTIVACIÓN DE CORS: Debe ir estrictamente después de UseRouting y antes de los controladores.
// Nota: Usamos "AllowAll" para asegurar que tu Web App de Azure no rechace los Preflights (peticiones de login).
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();