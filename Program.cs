using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BookMateHub.Api.Data;
using BookMateHub.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL bağlantısı yapılandırması
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(3)));

// Authentication ve Authorization yapılandırması
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true; // HTTPS zorunluluğu
        options.SaveToken = true; // Token saklama
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
        };
    });

builder.Services.AddAuthorization(); // Authorization servisi ekleme
builder.Services.AddControllers();   // Controller desteği ekleme
builder.Services.AddScoped<EmailService>(); // EmailService kaydı

// Swagger desteği (isteğe bağlı)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// HTTPS yönlendirmesini yalnızca https profilinde etkinleştir
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Geliştirme ortamında Swagger UI etkinleştir
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware yapılandırması
app.UseAuthentication(); // Kimlik doğrulama
app.UseAuthorization();  // Yetkilendirme

// Controller rotalarını haritalandırma
app.MapControllers();

// Uygulamayı çalıştır
app.Run();
