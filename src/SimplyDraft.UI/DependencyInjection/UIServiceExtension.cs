// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.UI.Services;
using SimplyDraft.UI.Views;

namespace SimplyDraft.UI.DependencyInjection;

public static class UIServiceExtension
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddServices();
        services.AddSingleton<App>();
        services.AddSingleton<MainWindow>();
        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IUriPaths, UriPaths>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IStartupTasks, StartupTasks>();
        return services;
    }
}