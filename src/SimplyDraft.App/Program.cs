using Microsoft.Extensions.Logging;
using Avalonia;
using Avalonia.ReactiveUI;
using Serilog;
using Serilog.Templates;
using Serilog.Extensions.Logging;
using SimplyDraft.App.Configuration;
using SimplyDraft.App.Exceptions;
using SimplyDraft.App.Utils;
using SimplyDraft.App.Services.Settings;
using Avalonia.WebView.Desktop;

namespace SimplyDraft.App;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // ─── BOOTSTRAP LOGGER ──────────────────────
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(formatter: new ExpressionTemplate(AppConstants.Service.Logger.LoggingTemplate))
            .WriteTo.File(
                path: AppConstants.File.Path.BootstrapLog,
                rollingInterval: RollingInterval.Day,
                formatter: new ExpressionTemplate(AppConstants.Service.Logger.LoggingTemplate)
            )
            .CreateBootstrapLogger();

        Log.Information("Application starting");

        // ─── LOAD SETTINGS ─────────────────────────
        var settings = new SettingsProvider(
            new SerilogLoggerFactory(Log.Logger).CreateLogger<SettingsProvider>()
        );

        Directory.CreateDirectory(AppConstants.Directory.Path.Logs);
        LogsHandler.CleanOldLogs(7);
        LogsHandler.ArchivePreviousLogs();

        // ─── SET UP LOGGER ─────────────────────────
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext();
        #if DEBUG
            loggerConfig = loggerConfig
                .MinimumLevel.Verbose()
                .WriteTo.Console(formatter: new ExpressionTemplate(AppConstants.Service.Logger.LoggingTemplate));
        #else
            loggerConfig = loggerConfig
                .MinimumLevel.Is(settings.Current.LoggingSection.MinimumLevel);
        #endif
        
        if (settings.Current.LoggingSection.OutputToFile)
        {
            loggerConfig = loggerConfig.WriteTo.File(
                    path: AppConstants.File.Path.LatestLog,
                    retainedFileCountLimit: settings.Current.LoggingSection.RetainedFileCountLimit,
                    formatter: new ExpressionTemplate(AppConstants.Service.Logger.LoggingTemplate)
                );
        }

        // Dispose bootstrap logger to release sinks
        var oldLogger = Log.Logger;
        Log.Logger = loggerConfig.CreateLogger();
        (oldLogger as IDisposable)?.Dispose();
        
        Log.ForContext<Program>().Debug("Successfully initialized Serilog");

        // ─── LAUNCH AVALONIA APP ───────────────────
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) when (ex is not SimplyDraftException)
        {
            Log.Fatal(ex, "Unhandled exception");
        }
        catch (SimplyDraftException ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().UseReactiveUI().UseDesktopWebView();
}