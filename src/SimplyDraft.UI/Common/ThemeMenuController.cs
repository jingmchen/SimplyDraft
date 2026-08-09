// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SimplyDraft.Core.Abstractions.UI;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.UI.Common;

public sealed class ThemeMenuController : IDisposable
{
    private readonly IThemeService _theme;
    private readonly Dictionary<AppTheme, MenuItem> _themeItems = [];
    private readonly Dictionary<AppAccent, MenuItem> _accentItems = [];

    public ThemeMenuController(
        IThemeService theme,
        MenuItem themeRoot,
        MenuItem accentRoot)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        ArgumentNullException.ThrowIfNull(themeRoot);
        ArgumentNullException.ThrowIfNull(accentRoot);

        BuildThemeItems(themeRoot);
        BuildAccentItems(accentRoot);
        SyncChecks();
        _theme.ThemeChanged += OnThemeChanged;
    }

    public void Dispose()
        => _theme.ThemeChanged -= OnThemeChanged;

    private void BuildThemeItems(MenuItem root)
    {
        foreach (var theme in Enum.GetValues<AppTheme>())
        {
            var item = new MenuItem
            {
                Header = DisplayName(theme.ToString()),
                Tag = theme,
                ToggleType = MenuItemToggleType.Radio,
                GroupName = "theme",
            };

            if (theme == AppTheme.System)
                ToolTip.SetTip(item, "Follows the OS light/dark setting (Black / White)");
            
            item.Click += OnThemePick;
            _themeItems[theme] = item;
            root.Items.Add(item);
        }
    }

    private void BuildAccentItems(MenuItem root)
    {
        foreach (var accent in Enum.GetValues<AppAccent>())
        {
            var item = new MenuItem
            {
                Header = DisplayName(accent.ToString()),
                Tag = accent,
                ToggleType = MenuItemToggleType.Radio,
                GroupName = "accent",
            };

            item.Click += OnAccentPick;
            _accentItems[accent] = item;
            root.Items.Add(item);
        }
    }

    private void OnThemePick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem {Tag: AppTheme theme})
            _theme.SetTheme(theme);
    }

    private void OnAccentPick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem {Tag: AppAccent accent})
            _theme.SetAccent(accent);
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e) => SyncChecks();

    private void SyncChecks()
    {
        foreach (var (theme, item) in _themeItems)
            item.IsChecked = theme == _theme.CurrentTheme;
        foreach (var (accent, item) in _accentItems)
            item.IsChecked = accent == _theme.CurrentAccent;
    }

    private static string DisplayName(string enumName)
    {
        var builder = new StringBuilder(enumName.Length + 2);

        for (int i = 0; i < enumName.Length; i++)
        {
            if (i > 0 && char.IsUpper(enumName[i]))
                builder.Append(' ');
            builder.Append(enumName[i]);
        }

        return builder.ToString();
    }
}