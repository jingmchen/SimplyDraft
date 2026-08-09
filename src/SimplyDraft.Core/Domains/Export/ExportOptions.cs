// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Export;

public sealed record ExportOptions
{
    public NewLineMode NewLine {get; init;} = NewLineMode.Platform;
    public bool WriteBom {get; init;}
}