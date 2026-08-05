// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using SimplyDraft.Core.Abstractions.Engine;

namespace SimplyDraft.Engine.DependencyInjection;

public static class EngineServiceExtension
{
    public static IServiceCollection AddEngineServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMarkupEngine, MarkupEngine>();
        services.AddSingleton<IScriptingEngine, ScriptingEngine>();
        services.AddSingleton<IBatchGenerator, BatchGenerator>();
        return services;
    }
}