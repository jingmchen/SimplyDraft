// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Library;

public sealed record LibraryItem(
    string FilePath,
    LibraryItemKind Kind,
    string Name,
    string? TemplateRef,
    DateTime Modified,
    bool Baked = false
);