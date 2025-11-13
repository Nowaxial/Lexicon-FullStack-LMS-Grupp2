using LMS.Blazor.Client.Handlers;
using LMS.Blazor.Client.Services;

namespace LMS.Blazor.Client.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Client API Services
        services.AddScoped<IApiService, ClientApiService>();
        services.AddScoped<IAuthReadyService, AuthReadyService>();
        services.AddScoped<DocumentsClient>();
        services.AddScoped<IClientTokenStorage, ClientTokenStorageService>();

        // JWT Handler
        services.AddTransient<JwtAuthorizationMessageHandler>();

        /*// RÄTTADE HTTP CLIENTS - ANVÄND AZURE-ADRESSER
        services.AddHttpClient("BffClient", cfg =>
        {
            // Gå direkt till din Blazor-servers proxy istället för localhost
            cfg.BaseAddress = new Uri("");
        })
        .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

        services.AddHttpClient("ApiDirect", cfg =>
        {
            // Direkt till API:et
            cfg.BaseAddress = new Uri("");
        })
        .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

        services.AddHttpClient("LmsAPIClient", cfg =>
        {
            // Använd proxy-endpoint på din Blazor-server
            cfg.BaseAddress = new Uri("");
        })
        .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();*/


        // RÄTTADE HTTP CLIENTS - ANVÄND LOCALHOST
        services.AddHttpClient("BffClient", cfg =>
        {
            // Gå direkt till din Blazor-servers proxy istället för localhost
            cfg.BaseAddress = new Uri("http://localhost:7224/");
        })
        .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

        services.AddHttpClient("ApiDirect", cfg =>
        {
            // Direkt till API:et
            cfg.BaseAddress = new Uri("http://localhost:7213/");
        })
        .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

        services.AddHttpClient("LmsAPIClient", cfg =>
        {
            // Använd proxy-endpoint på din Blazor-server
            cfg.BaseAddress = new Uri("http://localhost:7213/");
        })
        .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

        return services;
    }
}
