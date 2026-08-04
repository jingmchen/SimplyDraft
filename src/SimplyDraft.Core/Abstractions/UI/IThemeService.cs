using Avalonia.Media;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Abstractions.UI;

public sealed class ThemeChangedEventArgs(AppTheme theme, AppAccent accent) : EventArgs
{
    public AppTheme Theme { get; } = theme;
    public AppAccent Accent { get; } = accent;
}

public interface IThemeService : IDisposable
{
    AppTheme CurrentTheme { get; }
    AppAccent CurrentAccent { get; }
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    void Initialize();
    void SetTheme(AppTheme theme);
    void SetAccent(AppAccent accent);
    void SetBoth(AppTheme theme, AppAccent accent);
    IBrush GetAccentSwatch(AppAccent accent);
}