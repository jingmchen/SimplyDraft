// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.Views;
using SimplyDraft.UI.Utils;

namespace SimplyDraft.UI;

public sealed partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly IAtomicFileAsync _fileWriter;
    private readonly ILogger<App> _logger;

    public App(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _fileWriter = _services.GetRequiredService<IAtomicFileAsync>();
        _logger = _services.GetRequiredService<ILogger<App>>();

        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
    }

    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (_services is { } services)
        {
            var startupTasks = _services.GetRequiredService<IStartupTasks>();
            startupTasks.Run();

            var themeService = _services.GetRequiredService<IThemeService>();
            themeService.Initialize();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
                desktop.Exit += OnExit;
                DispatcherHelper.PostOnUIThread(RunTermsConditionGate);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void RunTermsConditionGate()
    {
        var terms = _services.GetRequiredService<ITermsService>();
        bool accepted = await terms.EnsureAcceptedAsync();

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        
        if (!accepted)
        {
            desktop.Shutdown();
            return;
        }

        var mainWindow = _services.GetRequiredService<MainWindow>();
        desktop.MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        => _fileWriter.FlushAsync().GetAwaiter().GetResult();

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unhandled exception on UI thread.");
        e.Handled = true;
    }
}