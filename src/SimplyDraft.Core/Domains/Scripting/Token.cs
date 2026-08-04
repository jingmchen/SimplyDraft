using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Scripting;

public readonly record struct Token(
    TokenKind Kind,
    string Text,
    double NumberValue,
    int Line,
    int Column
);