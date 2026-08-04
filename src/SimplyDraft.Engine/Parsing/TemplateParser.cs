using System.Text;
using System.Text.RegularExpressions;
using SimplyDraft.Core.Domains;
using SimplyDraft.Core.Domains.Document.Segments;
using SimplyDraft.Core.Enums;
using SimplyDraft.Core.Exceptions;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Parsing;

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

        void FlushText()
        {
            if (textBuffer.Length > 0)
            {
                ScanText(textBuffer.ToString(), textStartLine, segments);
                textBuffer.Clear();
            }
        }

        while (position < normalized.Length)
        {
            int newlineIndex = normalized.IndexOf('\n', position);
            bool hasNewline = newlineIndex >= 0;
            string lineText = hasNewline ? normalized[position..newlineIndex] : normalized[position..];

            if (lineText.Trim() == ScriptingConstants.Template.ScriptOpen)
            {
                FlushText();
                int scriptBlockLine = lineNumber;
                position = hasNewline ? newlineIndex + 1 : normalized.Length;
                lineNumber++;
                var scriptSource = new StringBuilder();
                int scriptContentLine = lineNumber;
                bool sawEndScript = false;
                while (position < normalized.Length)
                {
                    int innerNewlineIndex = normalized.IndexOf('\n', position);
                    bool hasInnerNewline = innerNewlineIndex >= 0;
                    string scriptLine = hasInnerNewline ? normalized[position..innerNewlineIndex] : normalized[position..];
                    position = hasInnerNewline ? innerNewlineIndex + 1 : normalized.Length;
                    lineNumber++;
                    if (scriptLine.Trim() == ScriptingConstants.Template.ScriptClose) { sawEndScript = true; break; }
                    scriptSource.Append(scriptLine).Append('\n');
                }
                if (!sawEndScript)
                    throw ScriptException.Error(DiagnosticCode.SyntaxError,
                        "missing #ENDSCRIPT for this #SCRIPT block", scriptBlockLine, 1);
                segments.Add(new ScriptSegment(scriptContentLine, 1, scriptSource.ToString()));
                textStartLine = lineNumber;
                continue;
            }

            textBuffer.Append(lineText);
            if (hasNewline) textBuffer.Append('\n');
            position = hasNewline ? newlineIndex + 1 : normalized.Length;
            lineNumber++;
        }
        FlushText();
        return segments;
    }

    public static bool TryParse(string body, out List<Segment> segments, out Diagnostic? error)
    {
        try { segments = Parse(body); error = null; return true; }
        catch (ScriptException ex) { segments = new List<Segment>(); error = ex.Diagnostic; return false; }
    }

    private static void ScanText(string text, int startLine, List<Segment> segments)
    {
        var literalBuffer = new StringBuilder();
        int lineNumber = startLine, column = 1;
        int literalStartLine = lineNumber, literalStartColumn = column;
        int position = 0;
        // LaTeX-argument tracking: a '{' right after \cmd / \cmd* / \cmd[opts] (or chained onto the
        // '}' of a previous such argument, e.g. \begin{tabular}{lcr}) is literal markup, not a
        // placeholder. Placeholders INSIDE the argument still substitute: \section{ {title} }.
        int argumentDepth = 0;
        bool justClosedArg = false;

        void FlushLiteral()
        {
            if (literalBuffer.Length > 0)
            {
                segments.Add(new LiteralSegment(literalStartLine, literalStartColumn, literalBuffer.ToString()));
                literalBuffer.Clear();
            }
            literalStartLine = lineNumber; literalStartColumn = column;
        }

        while (position < text.Length)
        {
            char ch = text[position];
            if (ch == ScriptingConstants.Template.PlaceholderOpen)
            {
                // literal escape {{
                if (position + 1 < text.Length && text[position + 1] == ScriptingConstants.Template.PlaceholderOpen)
                { literalBuffer.Append(ScriptingConstants.Template.PlaceholderOpen); position += 2; column += 2; justClosedArg = false; continue; }

                // literal command-argument brace (see note above)
                if (justClosedArg || EndsWithCommand(literalBuffer))
                {
                    literalBuffer.Append(ScriptingConstants.Template.PlaceholderOpen);
                    argumentDepth++;
                    justClosedArg = false;
                    position++; column++;
                    continue;
                }

                int segmentLine = lineNumber, segmentColumn = column;

                // inline expression {= … }
                if (position + 1 < text.Length && text[position + 1] == ScriptingConstants.Template.ExpressionMarker)
                {
                    FlushLiteral();
                    int scanIndex = position + 2;
                    int braceDepth = 1;
                    char quote = '\0';   // '\0' = outside a string; otherwise the open quote char
                    var expressionSource = new StringBuilder();
                    while (scanIndex < text.Length)
                    {
                        char exprChar = text[scanIndex];
                        if (quote != '\0')
                        {
                            // Inside a string literal, mirror the script lexer: a backslash escapes the
                            // next char, and only the matching quote closes the string — so '{' and '}'
                            // inside it are literal and must NOT change the brace depth. (The old scanner
                            // tracked only " with ""-doubling, so a } inside '…' ended the expression
                            // early and a valid {= greeting + '}' } failed with a spurious error.)
                            if (exprChar == ScriptingConstants.Lexical.StringEscape && scanIndex + 1 < text.Length)
                            {
                                expressionSource.Append(exprChar).Append(text[scanIndex + 1]); scanIndex += 2; continue;
                            }
                            if (exprChar == quote) quote = '\0';
                            expressionSource.Append(exprChar); scanIndex++; continue;
                        }
                        if (exprChar == ScriptingConstants.Lexical.DoubleQuote || exprChar == ScriptingConstants.Lexical.SingleQuote) { quote = exprChar; expressionSource.Append(exprChar); scanIndex++; continue; }
                        if (exprChar == ScriptingConstants.Template.PlaceholderOpen) { braceDepth++; expressionSource.Append(exprChar); scanIndex++; continue; }
                        if (exprChar == ScriptingConstants.Template.PlaceholderClose) { braceDepth--; if (braceDepth == 0) break; expressionSource.Append(exprChar); scanIndex++; continue; }
                        expressionSource.Append(exprChar); scanIndex++;
                    }
                    if (braceDepth != 0)
                        throw ScriptException.Error(DiagnosticCode.SyntaxError, "unterminated {= expression", segmentLine, segmentColumn);
                    segments.Add(new InlineExpressionSegment(segmentLine, segmentColumn, expressionSource.ToString()));
                    for (int advanceIndex = position; advanceIndex <= scanIndex; advanceIndex++)
                    { if (text[advanceIndex] == '\n') { lineNumber++; column = 1; } else column++; }
                    position = scanIndex + 1;
                    literalStartLine = lineNumber; literalStartColumn = column;
                    justClosedArg = false;
                    continue;
                }

                // placeholder {name} or {SYSTEM.member} / {DOC.member}
                int nameScan = position + 1;
                if (nameScan < text.Length && (char.IsLetter(text[nameScan]) || text[nameScan] == '_'))
                {
                    int nameStart = nameScan; nameScan++;
                    while (nameScan < text.Length && (char.IsLetterOrDigit(text[nameScan]) || text[nameScan] == '_')) nameScan++;
                    string firstPart = text[nameStart..nameScan];
                    string? member = null;
                    if (nameScan < text.Length && text[nameScan] == ScriptingConstants.Template.MemberSeparator)
                    {
                        nameScan++;
                        int memberStart = nameScan;
                        while (nameScan < text.Length && (char.IsLetterOrDigit(text[nameScan]) || text[nameScan] == '_')) nameScan++;
                        member = text[memberStart..nameScan];
                    }
                    if (nameScan < text.Length && text[nameScan] == ScriptingConstants.Template.PlaceholderClose)
                    {
                        FlushLiteral();
                        if (member != null)
                        {
                            bool isValidBuiltin = member.Length > 0 &&
                                      (firstPart.Equals(ScriptingConstants.Builtins.System, StringComparison.OrdinalIgnoreCase) ||
                                       firstPart.Equals(ScriptingConstants.Builtins.Doc, StringComparison.OrdinalIgnoreCase));
                            if (!isValidBuiltin)
                                throw ScriptException.Error(DiagnosticCode.SyntaxError,
                                    $"invalid placeholder {{{firstPart}.{member}}} — dotted names are reserved for system.* and doc.*", segmentLine, segmentColumn);
                            segments.Add(new PlaceholderSegment(
                                segmentLine, segmentColumn, firstPart.ToLowerInvariant(), member.ToLowerInvariant()
                            ));
                        }
                        else
                        {
                            segments.Add(new PlaceholderSegment(segmentLine, segmentColumn, firstPart));
                        }
                        int consumed = (nameScan - position) + 1; // includes braces; placeholders never span lines
                        column += consumed;
                        position = nameScan + 1;
                        literalStartLine = lineNumber; literalStartColumn = column;
                        justClosedArg = false;
                        continue;
                    }
                    throw ScriptException.Error(DiagnosticCode.SyntaxError,
                        $"unterminated placeholder '{{{firstPart}…' — expected '}}'", segmentLine, segmentColumn);
                }
                throw ScriptException.Error(DiagnosticCode.SyntaxError,
                    "invalid placeholder — names start with a letter or '_' (write '{{' for a literal brace)", segmentLine, segmentColumn);
            }

            if (ch == ScriptingConstants.Template.PlaceholderClose)
            {
                if (position + 1 < text.Length && text[position + 1] == ScriptingConstants.Template.PlaceholderClose)
                { literalBuffer.Append(ScriptingConstants.Template.PlaceholderClose); position += 2; column += 2; justClosedArg = false; continue; }
                literalBuffer.Append(ScriptingConstants.Template.PlaceholderClose); position++; column++;   // a lone '}' is literal text
                if (argumentDepth > 0) { argumentDepth--; justClosedArg = true; }
                else justClosedArg = false;
                continue;
            }

            literalBuffer.Append(ch);
            if (ch == '\n') { lineNumber++; column = 1; } else column++;
            position++;
            justClosedArg = false;
        }
        FlushLiteral();
    }

    /// <summary>True when the pending literal text ends in \cmd, \cmd* or \cmd[opts] — i.e. the next '{' is a LaTeX argument.</summary>
    private static bool EndsWithCommand(StringBuilder pendingLiteral)
    {
        if (pendingLiteral.Length == 0) return false;
        int tailLength = Math.Min(pendingLiteral.Length, 96);
        return CommandTail.IsMatch(pendingLiteral.ToString(pendingLiteral.Length - tailLength, tailLength));
    }

    private static readonly Regex CommandTail =
        new(@"\\[A-Za-z]+\*?(\[[^\[\]\n]*\])?$", RegexOptions.Compiled);

    private static readonly Regex PlaceholderRegex =
        new(@"(?<!\{)\{([A-Za-z_][A-Za-z0-9_]*)\}(?!\})", RegexOptions.Compiled);

    /// <summary>Distinct user variables referenced by the body (lenient — used for the editor's implicit-variables panel).</summary>
    public static IReadOnlyList<string> ScanUserVariables(string body)
    {
        var found = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (TryParse(body, out var segments, out _))
        {
            foreach (var segment in segments)
                if (segment is PlaceholderSegment { IsBuiltin: false } placeholder && seen.Add(placeholder.Name))
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
}