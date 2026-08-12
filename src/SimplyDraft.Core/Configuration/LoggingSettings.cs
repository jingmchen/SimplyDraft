// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;

namespace SimplyDraft.Core.Configuration;

public sealed record LoggingSettings
{
    public LogLevel MinimumLevel {get; set;} = LogLevel.Information;
    public int RetainedFileCountLimit {get; set;} = 7;
}
