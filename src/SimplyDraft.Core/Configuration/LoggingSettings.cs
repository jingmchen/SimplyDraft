// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Serilog.Events;

namespace SimplyDraft.Core.Configuration;

public sealed record LoggingSettings
{
    public LogEventLevel MinimumLevel {get; set;} = LogEventLevel.Information;
    public int RetainedFileCountLimit {get; set;} = 7;
}