// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Configuration.AppSettings;
using SimplyDraft.Core.Enums;
using SimplyDraft.UI.Common.Editor;
using SimplyDraft.UI.Constants;
using SimplyDraft.UI.Utils;

namespace SimplyDraft.UI.Services;

public sealed class ThemeService : IThemeService
{
    private readonly ISettingsProvider<AppSettings> _settings;
    private readonly ILogger<ThemeService> _logger;
    private IPlatformSettings? _platformSettings;
    private ResourceDictionary? _themeSlot;
    private ResourceDictionary? _accentSlot;
    private readonly Dictionary<AppTheme, ResourceDictionary> _themeCache = [];
    private readonly Dictionary<AppAccent, ResourceDictionary> _accentCache = [];
    private bool _isInitialized;
    private bool _isDisposed;
    private readonly CompositeFormat _themeTemplate;
    private readonly CompositeFormat _accentTemplate;
    public AppTheme CurrentTheme {get; private set;}
    public AppAccent CurrentAccent {get; private set;}
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    
    public ThemeService(ISettingsProvider<AppSettings> settings, ILogger<ThemeService> logger, IUriPaths uriPaths)
    {
        ArgumentNullException.ThrowIfNull(uriPaths);
        
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _themeTemplate = CompositeFormat.Parse(uriPaths.ThemeTemplate);
        _accentTemplate = CompositeFormat.Parse(uriPaths.AccentTemplate);

        Initialize();
    }

    // ─── PUBLIC METHODS ────────────────────────
    public void Initialize()
    {
        if (_isInitialized)
        {
            _logger.LogWarning("{ThemeService} is already initialized.", nameof(ThemeService));
            return;
        }

        ThrowIfAppNotReady();

        CurrentTheme = _settings.Current.Theme.Theme;
        CurrentAccent = _settings.Current.Theme.Accent;

        _themeSlot = new ResourceDictionary();
        _accentSlot = new ResourceDictionary();

        var merged = Application.Current!.Resources.MergedDictionaries;

        merged.Add(_themeSlot);
        merged.Add(_accentSlot);

        ApplyCore(CurrentTheme, CurrentAccent, fireEvent: false, persist: true);

        // Subscribe to OS theme changes
        _platformSettings = Application.Current!.PlatformSettings;
        _platformSettings!.ColorValuesChanged += OnSystemThemeChanged;

        _isInitialized = true;
    }

    public void SetTheme(AppTheme theme)
        => SetBoth(theme, CurrentAccent);
    
    public void SetAccent(AppAccent accent)
        => SetBoth(CurrentTheme, accent);
    
    public void SetBoth(AppTheme theme, AppAccent accent)
    {
        ThrowIfNotInitialized();
        ThrowIfDisposed();

        if (theme == CurrentTheme && accent == CurrentAccent)
            return;

        DispatcherHelper.PostOnUIThread(()
            => ApplyCore(theme, accent, fireEvent: true, persist: true));
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _isDisposed = true;
        
        if (_platformSettings is not null)
            _platformSettings.ColorValuesChanged -= OnSystemThemeChanged;
    }

    // ─── PRIVATE METHODS ───────────────────────
    // Core implementation
    private void ApplyCore(AppTheme theme, AppAccent accent, bool fireEvent, bool persist = true)
    {
        if (_isDisposed)
            return;

        CurrentTheme = theme;
        CurrentAccent = accent;
        
        var effectiveTheme = theme == AppTheme.System ? GetSystemTheme() : theme;
        var isDark = IsDarkTheme(effectiveTheme);

        // Fluent theme for built-in control styles
        Application.Current!.RequestedThemeVariant =
            isDark ? ThemeVariant.Dark : ThemeVariant.Light;

        var merged = Application.Current!.Resources.MergedDictionaries;
        var themeDictionary = GetOrLoadDictionary(_themeCache, effectiveTheme, ThemeUri);
        var accentDictionary = GetOrLoadDictionary(_accentCache, CurrentAccent, AccentUri);

        _themeSlot?.MergedDictionaries.Clear();
        _themeSlot?.MergedDictionaries.Add(themeDictionary);

        _accentSlot?.MergedDictionaries.Clear();
        _accentSlot?.MergedDictionaries.Add(accentDictionary);
        
        // Call EditorSyntax to update palette
        EditorSyntax.SetTheme(isDark, GetColor(accentDictionary));

        if (persist)
            Persist();

        if (fireEvent)
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(theme, accent));
    }

    private void Persist()
    {
        _settings.Current.Theme.Theme = CurrentTheme;
        _settings.Current.Theme.Accent = CurrentAccent;
        _settings.Save();
    }

    private void OnSystemThemeChanged(object? sender, PlatformColorValues e)
    {
        if (_isDisposed || CurrentTheme != AppTheme.System)
            return;

        DispatcherHelper.PostOnUIThread(()
            => ApplyCore(AppTheme.System, CurrentAccent, fireEvent: true, persist: true));
    }
    
    private static AppTheme GetSystemTheme()
        => Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant == PlatformThemeVariant.Dark
            ? AppTheme.Black
            : AppTheme.Light;

    private static bool IsDarkTheme(AppTheme theme)
        => theme switch
        {
            AppTheme.Black or AppTheme.DarkGraphite or AppTheme.DarkNavy => true,
            AppTheme.Light or AppTheme.White => false,
            _ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "Invalid theme.")
        };
    
    // Color helpers for AvalonEditor
    private Color GetColor(ResourceDictionary dictionary)
    {
        if (dictionary.TryGetValue(UIConstants.ThemeManagement.AccentKey, out var value1) && value1 is Color color)
            return color;
        
        if (dictionary.TryGetValue(UIConstants.ThemeManagement.AccentBrushKey, out var value2) && value2 is SolidColorBrush brush)
            return brush.Color;

        throw new KeyNotFoundException($"Accent keys not found in {CurrentAccent}.");
    }

    // Cache helpers
    private static ResourceDictionary GetOrLoadDictionary<TKey>(
        Dictionary<TKey, ResourceDictionary> cache,
        TKey key,
        Func<TKey, Uri> uriFactory) where TKey : notnull
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
        return include.Loaded as ResourceDictionary
            ?? throw new InvalidOperationException($"Resource at '{uri}' is not a ResourceDictionary.");
    }

    // Uri helpers
    private Uri ThemeUri(AppTheme theme)
        => new(string.Format(CultureInfo.InvariantCulture, _themeTemplate, theme));
    
    private Uri AccentUri(AppAccent accent)
        => new(string.Format(CultureInfo.InvariantCulture, _accentTemplate, accent));
    

    // Guard
    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException($"{nameof(ThemeService)} is not initialized yet.");
    }

    private static void ThrowIfAppNotReady()
    {
        if (Application.Current?.ApplicationLifetime is null)
            throw new InvalidOperationException("Application is not yet ready.");
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_isDisposed, nameof(ThemeService));
}
