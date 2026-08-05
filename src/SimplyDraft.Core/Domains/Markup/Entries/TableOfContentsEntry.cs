// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Markup.Entries;

public sealed record TableOfContentsEntry(
    int Level,
    string Number,
    string Text
);