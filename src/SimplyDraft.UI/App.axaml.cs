// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using SimplyDraft.UI.Views;

namespace SimplyDraft.UI;

public sealed partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly ILogger<App> _logger;
    private readonly IStartupTasks _startupTasks;

    public App(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = _services.GetRequiredService<ILogger<App>>();
        _startupTasks = _services.GetRequiredService<IStartupTasks>();
    }

    public override void Initialize()
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (_services is { } services)
        {
            Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
            _startupTasks.Run();
            
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = _services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Unhandled exception on UI thread.");
        e.Handled = true;
    }
}