using System.Text;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Penca_uy2026.Data;
using Penca_uy2026.Services;
using Penca_uy2026.Interfaces;
using Penca_uy2026.Middleware;
using Microsoft.AspNetCore.DataProtection;

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
builder.Services.AddDataProtection()
    .SetApplicationName("PencaUy2026")
    .PersistKeysToFileSystem(new DirectoryInfo(@"/home/app/.aspnet/DataProtection-Keys"));
builder.Services.AddScoped<ApiFootballService>();
builder.Services.AddScoped<ParametrosSistemaService>();
builder.Services.AddScoped<ActualizarResultadosService>();
builder.Services.AddHostedService<ActualizarResultadosBackgroundService>();
builder.Services.AddHostedService<RecordatoriosBackgroundService>();
builder.Services.AddSignalR();

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
            var path = context.HttpContext.Request.Path;
        
            // SignalR: priorizar el token que manda el frontend por query string
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/chat") || path.StartsWithSegments("/hubs/penca")))
            {
                context.Token = accessToken;
                return Task.CompletedTask;
            }

            // 2. Razor / web con cookie
            var tokenCookie = context.Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(tokenCookie))
            {
                context.Token = tokenCookie;
                return Task.CompletedTask;
            }
        
            // 3. APIs/mobile por header Authorization
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                context.Token = authHeader.Substring("Bearer ".Length).Trim();
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminSitio", policy => policy.RequireRole("AdminSitio"));
    options.AddPolicy("Jugador", policy => policy.RequireRole("Jugador"));
});

builder.Services.AddHttpClient();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MobileAuthService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UsuarioAuthService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<PreferenciasService>();
builder.Services.AddScoped<SitioService>();
builder.Services.AddScoped<InvitacionService>();
builder.Services.AddScoped<PayPalService>();
builder.Services.AddScoped<FirebaseNotificationService>();
builder.Services.AddScoped<ProcesadorResultadosService>();
builder.Services.AddScoped<PosicionesService>();

// -----------------------------------------------------------
// Configuración de Cloudinary
// Se lee la configuración desde appsettings.json o user-secrets
// y se registra la instancia de Cloudinary como Singleton.
// -----------------------------------------------------------
var cloudinaryAccount = new Account(
    builder.Configuration["Cloudinary:CloudName"],
    builder.Configuration["Cloudinary:ApiKey"],
    builder.Configuration["Cloudinary:ApiSecret"]
);
var cloudinary = new Cloudinary(cloudinaryAccount);
builder.Services.AddSingleton(cloudinary);

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

// Registrar el servicio de Email
builder.Services.AddScoped<IEmailServicio, EmailServicio>();

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




// El orden aquí es vital: Autenticación antes que Autorización
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

// -----------------------------------------------------------
// 4. RUTAS Y MIGRACIONES
// -----------------------------------------------------------

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=AdminAuth}/{action=Login}/{id?}");

app.MapControllers();
app.MapHub<Penca_uy2026.Hubs.ChatHub>("/hubs/chat");
app.MapHub<Penca_uy2026.Hubs.PencaHub>("/hubs/penca");
// Ejecución automática de migraciones al iniciar (Railway/Producción)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<MyDbContext>();
        // db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al migrar la base de datos.");
    }
}



app.Run();
