// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Enums;

namespace SimplyDraft.Core.Domains;

public sealed record Diagnostic(
    DiagnosticCode Code,
    DiagnosticSeverity Severity,
    string Message,
    int Line,
    int Col)
{
    public override string ToString() => $"{Code} ({Line}:{Col}): {Message}";
}