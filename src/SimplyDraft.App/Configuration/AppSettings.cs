using SimplyDraft.App.Services.Themes;
using Serilog.Events;

namespace SimplyDraft.App.Configuration;

public sealed class AppSettings
{
    public LoggingConfig LoggingSection {get; set;} = new();
    public ThemeConfig ThemeSection {get; set;} = new();
}

public sealed class LoggingConfig
{
    public bool OutputToFile {get; set;} = true;
    public LogEventLevel MinimumLevel {get; set;} = LogEventLevel.Information;
    public int RetainedFileCountLimit {get; set;} = 7;
}

public sealed class ThemeConfig
{
    public bool FollowSystemTheme {get; set;} = true;
    public bool AutoSave {get; set;} = true;
    public AppTheme Theme {get; set;} = AppTheme.Light;
    public AppAccent Accent {get; set;} = AppAccent.Black;
}