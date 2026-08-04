using Microsoft.Extensions.DependencyInjection;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Infrastructure.Services;

namespace SimplyDraft.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtension
{   
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IAppInfo, AppInfo>();
        services.AddSingleton<IAppPaths, AppPaths>();
        services.AddSingleton<ILibraryPaths, LibraryPaths>();
        services.AddSingleton<IAppSettingsProvider, AppSettingsProvider>();
        services.AddSingleton<ILibrary, Library>();
        return services;
    }
}