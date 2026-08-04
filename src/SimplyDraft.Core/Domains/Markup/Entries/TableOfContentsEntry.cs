namespace SimplyDraft.Core.Domains.Markup.Entries;

public sealed record TableOfContentsEntry(
    int Level,
    string Number,
    string Text
);