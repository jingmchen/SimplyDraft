using Microsoft.Extensions.DependencyInjection;
using SimplyDraft.Core.Abstractions.Engine;
using SimplyDraft.Engine.Services;

namespace SimplyDraft.Engine.DependencyInjection;

public static class EngineServiceExtension
{
    public static IServiceCollection AddEngineServices(this IServiceCollection services)
    {
        services.AddSingleton<IMarkupEngine, MarkupEngine>();
        services.AddSingleton<IScriptingEngine, ScriptingEngine>();
        services.AddSingleton<IBatchGenerator, BatchGenerator>();
        return services;
    }
}