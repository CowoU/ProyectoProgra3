using Microsoft.EntityFrameworkCore;
using ProyectoProgra3.Data;
using ProyectoProgra3.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<BancoService>();
builder.Services.AddHttpClient();

builder.Services.AddScoped<
    CementerioService
>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
// INSTANCIA ÚNICA DE LA APP (Solo se debe compilar una vez)
var app = builder.Build();

// Agregar middleware adicional para asegurar encabezados CORS en respuestas,
// útil en entornos donde un proxy o la plataforma de despliegue pueda eliminarlos.
app.Use(async (context, next) =>
{
    if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
        context.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });

    if (string.Equals(context.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
        context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type,Authorization";
        context.Response.StatusCode = 204;
        await context.Response.CompleteAsync();
        return;
    }

    await next();
});

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

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();