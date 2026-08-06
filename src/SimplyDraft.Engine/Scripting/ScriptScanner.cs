// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using SimplyDraft.Core.Domains.Scripting;
using SimplyDraft.Core.Domains.Scripting.Expressions;
using SimplyDraft.Core.Domains.Scripting.Statements;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Engine.Scripting;

public static class ScriptScanner
{
    public static IReadOnlyList<ScenarioChoice> Scenarios(string script)
    {
        List<Statement> stmts;
        try
        {
            stmts = Parser.ParseScript(script);
        }
        catch
        {
            return Array.Empty<ScenarioChoice>();
        }

        var result = new List<ScenarioChoice>();

        foreach (var m in stmts.OfType<MatchStatement>())
        {
            if (m.Subject is not NameExpression name)
                continue;
            
            var options = m.Cases
                .Where(c => c.Literal is {Kind: ValueKind.Str})
                .Select(c => c.Literal!.AsString)
                .Distinct(StringComparer.Ordinal)
                .ToList();
            
            if (options.Count > 0)
                result.Add(new ScenarioChoice(name.Name, options, m.Cases.Any(c => c.Literal is null)));
        }
        return result;
    }

    public static IReadOnlyList<string> AssignedNames(string script)
    {
        try
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Collect(Parser.ParseScript(script), set);
            return set.ToList();
        }
        catch
        {
            return [];
        }
    }

    private static void Collect(IEnumerable<Statement> statements, HashSet<string> into)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case AssignmentStatement assignStmt:
                    into.Add(assignStmt.Name);
                    break;
                
                case IfStatement ifStmt:
                    foreach (var (_, body) in ifStmt.Branches)
                        Collect(body, into);
                    break;

                case MatchStatement matchStmt:
                    foreach (var c in matchStmt.Cases)
                        Collect(c.Body, into);
                    break;
            }
        }
    }
}