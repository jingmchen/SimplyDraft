using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using SimplyDraft.App.Configuration;
using SimplyDraft.App.Services.Settings;

namespace SimplyDraft.App.Services.Themes;

/// <summary>
/// Manages theme and accent for all Views centrally for the application
/// Support automatic theme switching based on platform
/// </summary>
public sealed class ThemeService : IThemeService
{
    // ─── DI-INJECTED ───────────────────────────
    private readonly ILogger<ThemeService> _logger;
    private readonly ISettingsProvider _settings;
    
    // ─── INITIALIZATION ────────────────────────
    private IPlatformSettings? _platformSettings;
    private int _themeSlot;
    private int _accentSlot;

    // ─── CONTRACT ──────────────────────────────
    public bool FollowSystemTheme {get; private set;}
    public bool Persistence {get; private set;}
    public AppTheme CurrentTheme {get; private set;}
    public AppAccent CurrentAccent {get; private set;}
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    // ─── STATE ─────────────────────────────────
    private readonly Dictionary<AppTheme, ResourceDictionary> _themeCache = [];
    private readonly Dictionary<AppAccent, ResourceDictionary> _accentCache = [];
    private bool _isInitialized;
    private bool _disposed;

    // ─── CONSTRUCTOR ───────────────────────────
    public ThemeService(ILogger<ThemeService> logger, ISettingsProvider settings)
    {
        _logger = logger;
        _settings = settings;
    }
    
    // ─── INTERFACE IMPLEMENTATION ──────────────
    /// <summary>
    /// Wires up Avalonia dependencies and applies the iniital theme and accent.
    /// <para> Throws an error if not called after <c>OnFrameworkInitializationCompleted</c> due to <c>Application.Current</c> not being set yet.</para>
    /// <para> Must be called before any other methods in <c>ThemeManager</c></para>
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("{ThemeManager} is already initialized.", nameof(ThemeService));
            return;
        }

        // Guard
        ThrowIfAppNotReady();

        // Load config from appsettings.json
        FollowSystemTheme = _settings.Current.ThemeSection.FollowSystemTheme;
        Persistence = _settings.Current.ThemeSection.AutoSave;
        CurrentTheme = _settings.Current.ThemeSection.Theme;
        CurrentAccent = _settings.Current.ThemeSection.Accent;

        var merged = Application.Current!.Resources.MergedDictionaries;
        
        // Theme slot
        merged.Add(new ResourceDictionary());
        _themeSlot = merged.Count - 1;

        // Accent slot
        merged.Add(new ResourceDictionary());
        _accentSlot = merged.Count - 1;

        // Subscribe to OS theme changes
        _platformSettings = Application.Current!.PlatformSettings;
        _platformSettings!.ColorValuesChanged += new EventHandler<PlatformColorValues>(OnSystemThemeChanged);

        // Apply theme and accent
        ApplyCore(CurrentTheme, CurrentAccent, fireEvent:false);

        _isInitialized = true;
        _logger.LogInformation($"Successfully initialized {nameof(ThemeService)}.");
    }
    public void SetTheme(AppTheme theme)
    {
        Apply(theme, CurrentAccent);
    }
    public void SetAccent(AppAccent accent)
    {
        Apply(CurrentTheme, accent);
    }
    public void Apply(AppTheme theme, AppAccent accent)
    {
        ThrowIfNotInitialized();
        ThrowIfDisposed();

        if (theme == CurrentTheme && accent == CurrentAccent) return;
        
        if (Dispatcher.UIThread.CheckAccess())
        {
            ApplyCore(theme, accent, fireEvent:true);
        }
        else
        {
            Dispatcher.UIThread.Post(() => ApplyCore(theme, accent, fireEvent:true));
        }

        if (Persistence) SavePreferences();
    }
    public void ToggleSystemTheme(bool followSystemTheme)
    {
        FollowSystemTheme = followSystemTheme;
        SetSystemTheme();
    }
    public void ToggleDarkMode()
    {
        if (FollowSystemTheme) FollowSystemTheme = false;
        var nextTheme = CurrentTheme == AppTheme.Dark? AppTheme.Light : AppTheme.Dark;
        SetTheme(nextTheme);
    }
    public void TogglePersistence(bool autoSave)
    {
        Persistence = autoSave;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _platformSettings!.ColorValuesChanged -= OnSystemThemeChanged;
    }

    // ─── INTERNAL METHODS ──────────────────────
    // Core implementation
    private void ApplyCore(AppTheme theme, AppAccent accent, bool fireEvent)
    {
        CurrentTheme = theme;
        CurrentAccent = accent;

        // Fluent theme for built-in control styles
        Application.Current!.RequestedThemeVariant = CurrentTheme == AppTheme.Dark? ThemeVariant.Dark : ThemeVariant.Light;
        
        var merged = Application.Current!.Resources.MergedDictionaries;

        // Verification
        System.Diagnostics.Debug.Assert(
            merged.Count > _accentSlot,
            "MergedDictionaries was modified externally — theme slots are corrupted."
        );

        merged[_themeSlot] = GetOrLoadDictionary(
            _themeCache,
            CurrentTheme,
            ThemeUri
        );

        merged[_accentSlot] = GetOrLoadDictionary(
            _accentCache,
            CurrentAccent,
            AccentUri
        );

        _logger.LogInformation(
            "Applied theme: {CurrentTheme}, accent: {CurrentAccent}",
            CurrentTheme, CurrentAccent
        );

        if (fireEvent)
        {
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(theme, accent));
        }
    }

    private void SavePreferences()
    {
        _settings.Current.ThemeSection.Theme = CurrentTheme;
        _settings.Current.ThemeSection.Accent = CurrentAccent;
    }
    
    // Guard
    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException(
                $"{nameof(ThemeService)} not initialized yet."
            );
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, nameof(ThemeService));

    private static void ThrowIfAppNotReady()
    {
        if (Application.Current?.ApplicationLifetime is null)
            throw new InvalidOperationException(
                "ApplicationLifetime is not yet set."
            );
    }

    // System theme listener
    private void OnSystemThemeChanged(object? sender, PlatformColorValues e)
    {
        if (FollowSystemTheme) SetSystemTheme();
    }

    private void SetSystemTheme()
    {
        Dispatcher.UIThread.Post(() => ApplyCore(GetSystemTheme(), CurrentAccent, fireEvent:true));
    }

    private static AppTheme GetSystemTheme()
    {
        return Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant
            == PlatformThemeVariant.Dark
            ? AppTheme.Dark
            : AppTheme.Light;
    }

    // Cache helpers
    private static ResourceDictionary GetOrLoadDictionary<TKey>(
        Dictionary<TKey, ResourceDictionary> cache,
        TKey key,
        Func<TKey, Uri> uriFactory
    ) where TKey : notnull
    {
        if (!cache.TryGetValue(key, out var dict))
        {
            dict = LoadDictionary(uriFactory(key));
            cache[key] = dict;
        }
        return dict;
    }

    private static ResourceDictionary LoadDictionary(Uri uri)
    {
        var include = new ResourceInclude(uri) {Source = uri};
        return include.Loaded as ResourceDictionary ?? throw new InvalidOperationException($"Resource at '{uri}' is not a ResourceDictionary.");
    }

    // URI helpers
    private static Uri ThemeUri(AppTheme theme)
        => new(string.Format(AppConstants.Uri.ThemeTemplate, theme));
    
    private static Uri AccentUri(AppAccent accent)
        => new (string.Format(AppConstants.Uri.AccentTemplate, accent));
}