// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Scripting;

public readonly record struct Token(
    TokenKind Kind,
    string Text,
    double NumberValue,
    int Line,
    int Column
);