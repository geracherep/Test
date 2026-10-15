using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SeaBattle.Core.Domain.Entities;
using SeaBattle.Core.Domain.Exceptions;

namespace SeaBattle.WebApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var response = new { error = ex.Message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (DbUpdateException ex)
        {
            // Детальное логирование конфликта
            var conflictDetails = new List<string>();
            foreach (var entry in ex.Entries)
            {
                if (entry.Entity is Move move)
                {
                    conflictDetails.Add($"[Move] GameId={move.GameId}, Row={move.Row}, Col={move.Column}, Side={move.PlayerSide}, Result={move.Result}");
                }
                else
                {
                    conflictDetails.Add($"[{entry.Entity.GetType().Name}] State={entry.State}");
                }
            }
            
            _logger.LogError(ex, "Database Update Conflict! Entities: {Details}", string.Join(" | ", conflictDetails));
            
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            var response = new { 
                error = "Конфликт данных при сохранении", 
                details = conflictDetails 
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var response = new { error = "Внутренняя ошибка сервера", details = ex.Message };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
