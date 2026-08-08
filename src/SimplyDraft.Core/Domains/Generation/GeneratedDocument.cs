// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Documents;

namespace SimplyDraft.Core.Domains.Generation;

public sealed record GeneratedDocument
{
    public required string Text {get; init;}
    public string? FontName {get; init;}
    public int? FontSizePt {get; init;}
    public bool HasMarkup {get; init;}
    public string? BaseDirectory {get; init;}
    public string? PageHeader {get; init;}

    public static GeneratedDocument From(string text, FrontMatter fm, string? baseDirectory = null)
        => new()
        {
            Text = text,
            FontName = fm.DocxFont,
            FontSizePt = fm.DocxSizePt,
            HasMarkup = fm.HasMarkup,
            BaseDirectory = baseDirectory,
            PageHeader = fm.DocxHeader
        };
}