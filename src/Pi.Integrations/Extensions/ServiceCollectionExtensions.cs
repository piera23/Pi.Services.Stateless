using Microsoft.Extensions.DependencyInjection;
using Pi.Integrations.Protocollo;
using Pi.Integrations.Reporting;

namespace Pi.Integrations.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPiProtocolloMock(this IServiceCollection services)
    {
        services.AddScoped<IProtocolloService, MockProtocolloService>();
        return services;
    }

    public static IServiceCollection AddPiReporting(this IServiceCollection services)
    {
        services.AddScoped<IReportGenerator, DocumentReportGenerator>();
        return services;
    }
}
