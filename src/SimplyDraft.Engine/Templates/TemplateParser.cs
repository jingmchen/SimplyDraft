// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Text;
using System.Text.RegularExpressions;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Document.Segments;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Templates;

public static class TemplateParser
{
    public static List<Segment> Parse(string body)
    {
        var normalized = (body ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        var segments = new List<Segment>();
        var textBuffer = new StringBuilder();
        int textStartLine = 1;
        int lineNumber = 1;
        int position = 0;

        void FlushTextRun()
        {
            if (textBuffer.Length == 0)
                return;
            
            new TemplateTextScanner(textBuffer.ToString(), textStartLine, segments).Scan();
            textBuffer.Clear();
        }

        while (position < normalized.Length)
        {
            int newlineIndex = normalized.IndexOf('\n', position);
            bool hasNewline = newlineIndex >= 0;
            string lineText = hasNewline ? normalized[position..newlineIndex] : normalized[position..];

            if (lineText.Trim() == ScriptingConstants.Template.ScriptOpen)
            {
                FlushTextRun();
                position = hasNewline ? newlineIndex + 1 : normalized.Length;
                ReadScriptBlock(normalized, ref position, ref lineNumber, segments);
                textStartLine = lineNumber;
                continue;
            }

            textBuffer.Append(lineText);
            
            if (hasNewline)
                textBuffer.Append('\n');
            
            position = hasNewline ? newlineIndex + 1 : normalized.Length;
            lineNumber++;
        }
        FlushTextRun();
        return segments;
    }

    public static bool TryParse(string body, out List<Segment> segments, out Diagnostic? error)
    {
        try
        {
            segments = Parse(body);
            error = null;
            return true;
        }
        catch (ScriptException ex)
        {
            segments = [];
            error = ex.Diagnostic;
            return false;
        }
    }

    public static IReadOnlyList<string> ScanUserVariables(string body)
    {
        var found = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (TryParse(body, out var segments, out _))
        {
            foreach (var segment in segments)
                if (segment is PlaceholderSegment {IsBuiltin: false} placeholder && seen.Add(placeholder.Name))
                    found.Add(placeholder.Name);
        }
        else
        {
            foreach (Match match in PlaceholderRegex.Matches(body ?? ""))
                if (seen.Add(match.Groups[1].Value))
                    found.Add(match.Groups[1].Value);
        }
        return found;
    }

    private static void ReadScriptBlock(string normalized, ref int position, ref int lineNumber, List<Segment> segments)
    {
        int scriptBlockLine = lineNumber;
        lineNumber++; // past the #SCRIPT line itself
        int scriptContentLine = lineNumber;
        var scriptSource = new StringBuilder();
        bool sawEndScript = false;

        while (position < normalized.Length)
        {
            int newlineIndex = normalized.IndexOf('\n', position);
            bool hasNewline = newlineIndex >= 0;
            string scriptLine = hasNewline ? normalized[position..newlineIndex] : normalized[position..];
            position = hasNewline ? newlineIndex + 1 : normalized.Length;
            lineNumber++;
            
            if (scriptLine.Trim() == ScriptingConstants.Template.ScriptClose)
            {
                sawEndScript = true;
                break;
            }

            scriptSource.Append(scriptLine).Append('\n');
        }
        if (!sawEndScript)
            throw ScriptException.Error(
                DiagnosticCode.SyntaxError, "missing #ENDSCRIPT for this #SCRIPT block", scriptBlockLine, 1
            );
        
        segments.Add(new ScriptSegment(scriptSource.ToString(), scriptContentLine, 1));
    }

    private static readonly Regex PlaceholderRegex =
        new(@"(?<!\{)\{([A-Za-z_][A-Za-z0-9_]*)\}(?!\})", RegexOptions.Compiled);
}