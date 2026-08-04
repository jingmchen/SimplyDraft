using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Primitives;

namespace SimplyDraft.Core.Exceptions;

public sealed class ScriptException : Exception
{
    public Diagnostic Diagnostic {get;}

    public ScriptException(Diagnostic diagnostic) : base(diagnostic.ToString())
        => Diagnostic = diagnostic;
    
    public static ScriptException Error(DiagnosticCode code, string message, int line, int col)
        => new(new Diagnostic(code, message, line, col, DiagnosticSeverity.Error));
}