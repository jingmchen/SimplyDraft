// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Domains.Scripting.Statements;

public sealed record MatchCase(
    Value? Literal,
    IReadOnlyList<Statement> Body,
    int Line,
    int Column
);