// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Export;
using SimplyDraft.UI.Factories;
using SimplyDraft.UI.Services;
using SimplyDraft.UI.ViewModels;
using SimplyDraft.UI.ViewModels.Components;
using SimplyDraft.UI.Views;

namespace SimplyDraft.UI.DependencyInjection;

public static class UIServiceExtension
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMisc();
        services.AddServices();
        services.AddFactories();
        services.AddSingleton<App>();
        services.AddTransient<MainWindow>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<LibraryBrowserViewModel>();
        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ILibraryActions, LibraryActions>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<ConsoleLogService>();
        services.AddSingleton<IConsoleLogService>(sp => sp.GetRequiredService<ConsoleLogService>());
        services.AddSingleton<ILogEventSink>(sp => sp.GetRequiredService<ConsoleLogService>());
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();        
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IUriPaths, UriPaths>();
        services.AddSingleton<IStartupTasks, StartupTasks>();
        services.AddSingleton<ITermsService, TermsService>();
        return services;
    }

    private static IServiceCollection AddFactories(this IServiceCollection services)
    {
        services.AddSingleton<IWindowFactory<EditorWindow>, EditorWindowFactory>();
        services.AddSingleton<IWindowFactory<GenerateChildWindow>, GenerateChildWindowFactory>();
        services.AddSingleton<IWindowFactory<SettingsWindow>, SettingsWindowFactory>();
        return services;
    }
    
    private static IServiceCollection AddMisc(this IServiceCollection services)
    {
        services.TryAddSingleton<ExporterCatalog>();
        return services;
    }
}