// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using Microsoft.Extensions.Logging;
using SimplyDraft.Core.Abstractions.Infrastructure;
using SimplyDraft.Core.Configuration.UserStateSettings;

namespace SimplyDraft.Infrastructure.Services;

public sealed class UserStateSettingsProvider : SettingsProvider<UserStateSettings>
{
    public UserStateSettingsProvider(ILogger<UserStateSettingsProvider> logger, IAppPaths appPaths)
        : base(logger, appPaths.UserStateSettingsFile)
    {
    }

    // ─── OVERWRITTEN METHODS ───────────────────
    protected override UserStateSettings Sanitize(UserStateSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.Terms ??= new();
        
        return settings;
    }
}