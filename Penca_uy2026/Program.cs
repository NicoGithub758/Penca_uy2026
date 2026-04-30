using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Penca_uy2026.Data;
using Penca_uy2026.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Soporte para Vistas y Controladores
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DATABASE_URL")));

// 2. Configuración de Autenticación JWT + Cookies
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

    // CLAVE: Leer el token de la Cookie para las vistas Razor
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Buscamos la cookie que seteamos en el AdminAuthController
            var accessToken = context.Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// 3. Middlewares y Enrutamiento
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// El orden aquí es vital: Autenticación antes que Autorización
app.UseAuthentication();
app.UseAuthorization();

// 4. Mapeo de Rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=AdminAuth}/{action=Login}/{id?}"); // Cambiado a AdminAuth/Login como inicio

app.MapControllers();

app.Run();