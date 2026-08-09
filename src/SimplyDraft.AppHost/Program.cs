// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Templates;
using SimplyDraft.Engine.DependencyInjection;
using SimplyDraft.Infrastructure.DependencyInjection;
using SimplyDraft.Infrastructure.Services;
using SimplyDraft.Infrastructure.Utils;
using SimplyDraft.UI;
using SimplyDraft.UI.DependencyInjection;
using SimplyDraft.UI.Services;

namespace SimplyDraft.AppHost;

internal sealed class Program
{
    internal const string LoggerFormat =
        "[{@t:dd-MMM-yyyy}] [{@t:HH:mm:ss}] [{@l:u3}]" +
        "{#if SourceContext is not null} [{SourceContext}]{#end}" +
        " [{@m}]" +
        "{#if @x is not null} [{@x}]{#end}" +
        "\n";
    
    [STAThread]
    internal static int Main(string[] args)
    {
        var appInfo = new AppInfo(typeof(Program).Assembly);
        var appPaths = new AppPaths(appInfo);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(formatter: new ExpressionTemplate(LoggerFormat))
            .CreateBootstrapLogger();
        
        var bootstrapLoggerFactory = new SerilogLoggerFactory();

        try
        {
            var logsHandler = new LogsHandler(
                logger: bootstrapLoggerFactory.CreateLogger<LogsHandler>(),
                logsFolderPath: appPaths.LogsFolder,
                latestLogFilePath: appPaths.LatestLogFile);
            
            logsHandler.ArchivePreviousLatestLogFile();

            var settings = new AppSettingsProvider(
                logger: bootstrapLoggerFactory.CreateLogger<AppSettingsProvider>(),
                appPaths: appPaths);
            
            using IHost host = BuildHost(args, appPaths, settings);
            
            host.Start();

            Log.Information(
                "Starting {AppName} version: {Version}",
                appInfo.Product,
                appInfo.InfoVersion);

            try
            {
                return BuildAvaloniaApp(host.Services)
                    .StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                host.StopAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly.");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHost BuildHost(string[] args, AppPaths appPaths, AppSettingsProvider appSettings)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Services.AddSerilog((services, configuration) => configuration
            .MinimumLevel.Is(appSettings.Current.LoggingSection.MinimumLevel)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Debug(
                formatter: new ExpressionTemplate(LoggerFormat))
            .WriteTo.Console(
                formatter: new ExpressionTemplate(LoggerFormat))
            .WriteTo.File(
                path: appPaths.LatestLogFile,
                formatter: new ExpressionTemplate(LoggerFormat))
            .WriteTo.ConsolePane(services));  
        
        builder.Services.AddSingleton(typeof(Program).Assembly);
        builder.Services.AddInfrastructureServices();
        builder.Services.AddEngineServices();
        builder.Services.AddUIServices();
        
        return builder.Build();
    }

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
            .WithInterFont();
}