// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Configuration.AppSettings;

public sealed record ThemeSettings
{
    public AppTheme Theme {get; set;} = AppTheme.Light;
    public AppAccent Accent {get; set;} = AppAccent.Black;
}
