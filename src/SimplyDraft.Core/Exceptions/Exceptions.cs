// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Domains;

namespace SimplyDraft.Core.Exceptions;

public sealed class ScriptException : Exception
{
    public Diagnostic Diagnostic {get;}

    public ScriptException(Diagnostic diagnostic) : base(diagnostic.ToString())
        => Diagnostic = diagnostic;
    
    public static ScriptException Error(DiagnosticCode code, string message, int line, int column)
        => new(new Diagnostic(code, DiagnosticSeverity.Error, message, line, column));
}