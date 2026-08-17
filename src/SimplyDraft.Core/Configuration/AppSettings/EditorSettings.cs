// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Configuration.AppSettings;

public sealed record EditorSettings
{
    public bool WordWrap {get; set;} = true;
    public int AutoSaveMinutes {get; set;} = 2;
}