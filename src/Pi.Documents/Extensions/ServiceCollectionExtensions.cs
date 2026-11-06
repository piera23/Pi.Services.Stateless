using Microsoft.Extensions.DependencyInjection;
using Pi.Documents.Abstractions;
using Pi.Documents.Services;
using Pi.Documents.Storage.InMemory;

namespace Pi.Documents.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPiDocumentsCore(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        return services;
    }

    public static IServiceCollection AddPiDocumentsInMemory(this IServiceCollection services)
    {
        services.AddPiDocumentsCore();
        services.AddSingleton<IDocumentStore, InMemoryDocumentStore>();
        services.AddSingleton<IFileStore, InMemoryFileStore>();
        return services;
    }
}
