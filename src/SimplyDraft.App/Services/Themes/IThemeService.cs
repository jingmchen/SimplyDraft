namespace SimplyDraft.App.Services.Themes;

// ─── ENUMS ─────────────────────────────────
public enum AppTheme {Light, Dark, System}
public enum AppAccent {Black, White, System}

// ─── EVENT ARGS ────────────────────────────
public sealed class ThemeChangedEventArgs(AppTheme theme, AppAccent accent)
{
    public AppTheme Theme {get;} = theme;
    public AppAccent Accent {get;} = accent;
}

// ─── INTERFACE ─────────────────────────────
public interface IThemeService : IDisposable
{
    bool FollowSystemTheme {get;}
    bool Persistence {get;}
    AppTheme CurrentTheme {get;}
    AppAccent CurrentAccent {get;}
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    void Initialize();
    void SetTheme(AppTheme theme);
    void SetAccent(AppAccent accent);
    void Apply(AppTheme theme, AppAccent accent);
    void ToggleSystemTheme(bool followSystemTheme);
    void ToggleDarkMode();
    void TogglePersistence(bool autoSave);
}