namespace SimplyDraft.Core.Domains.Markup.Inlines;

public sealed record TextRun(
    string Text,
    bool Bold = false,
    bool Italic = false,
    bool Underline = false,
    bool Mono = false,
    bool SmallCaps = false
) : Inline;