// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Markup;

namespace SimplyDraft.Infrastructure.Export;

public static class DocxWriter
{
    // ─── PUBLIC METHODS ────────────────────────
    public static void Write(
        string path,
        string text,
        string? fontName,
        int? fontSizePt,
        string? pageHeader = null)
    {
        //
    }

    public static void WriteMarkup(
        string path,
        MarkupDocument doc,
        string? fontName,
        int? fontSizePt,
        string? pageHeader = null,
        string? baseDirectory = null
    )
    {
        //
    }

    // ─── PRIVATE METHODS ───────────────────────
}