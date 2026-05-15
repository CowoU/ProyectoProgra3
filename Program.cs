using Microsoft.EntityFrameworkCore;
using ProyectoProgra3.Data;
using ProyectoProgra3.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar la cadena de conexión a MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Registrar tus servicios
builder.Services.AddScoped<DataService>(); // IMPORTANTE: AddScoped para base de datos
builder.Services.AddScoped<PagoService>();

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

var app = builder.Build();

// 3. Configurar documentación y testing
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Agregar Scalar para documentación interactiva y testing de APIs
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Portal de Pagos - API Scalar")
               .WithTheme(ScalarTheme.BluePlanet)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// 4. Configurar archivos estáticos (Frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapControllers();

app.Run();