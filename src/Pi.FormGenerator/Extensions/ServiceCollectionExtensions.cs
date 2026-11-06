using Microsoft.Extensions.DependencyInjection;
using Pi.FormGenerator.Abstractions;
using Pi.FormGenerator.Services;
using Pi.FormGenerator.Storage.InMemory;

namespace Pi.FormGenerator.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPiFormGeneratorCore(this IServiceCollection services)
    {
        services.AddScoped<IFormGeneratorService, FormGeneratorService>();
        services.AddScoped<ISchemaGeneratorService, SchemaGeneratorService>();
        return services;
    }

    public static IServiceCollection AddPiFormGeneratorInMemory(this IServiceCollection services)
    {
        services.AddPiFormGeneratorCore();
        services.AddSingleton<IFormStore, InMemoryFormStore>();
        return services;
    }
}
