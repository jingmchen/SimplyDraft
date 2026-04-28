using System.Text.RegularExpressions;
using SimplyDraft.Core.Models;

namespace SimplyDraft.Core.Services;

public sealed class TxtService : IDocumentService
{
    private static readonly Regex variableRegex = new(@"\{\{[^}]+\}\}", RegexOptions.Compiled);

    // ─── EXPOSED METHODS ───────────────────────
    public List<(int Index, string Text)> ExtractParagraphs(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        return lines
            .Select((line, i) => (i, line))
            .ToList();
    }

    public Dictionary<string, int> FindExistingVariables(string filePath)
    {
        var counts = new Dictionary<string, int>();
        foreach (var line in File.ReadAllLines(filePath))
        {
            foreach (Match m in variableRegex.Matches(line))
                counts[m.Value] = counts.TryGetValue(m.Value, out var c) ? c + 1 : 1;
        }
        return counts;
    }

    public bool InjectVariable(
        string filePath, string selectedText, string variableKey,
        VariableType type = VariableType.Instance, int paragraphIndex = -1
    )
    {
        var lines = File.ReadAllLines(filePath);
        bool replaced = false;

        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains(selectedText, StringComparison.Ordinal)) continue;

            if (type == VariableType.Instance && paragraphIndex >= 0 && i != paragraphIndex) continue;

            lines[i] = type == VariableType.Instance
                ? ReplaceFirst(lines[i], selectedText, variableKey)
                : lines[i].Replace(selectedText, variableKey, StringComparison.Ordinal);

            replaced = true;
            if (type == VariableType.Instance) break;
        }

        if (replaced)
        {
            var rawContent = File.ReadAllText(filePath);
            string lineEnding = rawContent.Contains("\r\n") ? "\r\n" : "\n";
            File.WriteAllText(filePath, string.Join(lineEnding, lines));
        }

        return replaced;
    }

    public void ExportToChild(string filePath, string outputPath, List<Variable> variables)
    {
        var rawContent = File.ReadAllText(filePath);
        string lineEnding = rawContent.Contains("\r\n") ? "\r\n" : "\n";

        var lines = File.ReadAllLines(filePath);

        var substitutions = new Dictionary<string, string>();
        foreach (var v in variables)
        {
            if (v is FixedVariable fixedVariable) continue;

            string resolvedValue = v.RuntimeValue ?? string.Empty;

            if (v is ConditionalVariable conditionalVariable)
            {
                var rule = conditionalVariable.ConditionalRules
                    .FirstOrDefault(r => r.WhenValue.Equals(v.RuntimeValue,
                        StringComparison.OrdinalIgnoreCase));
                resolvedValue = rule?.ThenSubstitute ?? v.RuntimeValue ?? string.Empty;
            }

            substitutions[v.Key] = resolvedValue;
        }

        for (int i = 0; i < lines.Length; i++)
            foreach (var (key, value) in substitutions)
                lines[i] = lines[i].Replace(key, value);

        File.WriteAllText(outputPath, string.Join(lineEnding, lines));
    }

    // ─── INTERNAL METHODS (VARIABLE) ───────────
    private static string ReplaceFirst(string source, string search, string replacement)
    {
        int pos = source.IndexOf(search, StringComparison.Ordinal);
        if (pos < 0) return source;
        return source[..pos] + replacement + source[(pos + search.Length)..];
    }
}