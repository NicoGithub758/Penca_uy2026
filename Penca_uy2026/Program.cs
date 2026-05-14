using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Penca_uy2026.Data;
using Penca_uy2026.Services;
using Penca_uy2026.Interfaces;
using Penca_uy2026.Middleware;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------
// 1. REGISTRO DE SERVICIOS (Dependency Injection)
// -----------------------------------------------------------

// Acceso al contexto HTTP (necesario para que el TenantService lea la URL)
builder.Services.AddHttpContextAccessor();

// Soporte para Controladores con Vistas (Razor) y API
builder.Services.AddControllersWithViews();

// Configuración del DbContext (SQL Server)
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registro de Servicios de Lógica de Negocio
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<AuthService>();

// -----------------------------------------------------------
// 2. CONFIGURACIÓN DE SEGURIDAD (JWT + COOKIES)
// -----------------------------------------------------------

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };

    // Permite que las vistas Razor usen el Token guardado en la Cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddHttpClient();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MobileAuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UsuarioAuthService>();

// Buscar en la config las URLs permitidas, si no encontró nada se asume ambiente de desarrollo.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string>()?.Split(',') ?? new[] { "http://localhost:5173" };
// Autorizar a la aplicación de React para evitar error de CORS.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
});

var app = builder.Build();

// -----------------------------------------------------------
// 3. PIPELINE DE MIDDLEWARES (El orden es vital)
// -----------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowReactApp"); // No mover de lugar, el orden es importante.

// MIDDLEWARE MULTI-TENANT: Debe ir después de Routing pero antes de Auth
// Este identifica qué sitio (URL) está accediendo para filtrar la DB
app.UseMiddleware<TenantMiddleware>();

// El orden aquí es vital: Autenticación antes que Autorización
app.UseAuthentication();
app.UseAuthorization();

// -----------------------------------------------------------
// 4. RUTAS Y MIGRACIONES
// -----------------------------------------------------------

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=AdminAuth}/{action=Login}/{id?}");

app.MapControllers();

// Ejecución automática de migraciones al iniciar (Railway/Producción)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<MyDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al migrar la base de datos.");
    }
}

app.Run();