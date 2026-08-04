using System.Text;
using SimplyDraft.Core.Domains.Markup.Inlines;
using SimplyDraft.Engine.Constants;

namespace SimplyDraft.Engine.Markup;

public static class MarkupHelper
{
    public static string StripComment(string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == MarkupConstants.Delimiters.SyntaxStart) { i++; continue; }
            if (line[i] == MarkupConstants.Delimiters.Comment) return line[..i];
        }
        return line;
    }

    public static string Flatten(IEnumerable<Inline> inlines)
    {
        var sb = new StringBuilder();
        foreach (var i in inlines)
        {
            if (i is TextRun r) sb.Append(r.Text);
            else if (i is LineBreak) sb.Append(' ');
            else if (i is RefRun rr) sb.Append('?').Append(rr.Key).Append('?');
        }
        return sb.ToString();
    }

    public static int MatchBrace(string s, int openIdx, int end)
    {
        int depth = 0;
        for (int i = openIdx; i < end; i++)
        {
            char c = s[i];
            if (c == MarkupConstants.Delimiters.SyntaxStart) { i++; continue; }
            if (c == MarkupConstants.Delimiters.GroupOpen) depth++;
            else if (c == MarkupConstants.Delimiters.GroupClose)
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }
}