// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains.Generation;

public sealed class GenerationResult
{
    public bool Success {get; set;}
    public string Text {get; set;} = "";
    public List<Diagnostic> Diagnostics {get; set;} = [];

    public static GenerationResult Fail(DiagnosticCode code, string message)
    {
        var result = new GenerationResult {Success = false};
        result.Diagnostics.Add(new Diagnostic(code, DiagnosticSeverity.Error, message, 1, 1));
        return result;
    }
}