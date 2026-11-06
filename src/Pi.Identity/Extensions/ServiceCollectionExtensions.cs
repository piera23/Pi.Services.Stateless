using Microsoft.Extensions.DependencyInjection;
using Pi.Identity.Abstractions;
using Pi.Identity.Services;
using Pi.Identity.Storage.InMemory;

namespace Pi.Identity.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPiIdentityCore(this IServiceCollection services)
    {
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IActorResolver, ActorResolver>();
        return services;
    }

    public static IServiceCollection AddPiIdentityInMemory(this IServiceCollection services)
    {
        services.AddPiIdentityCore();
        services.AddSingleton<IUserStore, InMemoryUserStore>();
        return services;
    }
}
