using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SimplyDraft.App.Views;
using SimplyDraft.App.ViewModels;
using SimplyDraft.App.Services;
using SimplyDraft.App.Services.Themes;
using SimplyDraft.App.Services.Settings;
using SimplyDraft.App.Services.FileExplorer;
using SimplyDraft.App.Configuration;

namespace SimplyDraft.App;

public sealed partial class App : Application
{
    private IHost? _host;
    private ILogger<App> _logger = null!;
    
    public override void Initialize() => AvaloniaXamlLoader.Load(this);
    public override async void OnFrameworkInitializationCompleted()
    {
        // ─── HOST SETUP ────────────────────────────
        _host = Host.CreateDefaultBuilder()
            .UseSerilog() // Microsoft.Logger as interface, Serilog as engine
            .ConfigureServices(ConfigureAppServices)
            .Build();
        
        await _host.StartAsync();
        _logger = _host.Services.GetRequiredService<ILogger<App>>();
        _logger.LogDebug("Successfully started Host.");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // ─── LOAD THEMES ───────────────────────────
            var themeManager = _host.Services.GetRequiredService<IThemeService>();
            themeManager.Initialize();
            
            _logger.LogInformation("Launching App...");
            var viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow(viewModel);

            desktop.ShutdownRequested += async(_, _) =>
            {
                _logger.LogInformation("Exiting App...");

                // Dispose objects
                themeManager.Dispose();

                await _host.StopAsync();
                _host.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureAppServices(IServiceCollection services)
    {
        // ViewModels
        services.AddSingleton<MainWindowViewModel>();

        // Services - SettingsProvider
        services.AddSingleton<ISettingsProvider, SettingsProvider>();

        // Services - Theme
        services.AddSingleton<IThemeService, ThemeService>();

        // Services - FileExplorerService related
        services.AddSingleton<FileWatcherService>(
            sp => new FileWatcherService(
                sp.GetRequiredService<ILogger<FileWatcherService>>(),
                TimeSpan.FromMilliseconds(AppConstants.Service.FileWatcher.DebounceMs)
            )
        );
        services.AddSingleton<IFileExplorerService, FileExplorerService>();
        services.AddSingleton<FileExplorerViewModel>();
    }
}