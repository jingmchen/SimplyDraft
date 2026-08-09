// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Markup.Inlines;

public readonly record struct InlineStyle(
    bool Bold,
    bool Italic,
    bool Underline,
    bool Mono,
    bool SmallCaps
)
{
    public static InlineStyle None => default;
}