using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Library;

public sealed record LibraryItem(
    string FilePath,
    LibraryItemKind Kind,
    string Name,
    string? TemplateRef,
    DateTime Modified,
    bool Broken,
    bool Baked = false
);