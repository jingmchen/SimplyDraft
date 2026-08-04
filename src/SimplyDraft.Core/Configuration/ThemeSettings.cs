using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Configuration;

public sealed record ThemeSettings
{
    public AppTheme Theme {get; set;} = AppTheme.Light;
    public AppAccent Accent {get; set;} = AppAccent.Black;
}
