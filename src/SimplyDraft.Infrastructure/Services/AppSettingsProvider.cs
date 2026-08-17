// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Configuration.AppSettings;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Infrastructure.Services;

public sealed partial class AppSettingsProvider : SettingsProvider<AppSettings>
{
    private const int MinRetainedFiles = 1;
    private const int MaxRetainedFiles = 30;
    private const int MinAutoSaveMins = 1;
    private const int MaxAutoSaveMins = 5;

    public AppSettingsProvider(ILogger<AppSettingsProvider> logger, IAppPaths appPaths)
        : base(logger, appPaths.UserAppSettingsFile)
    {
    }

    // ─── OVERWRITTEN METHODS ───────────────────
    protected override AppSettings Sanitize(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.Library ??= new();
        settings.Editor ??= new();
        settings.Generation ??= new();
        settings.Export ??= new();
        settings.Theme ??= new();
        settings.Logging ??= new();
        settings.Theme ??= new();

        settings.Library.TrashPurgeDays =
            Math.Clamp(settings.Library.TrashPurgeDays, MinRetainedFiles, MaxRetainedFiles);
        
        settings.Editor.AutoSaveMinutes =
            Math.Clamp(settings.Editor.AutoSaveMinutes, MinAutoSaveMins, MaxAutoSaveMins);

        if (!Enum.IsDefined(settings.Logging.MinimumLevel))
            settings.Logging.MinimumLevel = LogLevel.Information;
        
        settings.Logging.RetainedFileCountLimit =
            Math.Clamp(settings.Logging.RetainedFileCountLimit, MinRetainedFiles, MaxRetainedFiles);
        
        if (!Enum.IsDefined(settings.Theme.Theme))
            settings.Theme.Theme = AppTheme.Light;
        
        if (!Enum.IsDefined(settings.Theme.Accent))
            settings.Theme.Accent = AppAccent.Black;
        
        return settings;
    }
}