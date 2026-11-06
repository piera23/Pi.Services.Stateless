using Microsoft.Extensions.DependencyInjection;
using Pi.Workflow.Abstractions;
using Pi.Workflow.Engine;
using Pi.Workflow.Storage.InMemory;

namespace Pi.Workflow.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPiWorkflowCore(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        return services;
    }

    public static IServiceCollection AddPiWorkflowInMemory(this IServiceCollection services)
    {
        services.AddPiWorkflowCore();
        services.AddSingleton<IWorkflowStore, InMemoryWorkflowStore>();
        return services;
    }
}
