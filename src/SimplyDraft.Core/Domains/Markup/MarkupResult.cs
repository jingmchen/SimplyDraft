// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Markup;

public readonly record struct MarkupResult(
    MarkupDocument Document,
    string Rendered
);