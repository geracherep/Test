using Microsoft.Extensions.DependencyInjection;
using SeaBattle.Core.Application.AI;
using SeaBattle.Core.Application.Interfaces;
using SeaBattle.Core.Application.Services;

namespace SeaBattle.Core.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGameService, GameService>(); // Мы его добавим чуть ниже, пока закомментируй если будет ошибка, но лучше добавить заглушку
        services.AddScoped<IShipPlacementService, ShipPlacementService>();
        services.AddScoped<IMoveService, MoveService>();
        services.AddScoped<IExperienceService, ExperienceService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IAiStrategyFactory, AiStrategyFactory>();

        return services;
    }
}
