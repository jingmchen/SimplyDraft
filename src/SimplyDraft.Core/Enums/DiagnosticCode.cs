// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace SimplyDraft.Core.Enums;

public enum DiagnosticCode
{
    // 100 - Syntax / Evaluation
    SyntaxError = 100,
    TypeMismatch = 101,
    TemporalParse = 102,

    // 200 - Assignment
    AssignToBuiltin = 200,

    // 300 - Functions / Builtins / Formulas
    UnknownFunction = 300,
    WrongArity = 301,
    UnknownBuiltin = 302,
    CircularFormula = 303,

    // 400 - Advisory / Warnings
    FrontMatterWarning = 400,
    ScriptLikeText = 401,
    MarkupWarning = 402,
    UndefinedVariable = 403,
    DeclaredTypeMismatch = 404,

    // 500 - Resource limits
    LimitExceeded = 500
}