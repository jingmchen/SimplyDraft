// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace SimplyDraft.UI.Common;

internal static class UIWindows
{
    internal static Window? Active
        => Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow
            : null;
}