using Microsoft.Extensions.DependencyInjection;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.Services;
using SimplyDraft.UI.Views;

namespace SimplyDraft.UI.DependencyInjection;

public static class UIServiceExtension
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        services.AddServices();
        
        services.AddSingleton<App>();
        services.AddSingleton<MainWindow>();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IUriPaths, UriPaths>();
        services.AddSingleton<IThemeService, ThemeService>();
        
        return services;
    }
}