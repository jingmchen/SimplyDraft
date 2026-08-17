// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Configuration.AppSettings;

public sealed record LibrarySettings
{
    public int TrashPurgeDays {get; set;} = 7;
}