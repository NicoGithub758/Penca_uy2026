using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Penca_uy2026.Data;
using Penca_uy2026.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. IMPORTANTE: Cambiar AddControllers por AddControllersWithViews
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DATABASE_URL")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    });

builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// 2. IMPORTANTE: Habilitar archivos estáticos (para CSS/JS) y enrutamiento estándar
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 3. IMPORTANTE: Mapear la ruta predeterminada para encontrar los Controllers de tipo View
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // Mantiene soporte para tus APIs actuales

app.Run();