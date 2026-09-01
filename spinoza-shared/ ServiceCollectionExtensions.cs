using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Spinoza.Shared;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSpinozaClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Spinoza:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Spinoza:BaseUrl is not configured.");
        }

        services.AddHttpClient<ISpinozaClient, SpinozaClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }
}