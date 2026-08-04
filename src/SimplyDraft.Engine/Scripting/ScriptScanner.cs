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
        try { stmts = Parser.ParseScript(script); }
        catch { return Array.Empty<ScenarioChoice>(); }

        var result = new List<ScenarioChoice>();
        foreach (var m in stmts.OfType<MatchStatement>())
        {
            if (m.Subject is not NameExpression name) continue;
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
            return Array.Empty<string>();
        }
    }

    private static void Collect(IEnumerable<Statement> stmts, HashSet<string> into)
    {
        foreach (var s in stmts)
        {
            switch (s)
            {
                case AssignmentStatement a:
                    into.Add(a.Name);
                    break;
                case IfStatement i:
                    foreach (var (_, body) in i.Branches) Collect(body, into);
                    break;
                case MatchStatement m:
                    foreach (var c in m.Cases) Collect(c.Body, into);
                    break;
            }
        }
    }
}