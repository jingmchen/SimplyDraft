using System.Text;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Documents;
using SimplyDraft.Core.Enums;

namespace SimplyDraft.Engine.Parsing;

public static class FrontMatterParser
{
    public static (FrontMatter Fm, string Body, List<Diagnostic> Warnings) Parse(string fileText)
    {
        var frontMatter = new FrontMatter();
        var warnings = new List<Diagnostic>();
        var normalized = (fileText ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        if (!(normalized.StartsWith("---\n", StringComparison.Ordinal) || normalized == "---"))
            return (frontMatter, normalized, warnings);

        var lines = normalized.Split('\n');
        int closingFenceLine = -1;
        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            if (lines[lineIndex].TrimEnd() == "---") { closingFenceLine = lineIndex; break; }
        if (closingFenceLine < 0)
        {
            warnings.Add(Warn("front matter '---' is never closed; treating the whole file as body", 1));
            return (frontMatter, normalized, warnings);
        }

        string? currentMap = null;

        string ReadScalar(string rawValue, int keyIndent, ref int lineIndex)
        {
            rawValue = rawValue.Trim();
            if (rawValue == "|" || rawValue == "|-")
            {
                var blockLines = new List<string>();
                int baseIndent = -1;
                while (lineIndex + 1 < closingFenceLine)
                {
                    string nextLine = lines[lineIndex + 1];
                    if (nextLine.Trim().Length == 0) { blockLines.Add(""); lineIndex++; continue; }
                    int nextIndent = nextLine.Length - nextLine.TrimStart(' ').Length;
                    if (nextIndent <= keyIndent) break;
                    if (baseIndent < 0) baseIndent = nextIndent;
                    // Strip at most the line's OWN indent: an under-indented continuation line
                    // (less than the first content line but more than the key) must not lose real
                    // characters to a fixed baseIndent slice.
                    blockLines.Add(nextLine[Math.Min(baseIndent, nextIndent)..]);
                    lineIndex++;
                }
                while (blockLines.Count > 0 && blockLines[^1].Length == 0) blockLines.RemoveAt(blockLines.Count - 1);
                return string.Join('\n', blockLines);
            }
            return Unquote(rawValue);
        }

        for (int lineIndex = 1; lineIndex < closingFenceLine; lineIndex++)
        {
            string rawLine = lines[lineIndex];
            if (rawLine.Trim().Length == 0) continue;
            int indent = rawLine.Length - rawLine.TrimStart(' ', '\t').Length;
            string trimmed = rawLine.Trim();
            int colonIndex = trimmed.IndexOf(':');
            if (colonIndex < 0)
            {
                warnings.Add(Warn($"front matter line ignored (expected 'key: value'): {trimmed}", lineIndex + 1));
                continue;
            }
            string key = trimmed[..colonIndex].Trim();
            string rawValue = trimmed[(colonIndex + 1)..];

            if (indent == 0)
            {
                currentMap = null;
                switch (key.ToLowerInvariant())
                {
                    case "variables": currentMap = "variables"; break;
                    case "values": currentMap = "values"; break;
                    case "types": currentMap = "types"; break;
                    case "docx": currentMap = "docx"; break;
                    case "name": frontMatter.Name = ReadScalar(rawValue, indent, ref lineIndex); break;
                    case "description": frontMatter.Description = ReadScalar(rawValue, indent, ref lineIndex); break;
                    case "template": frontMatter.TemplatePath = ReadScalar(rawValue, indent, ref lineIndex); break;
                    case "markup": frontMatter.HasMarkup = ParseMarkupFlag(ReadScalar(rawValue, indent, ref lineIndex)); break;
                    default:
                        frontMatter.Extras[key] = ReadScalar(rawValue, indent, ref lineIndex);
                        warnings.Add(Warn($"unknown front matter key '{key}' (kept as-is)", lineIndex + 1));
                        break;
                }
            }
            else
            {
                if (currentMap == null)
                {
                    warnings.Add(Warn($"indented front matter line outside a map ignored: {trimmed}", lineIndex + 1));
                    continue;
                }
                string value = ReadScalar(rawValue, indent, ref lineIndex);
                switch (currentMap)
                {
                    case "variables": frontMatter.Variables[key] = value; break;
                    case "values": frontMatter.Values[key] = value; break;
                    case "types": frontMatter.Types[key] = value.Trim().ToLowerInvariant(); break;
                    case "docx":
                        if (key.Equals("font", StringComparison.OrdinalIgnoreCase)) frontMatter.DocxFont = value;
                        else if (key.Equals("size", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var sizePoints)) frontMatter.DocxSizePt = sizePoints;
                        else if (key.Equals("header", StringComparison.OrdinalIgnoreCase)) frontMatter.DocxHeader = value;
                        else warnings.Add(Warn($"unknown docx setting '{key}' ignored", lineIndex + 1));
                        break;
                }
            }
        }

        string body = closingFenceLine + 1 < lines.Length ? string.Join('\n', lines[(closingFenceLine + 1)..]) : "";
        return (frontMatter, body, warnings);
    }

    private static Diagnostic Warn(string message, int line)
        => new(DiagnosticCode.FrontMatterWarning, DiagnosticSeverity.Warning, message, line, 1);

    // The markup layer is LaTeX-only, so `markup:` is just on/off. The historical `latex` token is
    // accepted alongside plain booleans so files written before this change keep rendering.
    private static bool ParseMarkupFlag(string value)
        => value.Trim().ToLowerInvariant() is "latex" or "true" or "yes" or "on" or "1";

    private static string Unquote(string quoted)
    {
        if (quoted.Length >= 2 && quoted[0] == '"' && quoted[^1] == '"')
        {
            var inner = quoted[1..^1];
            var builder = new StringBuilder(inner.Length);
            for (int index = 0; index < inner.Length; index++)
            {
                char ch = inner[index];
                if (ch == '\\' && index + 1 < inner.Length)
                {
                    index++;
                    builder.Append(inner[index] switch { 'n' => '\n', 't' => '\t', '\\' => '\\', '"' => '"', var other => other });
                }
                else builder.Append(ch);
            }
            return builder.ToString();
        }
        return quoted;
    }

    public static string Write(FrontMatter frontMatter, string body)
    {
        var builder = new StringBuilder();
        builder.Append("---\n");

        void Scalar(string key, string value, int indent)
        {
            string padding = new(' ', indent);
            if (value.Contains('\n'))
            {
                builder.Append(padding).Append(key).Append(": |-\n");
                foreach (var line in value.Split('\n'))
                    builder.Append(padding).Append("  ").Append(line).Append('\n');
            }
            else
            {
                builder.Append(padding).Append(key).Append(": ")
                  .Append('"').Append(value.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"').Append('\n');
            }
        }

        if (frontMatter.Name != null) Scalar("name", frontMatter.Name, 0);
        if (frontMatter.Description != null) Scalar("description", frontMatter.Description, 0);
        if (frontMatter.TemplatePath != null) Scalar("template", frontMatter.TemplatePath, 0);
        if (frontMatter.HasMarkup) builder.Append("markup: true\n");
        if (frontMatter.Variables.Count > 0)
        {
            builder.Append("variables:\n");
            foreach (var entry in frontMatter.Variables) Scalar(entry.Key, entry.Value, 2);
        }
        if (frontMatter.Types.Count > 0)
        {
            builder.Append("types:\n");
            foreach (var entry in frontMatter.Types) Scalar(entry.Key, entry.Value, 2);
        }
        if (frontMatter.Values.Count > 0)
        {
            builder.Append("values:\n");
            foreach (var entry in frontMatter.Values) Scalar(entry.Key, entry.Value, 2);
        }
        if (frontMatter.DocxFont != null || frontMatter.DocxSizePt != null || frontMatter.DocxHeader != null)
        {
            builder.Append("docx:\n");
            if (frontMatter.DocxFont != null) Scalar("font", frontMatter.DocxFont, 2);
            if (frontMatter.DocxSizePt is int sizePoints) builder.Append("  size: ").Append(sizePoints).Append('\n');
            if (frontMatter.DocxHeader != null) Scalar("header", frontMatter.DocxHeader, 2);
        }
        foreach (var entry in frontMatter.Extras) Scalar(entry.Key, entry.Value, 0);
        builder.Append("---\n");
        builder.Append(body);
        return builder.ToString();
    }
}