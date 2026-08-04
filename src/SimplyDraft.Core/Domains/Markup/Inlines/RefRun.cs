namespace SimplyDraft.Core.Domains.Markup.Inlines;

public sealed record RefRun(
    string Key,
    int Line,
    bool Bold = false,
    bool Italic = false,
    bool Underline = false,
    bool Mono = false
) : Inline;