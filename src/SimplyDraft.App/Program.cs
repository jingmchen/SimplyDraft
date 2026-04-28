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
        var loggingTemplate = "[{@t:dd-MMM-yyyy}] [{@t:HH:mm:ss}] [{@l:u3}]" +
            "{#if SourceContext is not null} [{SourceContext}]{#end}" +
            " [{@m}]" +
            "{#if @x is not null} [{@x}]{#end}" +
            "\n";
        
        // ─── BOOTSTRAP LOGGER ──────────────────────
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(formatter: new ExpressionTemplate(loggingTemplate))
            .WriteTo.File(
                path: AppConstants.File.Path.BootstrapLog,
                rollingInterval: RollingInterval.Day,
                formatter: new ExpressionTemplate(loggingTemplate)
            )
            .CreateBootstrapLogger();

        Log.Information("Application starting");

        // ─── LOAD SETTINGS ─────────────────────────
        ILoggerFactory factory = new SerilogLoggerFactory(Log.Logger);
        var settingsLogger = factory.CreateLogger<SettingsProvider>();
        var settings = new SettingsProvider(settingsLogger);

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
                .WriteTo.Console(formatter: new ExpressionTemplate(loggingTemplate));
        #else
            loggerConfig = loggerConfig
                .MinimumLevel.Is(settings.Current.LoggingSection.MinimumLevel);
        #endif
        
        if (settings.Current.LoggingSection.OutputToFile)
        {
            loggerConfig = loggerConfig.WriteTo.File(
                    path: AppConstants.File.Path.LatestLog,
                    retainedFileCountLimit: settings.Current.LoggingSection.RetainedFileCountLimit,
                    formatter: new ExpressionTemplate(loggingTemplate)
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