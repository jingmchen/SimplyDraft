// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;

namespace SimplyDraft.UI.Services;

public static class ConsolePaneSinkExtensions
{
    public static LoggerConfiguration ConsolePane(this LoggerSinkConfiguration writeTo, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(writeTo);
        ArgumentNullException.ThrowIfNull(services);

        return writeTo.Sink(services.GetRequiredService<ConsoleLogService>());
    }
}