using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SeaBattle.Core.Application;
using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Persistence;
using SeaBattle.Persistence.Seeders;
using SeaBattle.WebApi.Middleware;
using SeaBattle.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Контроллеры и базовый Swagger
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Убрали сложную настройку JWT для Swagger, чтобы не было ошибок с OpenApi

// 2. Подключаем слои (Persistence и Application)
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplication();

// 3. Регистрируем инфраструктуру WebApi
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// 3b. SignalR для онлайн-режима «человек-человек»
builder.Services.AddSignalR();
builder.Services.AddSingleton<SeaBattle.WebApi.Services.GameConnectionTracker>();
builder.Services.AddSingleton<SeaBattle.Core.Application.Interfaces.IOnlinePresenceTracker>(sp =>
    sp.GetRequiredService<SeaBattle.WebApi.Services.GameConnectionTracker>());
builder.Services.AddScoped<SeaBattle.Core.Application.Interfaces.IGameNotificationService, SeaBattle.WebApi.Services.SignalRGameNotificationService>();

// 4. Настраиваем JWT аутентификацию
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // SignalR: разрешаем токен в query string (?access_token=...)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };

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

// 5. CORS для фронта
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // AllowAnyOrigin + AllowCredentials несовместимы
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware пайплайн
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SeaBattle.WebApi.Hubs.GameHub>("/hubs/game");

// Автоматическое создание БД и сидирование админа при старте
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await AdminSeeder.SeedAsync(db);
}

app.Run();

