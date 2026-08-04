namespace SimplyDraft.Core.Domains.Markup;

public readonly record struct MarkupResult(
    MarkupDocument Document,
    string Rendered
);