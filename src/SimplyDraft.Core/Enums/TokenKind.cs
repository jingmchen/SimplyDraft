// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Enums;

public enum TokenKind
{
    NewLine, Indent, Dedent, EndOfLine,
    Ident, Str, Num,
    Colon, Comma, LeftParen, RightParen, LeftBracket, RightBracket, Dot,
    Assign, Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual,
    Plus, Minus, Star, Slash, Percent,
    If, ElseIf, Else, And, Or, Not, In, True, False
}