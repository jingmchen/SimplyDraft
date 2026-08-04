using Serilog.Events;

namespace SimplyDraft.Core.Configuration;

public sealed record LoggingSettings
{
    public LogEventLevel MinimumLevel {get; set;} = LogEventLevel.Information;
    public int RetainedFileCountLimit {get; set;} = 7;
}