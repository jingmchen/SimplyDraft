// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Infrastructure.Services;

namespace SimplyDraft.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtension
{   
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        services.AddSingleton<IDocumentExporter, DocxExporter>();
        services.AddSingleton<IDocumentExporter, TxtExporter>();
        services.AddSingleton<ILibrary, Library>();
        services.AddSingleton<ILibraryWatcher, LibraryWatcher>();
        services.AddSingleton<IAppInfo, AppInfo>();
        services.AddSingleton<IAppPaths, AppPaths>();
        services.AddSingleton<ILibraryPaths, LibraryPaths>();
        services.AddSingleton<IAppSettingsProvider, AppSettingsProvider>();
        return services;
    }
}