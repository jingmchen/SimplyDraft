// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Configuration.AppSettings;

public sealed record ExportSettings
{
    public DocumentKind DefaultFormat {get; set;} = DocumentKind.Docx;
    public bool TxtBom {get; set;}
    public NewLineMode TxtNewLine {get; set;} = NewLineMode.Platform;
}