using Microsoft.Extensions.DependencyInjection;
using SimplyDraft.App.Services.FileExplorer;
using SimplyDraft.App.Services.Settings;
using SimplyDraft.App.Services.Themes;
using SimplyDraft.App.ViewModels;

namespace SimplyDraft.App;

public static class AppServices
{
    public static void ConfigureAppServices(IServiceCollection services)
    {
        // Services - SettingsProvider
        services.AddSingleton<ISettingsProvider, SettingsProvider>();

        // Services - Theme
        services.AddSingleton<IThemeService, ThemeService>();

        // Services - FileExplorerService related
        services.AddSingleton<IFileExplorerFactory, FileExplorerFactory>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<TestWindowViewModel>();
    }
}